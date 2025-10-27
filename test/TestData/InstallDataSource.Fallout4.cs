using System;
using System.Collections.Generic;
using System.Text.Json;
using FomodInstaller.Interface;
using SharpCompress.Archives;

namespace TestData;

using static Fallout4TestData;

file class Fallout4TestData
{
    public record Fallout4Mod
    {
        public required string Name { get; init; }
        public required string ModFile { get; init; }
        public required Dictionary<string, List<Instruction>> InstructionsByChoice { get; init; }
    }

    public static List<string> StopPatterns =
    [
        "(^|/)fomod(/|$)",
        "[^/]*\\.esp$",
        "[^/]*\\.esm$",
        "[^/]*\\.esl$",
        "[^/]*\\.ba2$",
        "fomod/ModuleConfig.xml$",
        "(^|/)textures(/|$)",
        "(^|/)meshes(/|$)",
        "(^|/)music(/|$)",
        "(^|/)shaders(/|$)",
        "(^|/)video(/|$)",
        "(^|/)interface(/|$)",
        "(^|/)materials(/|$)",
        "(^|/)programs(/|$)",
        "(^|/)sound(/|$)",
        "(^|/)strings(/|$)",
        "(^|/)vis(/|$)",
        "(^|/)scripts(/|$)",
        "(^|/)seq(/|$)",
    ];

    public static Fallout4Mod ModImprovedMap = new()
    {
        Name = "Improved Map with Visible Roads",
        ModFile = "Improved Map with Visible Roads 2.0-1215-2-0.zip",
        InstructionsByChoice = new Dictionary<string, List<Instruction>>
        {
            ["Default"] =
            [
                new()
                {
                    type = @"copy",
                    source = @"01 Improved Map with Visible Roads - Default\Textures\interface\pip-boy\worldmap_d.dds",
                    destination = @"Textures\interface\pip-boy\worldmap_d.dds",
                },
                new()
                {
                    type = @"enableallplugins",
                },
            ],
            ["Default with Regions and Grid"] =
            [
                new()
                {
                    type = @"copy",
                    source = @"01 Improved Map with Visible Roads - Default - Regions and Grid\Textures\interface\pip-boy\worldmap_d.dds",
                    destination = @"Textures\interface\pip-boy\worldmap_d.dds",
                },
                new()
                {
                    type = @"enableallplugins",
                },
            ],
            ["Darker"] =
            [
                new()
                {
                    type = @"copy",
                    source = @"02 Improved Map with Visible Roads - Darker\Textures\interface\pip-boy\worldmap_d.dds",
                    destination = @"Textures\interface\pip-boy\worldmap_d.dds",
                },
                new()
                {
                    type = @"enableallplugins",
                },
            ],
            ["Darker with Regions and Grid"] =
            [
                new()
                {
                    type = @"copy",
                    source = @"02 Improved Map with Visible Roads - Darker - Regions and Grid\Textures\interface\pip-boy\worldmap_d.dds",
                    destination = @"Textures\interface\pip-boy\worldmap_d.dds",
                },
                new()
                {
                    type = @"enableallplugins",
                },
            ],
            ["Even Darker"] =
            [
                new()
                {
                    type = @"copy",
                    source = @"03 Improved Map with Visible Roads - Even Darker\Textures\interface\pip-boy\worldmap_d.dds",
                    destination = @"Textures\interface\pip-boy\worldmap_d.dds",
                },
                new()
                {
                    type = @"enableallplugins",
                },
            ],
            ["Even Darker with Regions and Grid"] =
            [
                new()
                {
                    type = @"copy",
                    source = @"03 Improved Map with Visible Roads - Even Darker - Regions and Grid\Textures\interface\pip-boy\worldmap_d.dds",
                    destination = @"Textures\interface\pip-boy\worldmap_d.dds",
                },
                new()
                {
                    type = @"enableallplugins",
                },
            ],
        },
    };

    public static IArchive GetFallout4Mod(string mod)
    {
        var resource = typeof(InstallDataSource).Assembly.GetManifestResourceStream($"TestData.Data.Fallout4.{mod}")!;
        return ArchiveFactory.Open(resource);
    }

    public static InstallData BaseTest(Fallout4Mod mod, string choice, IEnumerable<SelectedOption>? dialogChoices, JsonDocument? preset, List<string> installedPlugins) => new()
    {
        Game = "Fallout4",
        Mod = mod.Name,
        ModArchive = GetFallout4Mod(mod.ModFile),
        StopPatterns = StopPatterns,
        PluginPath = "Data",
        AppVersion = "1.0.0",
        GameVersion = "1.10.163.0",
        ExtenderVersion = "0.6.21",
        DialogChoices = dialogChoices,
        Preset = preset,
        InstalledPlugins = installedPlugins,
        Validate = true,
        Message = "Installation successful",
        Instructions = mod.InstructionsByChoice[choice],
    };
}

public partial class InstallDataSource
{
    public static IEnumerable<Func<InstallData>> Fallout4Data()
    {
        // Test each option choice
        yield return () => BaseTest(ModImprovedMap, "Default", [new(0, 0, [0])], null, []);
        yield return () => BaseTest(ModImprovedMap, "Default with Regions and Grid", [new(0, 0, [1])], null, []);
        yield return () => BaseTest(ModImprovedMap, "Darker", [new(0, 0, [2])], null, []);
        yield return () => BaseTest(ModImprovedMap, "Darker with Regions and Grid", [new(0, 0, [3])], null, []);
        yield return () => BaseTest(ModImprovedMap, "Even Darker", [new(0, 0, [4])], null, []);
        yield return () => BaseTest(ModImprovedMap, "Even Darker with Regions and Grid", [new(0, 0, [5])], null, []);
        
        // Test with presets
        var presetDefault = JsonDocument.Parse("[{\"name\":\"Install Improved Map with Visible Roads\",\"groups\":[{\"name\":\"Improved Map with Visible Roads\",\"choices\":[{\"name\":\"Default\",\"idx\":0}]}]}]");
        yield return () => BaseTest(ModImprovedMap, "Default", null, presetDefault, []);

        var presetDarker = JsonDocument.Parse("[{\"name\":\"Install Improved Map with Visible Roads\",\"groups\":[{\"name\":\"Improved Map with Visible Roads\",\"choices\":[{\"name\":\"Darker\",\"idx\":2}]}]}]");
        yield return () => BaseTest(ModImprovedMap, "Darker", null, presetDarker, []);
    }
}
