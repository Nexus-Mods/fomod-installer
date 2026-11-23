using FomodInstaller.Interface;
using FomodInstaller.Interface.snapshot;
using FomodInstaller.Interface.ui;
using FomodInstaller.ModInstaller;
using FomodInstaller.Scripting;
using FomodInstaller.Scripting.XmlScript;

using ModInstaller.Adaptor.Tests.Shared.Delegates;
using ModInstaller.Lite;

using System.Text.Json;

using TestData;

using Utils;

// Disambiguate from the test-only option snapshot helper in the shared delegates.
using OptionSnapshot = FomodInstaller.Interface.snapshot.OptionSnapshot;

namespace ModInstaller.Adaptor.Typed.Tests;

/// <summary>
/// Verifies the structured option-condition snapshot delivered on InstallResult.
/// </summary>
public class OptionSnapshotTests
{
    private const string ArchiveFile = "ConditionSnapshotTest.zip";

    private static InstallData MakeData(string[] installedPlugins, JsonDocument? preset = null)
    {
        return new InstallData
        {
            Game = "FOMOD",
            Mod = "Condition Snapshot Test",
            ModArchive = JsonTestDataLoader.GetArchive("FOMOD", ArchiveFile),
            StopPatterns = new List<string> { "(^|/)fomod(/|$)" },
            PluginPath = "Data",
            AppVersion = "1.0.0",
            GameVersion = "1.0.0",
            ExtenderVersion = "1.0.0",
            InstalledPlugins = installedPlugins.ToList(),
            DialogChoices = null,
            Preset = preset,
            Preselect = false,
            Validate = true,
            Message = "Installation successful",
            Instructions = new List<InstallInstruction>(),
        };
    }

    private static async Task<InstallResult> Install(InstallData data, UIDelegates ui)
    {
        var coreDelegates = new TestCoreDelegates(
            new CallbackPluginDelegates(_ => data.InstalledPlugins.ToArray()),
            new CallbackIniDelegates(null!, null!),
            new CallbackContextDelegates(
                () => data.AppVersion,
                () => data.GameVersion,
                (_) => data.ExtenderVersion,
                null!, null!, null!, null!
            ),
            ui
        );

        FileSystem.Instance = new ArchiveFileSystem(data.ModArchive);

        return await Installer.Install(
            data.ModArchive.Entries.Select(x => x.GetNormalizedName()).ToList(),
            data.StopPatterns,
            data.PluginPath,
            "",
            data.Preset,
            false,
            data.Validate,
            new ProgressDelegate((perc) => { }),
            coreDelegates);
    }

    private static OptionSnapshot FindOption(InstallSnapshot snapshot, string optionName)
    {
        return snapshot.Steps
            .SelectMany(s => s.Groups)
            .SelectMany(g => g.Options)
            .FirstOrDefault(o => o.Name == optionName);
    }

    /// <summary>
    /// Flattens a condition tree into every node it contains, including the root.
    /// </summary>
    private static IEnumerable<ConditionSnapshot> Walk(ConditionSnapshot condition)
    {
        if (condition == null)
            yield break;

        yield return condition;

        if (condition.Conditions == null)
            yield break;

        foreach (var sub in condition.Conditions)
            foreach (var node in Walk(sub))
                yield return node;
    }

    /// <summary>
    /// The condition tree the installer actually holds wraps even a single dependency in a
    /// CompositeCondition, so assertions address the leaves rather than assuming a flat shape.
    /// </summary>
    private static List<ConditionSnapshot> Leaves(ConditionSnapshot condition, string kind) =>
        Walk(condition).Where(c => c.Kind == kind).ToList();

    /// <summary>
    /// An evaluator that lives entirely outside the installer: it reads only the snapshot, a
    /// plugin list and a flag map. This is what a consumer such as Vortex would run.
    /// </summary>
    private static bool IsFulfilled(ConditionSnapshot condition,
                                   Func<string, bool> isActive,
                                   Func<string, bool> isPresent,
                                   IReadOnlyDictionary<string, string> flags)
    {
        switch (condition.Kind)
        {
            case ConditionKind.Plugin:
                return condition.PluginState switch
                {
                    "Active" => isActive(condition.PluginPath),
                    "Inactive" => isPresent(condition.PluginPath) && !isActive(condition.PluginPath),
                    "Missing" => !isPresent(condition.PluginPath),
                    _ => false,
                };
            case ConditionKind.Flag:
                flags.TryGetValue(condition.FlagName, out var value);
                return string.IsNullOrEmpty(condition.FlagValue)
                    ? string.IsNullOrEmpty(value)
                    : condition.FlagValue == value;
            case ConditionKind.Composite:
                if (condition.Operator == "Or")
                    return condition.Conditions.Any(c => IsFulfilled(c, isActive, isPresent, flags));
                return condition.Conditions.All(c => IsFulfilled(c, isActive, isPresent, flags));
            default:
                // Environment and Unknown kinds are stable / opaque; treat as not fulfilled
                // rather than guessing.
                return false;
        }
    }

