using FomodInstaller.Interface;
using FomodInstaller.Interface.snapshot;

using ModInstaller.Lite;
using ModInstaller.Native.Tests.Utils;

using System.Text.Json;

using NUnit.Framework;

using static ModInstaller.Native.Tests.Utils.Utils2;

namespace ModInstaller.Native.Tests;

/// <summary>
/// Verifies that the option-condition snapshot survives the same source-generated JSON
/// serialization the native binding uses. A type missing from CustomSourceGenerationContext
/// fails here rather than in production.
/// </summary>
public class OptionSnapshotSerializationTests
{
    private static InstallResult MakeResult()
    {
        return new InstallResult
        {
            Message = "Installation successful",
            Instructions = new List<Instruction>(),
            OptionSnapshot = new InstallSnapshot
            {
                Flags = new List<FlagSnapshot>
                {
                    new FlagSnapshot { Name = "patches", Value = "enabled" },
                },
                Steps = new List<StepSnapshot>
                {
                    new StepSnapshot
                    {
                        Id = 0,
                        Name = "Step1",
                        Visible = true,
                        Groups = new List<GroupSnapshot>
                        {
                            new GroupSnapshot
                            {
                                Name = "PatchForModB",
                                Type = "SelectAny",
                                Options = new List<OptionSnapshot>
                                {
                                    new OptionSnapshot
                                    {
                                        Name = "Patch for Mod B",
                                        Type = "NotUsable",
                                        Selected = false,
                                        Flags = new List<FlagSnapshot>(),
                                        Resolver = new ResolverSnapshot
                                        {
                                            Kind = ResolverKind.Conditional,
                                            DefaultType = "NotUsable",
                                            Patterns = new List<PatternSnapshot>
                                            {
                                                new PatternSnapshot
                                                {
                                                    Type = "Required",
                                                    Condition = new ConditionSnapshot
                                                    {
                                                        Kind = ConditionKind.Composite,
                                                        Operator = "Or",
                                                        Conditions = new List<ConditionSnapshot>
                                                        {
                                                            new ConditionSnapshot
                                                            {
                                                                Kind = ConditionKind.Plugin,
                                                                PluginPath = "b.esp",
                                                                PluginState = "Active",
                                                            },
                                                            new ConditionSnapshot
                                                            {
                                                                Kind = ConditionKind.Environment,
                                                                EnvironmentType = "GameVersion",
                                                                MinimumVersion = "99.0.0",
                                                                Message = "requires a newer game",
                                                            },
                                                        },
                                                    },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            },
        };
    }

    [Test]
    public void SnapshotSerializesThroughTheNativeSourceGenerationContext()
    {
        var result = MakeResult();

        var json = JsonSerializer.Serialize(result, SourceGenerationContext.Default.InstallResult);

        Assert.That(json, Does.Contain("optionSnapshot"));
        Assert.That(json, Does.Contain("Patch for Mod B"));
        Assert.That(json, Does.Contain("NotUsable"));
        Assert.That(json, Does.Contain("b.esp"));
        Assert.That(json, Does.Contain("Composite"));
        Assert.That(json, Does.Contain("Environment"));
        Assert.That(json, Does.Contain("99.0.0"));

        // Round-trip: the consumer side must be able to read it back.
        var back = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.InstallResult);
        Assert.That(back, Is.Not.Null);
        Assert.That(back!.OptionSnapshot, Is.Not.Null);

        var condition = back.OptionSnapshot!.Steps[0].Groups[0].Options[0]
            .Resolver.Patterns![0].Condition;
        Assert.That(condition.Kind, Is.EqualTo(ConditionKind.Composite));
        Assert.That(condition.Operator, Is.EqualTo("Or"));
        Assert.That(condition.Conditions!.Count, Is.EqualTo(2));
        Assert.That(condition.Conditions[0].PluginPath, Is.EqualTo("b.esp"));
        Assert.That(condition.Conditions[1].EnvironmentType, Is.EqualTo("GameVersion"));
    }

    [Test]
    public void ResultWithoutSnapshotStillSerializes()
    {
        // Backwards compatibility: an install with no script produces no snapshot, and the
        // result must still serialize for consumers that predate the field.
        var result = new InstallResult
        {
            Message = "Installation successful",
            Instructions = new List<Instruction>(),
        };

        var json = JsonSerializer.Serialize(result, SourceGenerationContext.Default.InstallResult);

        Assert.That(json, Does.Contain("instructions"));
        Assert.That(json, Does.Not.Contain("optionSnapshot"));
    }
}
