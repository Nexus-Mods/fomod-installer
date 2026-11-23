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

    private static InstallData MakeData(string[] installedPlugins,
                                        JsonDocument? preset = null,
                                        string gameVersion = "1.0.0")
    {
        return new InstallData
        {
            Game = "FOMOD",
            Mod = "Condition Snapshot Test",
            ModArchive = JsonTestDataLoader.GetArchive("FOMOD", ArchiveFile),
            StopPatterns = new List<string> { "(^|/)fomod(/|$)" },
            PluginPath = "Data",
            AppVersion = "1.0.0",
            GameVersion = gameVersion,
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
    /// The three answers a condition can give when evaluated from the snapshot alone. Environment
    /// conditions are <see cref="Truth.Undecidable"/> because the version observed at install time
    /// is not recorded, so the snapshot cannot say whether the requirement was met.
    /// </summary>
    private enum Truth { False, True, Undecidable }

    /// <summary>
    /// An evaluator that lives entirely outside the installer: it reads only the snapshot, a
    /// plugin list and a flag map. This is what a consumer such as Vortex would run.
    /// </summary>
    private static Truth IsFulfilled(ConditionSnapshot condition,
                                     Func<string, bool> isActive,
                                     Func<string, bool> isPresent,
                                     IReadOnlyDictionary<string, string> flags)
    {
        switch (condition.Kind)
        {
            case ConditionKind.Plugin:
                return condition.PluginState switch
                {
                    "Active" => isActive(condition.PluginPath) ? Truth.True : Truth.False,
                    "Inactive" => isPresent(condition.PluginPath) && !isActive(condition.PluginPath)
                        ? Truth.True : Truth.False,
                    "Missing" => !isPresent(condition.PluginPath) ? Truth.True : Truth.False,
                    _ => Truth.Undecidable,
                };
            case ConditionKind.Flag:
                flags.TryGetValue(condition.FlagName, out var value);
                bool fulfilled = string.IsNullOrEmpty(condition.FlagValue)
                    ? string.IsNullOrEmpty(value)
                    : condition.FlagValue == value;
                return fulfilled ? Truth.True : Truth.False;
            case ConditionKind.Composite:
                return IsFulfilledComposite(condition, isActive, isPresent, flags);
            default:
                // Environment and Unknown kinds: the snapshot records the requirement but not the
                // observed state, so this is undecidable - NOT false. Answering false would report
                // an option the installer found usable as having become unusable.
                return Truth.Undecidable;
        }
    }

    /// <summary>
    /// Undecidability propagates through a composite, but a decided sibling can still settle it:
    /// an Or with any true child is true, an And with any false child is false.
    /// </summary>
    private static Truth IsFulfilledComposite(ConditionSnapshot condition,
                                              Func<string, bool> isActive,
                                              Func<string, bool> isPresent,
                                              IReadOnlyDictionary<string, string> flags)
    {
        bool isOr = condition.Operator == "Or";
        bool sawUndecidable = false;

        foreach (var sub in condition.Conditions)
        {
            switch (IsFulfilled(sub, isActive, isPresent, flags))
            {
                case Truth.True when isOr:
                    return Truth.True;
                case Truth.False when !isOr:
                    return Truth.False;
                case Truth.Undecidable:
                    sawUndecidable = true;
                    break;
            }
        }

        return sawUndecidable ? Truth.Undecidable : (isOr ? Truth.False : Truth.True);
    }

    /// <summary>
    /// Reproduces ConditionalOptionTypeResolver.ResolveOptionType: first matching pattern wins,
    /// else the default type. When a pattern's condition is undecidable the walk cannot continue -
    /// it may have been the first pattern to match - so the recorded install-time type is returned
    /// instead of a guess.
    /// </summary>
    private static string Evaluate(ResolverSnapshot resolver,
                                   string recordedType,
                                   Func<string, bool> isActive,
                                   Func<string, bool> isPresent,
                                   IReadOnlyDictionary<string, string> flags)
    {
        if (resolver.Kind == ResolverKind.Static)
            return resolver.Type;

        foreach (var pattern in resolver.Patterns)
        {
            switch (IsFulfilled(pattern.Condition, isActive, isPresent, flags))
            {
                case Truth.True:
                    return pattern.Type;
                case Truth.Undecidable:
                    return recordedType;
            }
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
        var computed = Evaluate(option.Resolver, option.Type, onlyEslActive, nonePresent,
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

    /// <summary>
    /// The environment counterpart of EvaluatingSnapshotReproducesInstallerOptionType. The
    /// snapshot records the version requirement but not the version observed at install time, so
    /// the leaf is undecidable - and a consumer must fall back to the recorded type rather than
    /// assume the requirement failed. Assuming failure here would report an option the installer
    /// found Required as having become unusable.
    /// </summary>
    [Test]
    public async Task FulfilledEnvironmentConditionFallsBackToRecordedType()
    {
        // The fixture requires game version 99.0.0; install with 100.0.0 so it IS fulfilled.
        var data = MakeData(Array.Empty<string>(), gameVersion: "100.0.0");
        var result = await Install(data, new DeterministicUIContext(null));

        var option = FindOption(result.OptionSnapshot, "Environment Gated Option");
        await Assert.That(option.Type).IsEqualTo("Required");
        await Assert.That(option.Resolver.DefaultType).IsEqualTo("NotUsable");

        // The leaf is recorded as Environment, carrying the requirement but not the observation.
        var environment = Leaves(option.Resolver.Patterns[0].Condition, ConditionKind.Environment);
        await Assert.That(environment.Count).IsEqualTo(1);
        await Assert.That(environment[0].MinimumVersion).IsEqualTo("99.0.0");

        // Undecidable, so the consumer returns the recorded type - not the default type.
        var computed = Evaluate(option.Resolver, option.Type,
            _ => false, _ => false, new Dictionary<string, string>());
        await Assert.That(computed).IsEqualTo("Required");
        await Assert.That(computed).IsEqualTo(option.Type);

        // The naive 'treat environment as not fulfilled' answer is the one that is wrong.
        await Assert.That(computed).IsNotEqualTo(option.Resolver.DefaultType);
    }

    /// <summary>
    /// Undecidability propagates through a composite, but a decided sibling still settles it:
    /// an Or that a plugin leaf satisfies is True regardless of an undecidable environment leaf.
    /// </summary>
    [Test]
    public async Task DecidedSiblingSettlesCompositeDespiteUndecidableLeaf()
    {
        var data = MakeData(Array.Empty<string>(), gameVersion: "100.0.0");
        var result = await Install(data, new DeterministicUIContext(null));

        var option = FindOption(result.OptionSnapshot, "Patch for Mod B or B.esl");
        var condition = option.Resolver.Patterns[0].Condition;

        // Synthesise the mixed tree the real parser can produce: Or(plugin, environment).
        var mixed = new ConditionSnapshot
        {
            Kind = ConditionKind.Composite,
            Operator = "Or",
            Conditions = new List<ConditionSnapshot>
            {
                condition.Conditions[0],
                Leaves(FindOption(result.OptionSnapshot, "Environment Gated Option")
                       .Resolver.Patterns[0].Condition, ConditionKind.Environment)[0],
            },
        };

        // Plugin leaf satisfied -> True, even though the environment leaf is undecidable.
        await Assert.That(IsFulfilled(mixed, p => p == "b.esp", p => p == "b.esp",
                                      new Dictionary<string, string>())).IsEqualTo(Truth.True);

        // Plugin leaf unsatisfied -> still undecidable, because the environment leaf might pass.
        await Assert.That(IsFulfilled(mixed, _ => false, _ => false,
                                      new Dictionary<string, string>())).IsEqualTo(Truth.Undecidable);
    }

    [Test]
    public async Task EvaluatingSnapshotReproducesInstallerOptionType()
    {
        // 1. Install with B.esp absent: the installer says NotUsable.
        var absent = MakeData(Array.Empty<string>());
        var absentResult = await Install(absent, new DeterministicUIContext(null));
        var absentOption = FindOption(absentResult.OptionSnapshot, "Patch for Mod B");
        await Assert.That(absentOption.Type).IsEqualTo("NotUsable");

        var computedWhenAbsent = Evaluate(absentOption.Resolver, absentOption.Type,
            _ => false, _ => false, new Dictionary<string, string>());
        await Assert.That(computedWhenAbsent).IsEqualTo(absentOption.Type);

        // 2. Install with B.esp active: the installer says Required.
        var present = MakeData(new[] { "B.esp" });
        var presentResult = await Install(present, new DeterministicUIContext(null));
        var presentOption = FindOption(presentResult.OptionSnapshot, "Patch for Mod B");
        await Assert.That(presentOption.Type).IsEqualTo("Required");

        // 3. Re-evaluating the snapshot taken when B.esp was ABSENT, against the new plugin
        //    list, must now yield Required - the change Vortex wants to report.
        var recomputed = Evaluate(absentOption.Resolver, absentOption.Type,
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