    private static string Evaluate(ResolverSnapshot resolver,
                                   Func<string, bool> isActive,
                                   Func<string, bool> isPresent,
                                   IReadOnlyDictionary<string, string> flags)
    {
        if (resolver.Kind == ResolverKind.Static)
            return resolver.Type;

        foreach (var pattern in resolver.Patterns)
        {
            if (IsFulfilled(pattern.Condition, isActive, isPresent, flags))
                return pattern.Type;
        }
        return resolver.DefaultType;
    }

    [Test]
    public async Task SnapshotNamesAbsentGatingPluginAndRecordsNotUsable()
    {
        var data = MakeData(Array.Empty<string>());
        var result = await Install(data, new DeterministicUIContext(null));

        await Assert.That(result.OptionSnapshot).IsNotNull();

        var option = FindOption(result.OptionSnapshot, "Patch for Mod B");
        await Assert.That(option).IsNotNull();
        await Assert.That(option.Type).IsEqualTo("NotUsable");

        await Assert.That(option.Resolver.Kind).IsEqualTo(ResolverKind.Conditional);
        await Assert.That(option.Resolver.DefaultType).IsEqualTo("NotUsable");
        await Assert.That(option.Resolver.Patterns.Count).IsEqualTo(1);
        await Assert.That(option.Resolver.Patterns[0].Type).IsEqualTo("Required");

        var condition = option.Resolver.Patterns[0].Condition;
        var plugins = Leaves(condition, ConditionKind.Plugin);
        await Assert.That(plugins.Count).IsEqualTo(1);
        await Assert.That(plugins[0].PluginPath).IsEqualTo("b.esp");
        await Assert.That(plugins[0].PluginState).IsEqualTo("Active");

        // A plugin-gated option carries no flag or environment leaves.
        await Assert.That(Leaves(condition, ConditionKind.Flag).Count).IsEqualTo(0);
        await Assert.That(Leaves(condition, ConditionKind.Environment).Count).IsEqualTo(0);
    }

    [Test]
    public async Task SnapshotRecordsFlagConditionAndTheFlagThatSetsIt()
    {
        var data = MakeData(Array.Empty<string>());
        var result = await Install(data, new DeterministicUIContext(null));

        var setter = FindOption(result.OptionSnapshot, "Compatibility Patches");
        await Assert.That(setter).IsNotNull();
        await Assert.That(setter.Flags.Count).IsEqualTo(1);
        await Assert.That(setter.Flags[0].Name).IsEqualTo("patches");
        await Assert.That(setter.Flags[0].Value).IsEqualTo("enabled");

        var gated = FindOption(result.OptionSnapshot, "Patch for Mod C");
        var condition = gated.Resolver.Patterns[0].Condition;
        var flags = Leaves(condition, ConditionKind.Flag);
        await Assert.That(flags.Count).IsEqualTo(1);
        await Assert.That(flags[0].FlagName).IsEqualTo("patches");
        await Assert.That(flags[0].FlagValue).IsEqualTo("enabled");

        await Assert.That(Leaves(condition, ConditionKind.Plugin).Count).IsEqualTo(0);
    }

    [Test]
    public async Task CompositeOrRoundTripsWithOperatorAndAllLeaves()
    {
        var data = MakeData(Array.Empty<string>());
        var result = await Install(data, new DeterministicUIContext(null));

        var option = FindOption(result.OptionSnapshot, "Patch for Mod B or B.esl");
        var condition = option.Resolver.Patterns[0].Condition;

        await Assert.That(condition.Kind).IsEqualTo(ConditionKind.Composite);
        await Assert.That(condition.Operator).IsEqualTo("Or");
        await Assert.That(condition.Conditions.Count).IsEqualTo(2);

        await Assert.That(condition.Conditions[0].Kind).IsEqualTo(ConditionKind.Plugin);
        await Assert.That(condition.Conditions[0].PluginPath).IsEqualTo("b.esp");
        await Assert.That(condition.Conditions[0].PluginState).IsEqualTo("Active");

        await Assert.That(condition.Conditions[1].Kind).IsEqualTo(ConditionKind.Plugin);
        await Assert.That(condition.Conditions[1].PluginPath).IsEqualTo("b.esl");
        await Assert.That(condition.Conditions[1].PluginState).IsEqualTo("Active");

        // A consumer must compute Required when only B.esl is active.
        var onlyEslActive = new Func<string, bool>(p => p == "b.esl");
        var nonePresent = new Func<string, bool>(p => p == "b.esl");
        var computed = Evaluate(option.Resolver, onlyEslActive, nonePresent,
                                new Dictionary<string, string>());
        await Assert.That(computed).IsEqualTo("Required");
    }

    [Test]
    public async Task EnvironmentConditionIsPresentDistinguishableAndLeafFree()
    {
        var data = MakeData(Array.Empty<string>());
        var result = await Install(data, new DeterministicUIContext(null));

        var option = FindOption(result.OptionSnapshot, "Environment Gated Option");
        var condition = option.Resolver.Patterns[0].Condition;

        var environment = Leaves(condition, ConditionKind.Environment);
        await Assert.That(environment.Count).IsEqualTo(1);
        await Assert.That(environment[0].EnvironmentType).IsEqualTo("GameVersion");
        await Assert.That(environment[0].MinimumVersion).IsEqualTo("99.0.0");

        // Distinguishable as its own kind, and carrying no plugin or flag leaves anywhere in
        // the tree - a consumer can treat it as stable and skip it without guessing.
        await Assert.That(environment[0].PluginPath).IsNull();
        await Assert.That(environment[0].PluginState).IsNull();
        await Assert.That(environment[0].FlagName).IsNull();
        await Assert.That(environment[0].FlagValue).IsNull();
        await Assert.That(Leaves(condition, ConditionKind.Plugin).Count).IsEqualTo(0);
        await Assert.That(Leaves(condition, ConditionKind.Flag).Count).IsEqualTo(0);

        // It carries a message so a consumer can surface it without re-deriving it.
        await Assert.That(environment[0].Message).IsNotNull();
    }

    [Test]
    public async Task EvaluatingSnapshotReproducesInstallerOptionType()
    {
        // 1. Install with B.esp absent: the installer says NotUsable.
        var absent = MakeData(Array.Empty<string>());
        var absentResult = await Install(absent, new DeterministicUIContext(null));
        var absentOption = FindOption(absentResult.OptionSnapshot, "Patch for Mod B");
        await Assert.That(absentOption.Type).IsEqualTo("NotUsable");

        var computedWhenAbsent = Evaluate(absentOption.Resolver,
            _ => false, _ => false, new Dictionary<string, string>());
        await Assert.That(computedWhenAbsent).IsEqualTo(absentOption.Type);

        // 2. Install with B.esp active: the installer says Required.
        var present = MakeData(new[] { "B.esp" });
        var presentResult = await Install(present, new DeterministicUIContext(null));
        var presentOption = FindOption(presentResult.OptionSnapshot, "Patch for Mod B");
        await Assert.That(presentOption.Type).IsEqualTo("Required");

        // 3. Re-evaluating the snapshot taken when B.esp was ABSENT, against the new plugin
        //    list, must now yield Required - the change Vortex wants to report.
        var recomputed = Evaluate(absentOption.Resolver,
            p => p == "b.esp", p => p == "b.esp", new Dictionary<string, string>());
        await Assert.That(recomputed).IsEqualTo("Required");
        await Assert.That(recomputed).IsEqualTo(presentOption.Type);
    }

    [Test]
    public async Task UnattendedPresetPathProducesSameShapeAsAttended()
    {
        // Attended: no preset, dialog driven headlessly by DeterministicUIContext.
        var attended = await Install(MakeData(Array.Empty<string>()), new DeterministicUIContext(null));

        // Unattended: a preset with preselect=false runs headless with no UpdateState at all.
        var presetJson = """
            [{ "name": "Step1", "groups": [ { "name": "Compatibility", "choices": [ { "name": "Compatibility Patches", "idx": 0 } ] } ] }]
            """;
        using var preset = JsonDocument.Parse(presetJson);
        var unattended = await Install(MakeData(Array.Empty<string>(), preset), new DeterministicUIContext(null));

        await Assert.That(attended.OptionSnapshot).IsNotNull();
        await Assert.That(unattended.OptionSnapshot).IsNotNull();

        static IEnumerable<string> Shape(InstallSnapshot s) =>
            s.Steps.SelectMany(step => step.Groups.SelectMany(g => g.Options.Select(o =>
                $"{step.Name}/{g.Name}/{o.Name}|{o.Resolver.Kind}|{o.Resolver.DefaultType}|" +
                string.Join(",", (o.Resolver.Patterns ?? new List<PatternSnapshot>())
                    .Select(p => p.Type + ":" + string.Join("+", Walk(p.Condition).Select(c => c.Kind)))))));

        var attendedShape = Shape(attended.OptionSnapshot).ToList();
        var unattendedShape = Shape(unattended.OptionSnapshot).ToList();

        await Assert.That(unattendedShape.Count).IsGreaterThan(0);
        await Assert.That(unattendedShape).IsEquivalentTo(attendedShape);

        // Every option is present on the unattended path, including ones the preset did not
        // select.
        await Assert.That(FindOption(unattended.OptionSnapshot, "Patch for Mod B")).IsNotNull();
        await Assert.That(FindOption(unattended.OptionSnapshot, "Environment Gated Option")).IsNotNull();
    }

    [Test]
    public async Task ExistingInstallResultFieldsAreUnchanged()
    {
        var data = MakeData(Array.Empty<string>());
        var result = await Install(data, new DeterministicUIContext(null));

        await Assert.That(result.Message).IsEqualTo("Installation successful");
        await Assert.That(result.Instructions).IsNotNull();
        await Assert.That(result.Instructions.Count).IsGreaterThan(0);
    }
}
