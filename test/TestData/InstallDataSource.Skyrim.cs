using FomodInstaller.Interface;

using SharpCompress.Archives;

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TestData;

using static SkyrimTestData;

internal class SkyrimTestData
{
    public record SkyrimMod
    {
        public required string Name { get; init; }
        public required string ModFile { get; init; }
        public required List<InstallInstruction> InstructionsAE { get; init; }
        public required List<InstallInstruction> InstructionsSE { get; init; }
    }

    public static string GameVersionAE = "1.6.1170.0";
    public static string GameVersionSE = "1.5.97.0";

    public static List<string> StopPatterns =
    [
        "(^|/)fomod(/|$)",
        "[^/]*\\.esp$",
        "[^/]*\\.esm$",
        "[^/]*\\.esl$",
        "[^/]*\\.bsa$",
        "[^/]*\\.ba2$",
        "fomod/ModuleConfig.xml$",
        "(^|/)distantlod(/|$)",
        "(^|/)textures(/|$)",
        "(^|/)meshes(/|$)",
        "(^|/)music(/|$)",
        "(^|/)shaders(/|$)",
        "(^|/)video(/|$)",
        "(^|/)interface(/|$)",
        "(^|/)fonts(/|$)",
        "(^|/)scripts(/|$)",
        "(^|/)facegen(/|$)",
        "(^|/)menus(/|$)",
        "(^|/)lodsettings(/|$)",
        "(^|/)lsdata(/|$)",
        "(^|/)sound(/|$)",
        "(^|/)strings(/|$)",
        "(^|/)trees(/|$)",
        "(^|/)asi(/|$)",
        "(^|/)tools(/|$)",
        "(^|/)calientetools(/|$)",
        "(^|/)skse(/|$)",
    ];

    public static SkyrimMod ModSpellPerkItemDistributor = new()
    {
        Name = "Spell Perk Item Distributor",
        ModFile = "Spell Perk Item Distributor-36869-7-1-3-1713397024.7z",
        InstructionsAE =
        [
            new()
            {
                type = @"copy",
                source = @"Spell Perk Item Distributor FOMOD Installer\AE\SKSE\Plugins\po3_SpellPerkItemDistributor.dll",
                destination = @"SKSE\Plugins\po3_SpellPerkItemDistributor.dll",
            },
            new()
            {
                type = @"copy",
                source = @"Spell Perk Item Distributor FOMOD Installer\AE\SKSE\Plugins\po3_SpellPerkItemDistributor.pdb",
                destination = @"SKSE\Plugins\po3_SpellPerkItemDistributor.pdb",
            },
            new()
            {
                type = @"enableallplugins",
            },
        ],
        InstructionsSE =
        [
            new()
            {
                type = @"copy",
                source = @"Spell Perk Item Distributor FOMOD Installer\SE\SKSE\Plugins\po3_SpellPerkItemDistributor.dll",
                destination = @"SKSE\Plugins\po3_SpellPerkItemDistributor.dll",
            },
            new()
            {
                type = @"copy",
                source = @"Spell Perk Item Distributor FOMOD Installer\SE\SKSE\Plugins\po3_SpellPerkItemDistributor.pdb",
                destination = @"SKSE\Plugins\po3_SpellPerkItemDistributor.pdb",
            },
            new()
            {
                type = @"enableallplugins",
            },
        ],
    };

    public static SkyrimMod ModBetterMessageBoxControls = new()
    {
        Name = "Better MessageBox Controls",
        ModFile = "Better MessageBox Controls v1_2-1428-1-2.zip",
        InstructionsAE = [],
        InstructionsSE = [],
    };

    public static SkyrimMod ModXP32MaximumSkeletonSpecialExtended = new()
    {
        Name = "XP32 Maximum Skeleton Special Extended",
        ModFile = "XP32 Maximum Skeleton Special Extended-1988-5-06-1707663131.7z",
        InstructionsAE = [],
        InstructionsSE = [],
    };

    public static IArchive GetSkyrimMod(string mod)
    {
        var resource = typeof(InstallDataSource).Assembly.GetManifestResourceStream($"TestData.Data.Skyrim.{mod}")!;
        return ArchiveFactory.Open(resource);
    }

    public static InstallData BaseTest(SkyrimMod mod, string gameVersion, List<InstallInstruction> instructions, IEnumerable<SelectedOption>? dialogChoices, JsonDocument? preset, List<string> installedPlugins) => new()
    {
        Game = "Skyrim",
        Mod = mod.Name,
        ModArchive = GetSkyrimMod(mod.ModFile),
        StopPatterns = StopPatterns,
        PluginPath = "Data",
        AppVersion = "1.0.0",
        GameVersion = gameVersion,
        ExtenderVersion = "2.0.0",
        DialogChoices = dialogChoices,
        Preset = preset,
        InstalledPlugins = installedPlugins,
        Validate = true,
        Message = "Installation successful",
        Instructions = instructions,
    };

    public static InstallData TestAE(SkyrimMod mod, IEnumerable<SelectedOption>? dialogChoices, JsonDocument? preset, List<string> installedPlugins) =>
        BaseTest(mod, GameVersionAE, mod.InstructionsAE, dialogChoices, preset, installedPlugins);
    public static InstallData TestAEWithSEInstructions(SkyrimMod mod, IEnumerable<SelectedOption>? dialogChoices, JsonDocument? preset, List<string> installedPlugins) =>
        BaseTest(mod, GameVersionAE, mod.InstructionsSE, dialogChoices, preset, installedPlugins);

    public static InstallData TestSE(SkyrimMod mod, IEnumerable<SelectedOption>? dialogChoices, JsonDocument? preset, List<string> installedPlugins) =>
        BaseTest(mod, GameVersionSE, mod.InstructionsSE, dialogChoices, preset, installedPlugins);
    public static InstallData TestSEWithAEInstructions(SkyrimMod mod, IEnumerable<SelectedOption>? dialogChoices, JsonDocument? preset, List<string> installedPlugins) =>
        BaseTest(mod, GameVersionSE, mod.InstructionsAE, dialogChoices, preset, installedPlugins);

}

public partial class InstallDataSource
{
    public static IEnumerable<Func<InstallData>> SkyrimData()
    {
        //yield return () => TestAE(ModXP32MaximumSkeletonSpecialExtended, null, null, []);

        // No Preset and Choices - Unattended Mode
        // Uses Recommended
        yield return () => TestAE(ModSpellPerkItemDistributor, null, null, []);
        yield return () => TestSE(ModSpellPerkItemDistributor, null, null, []);

        // Choice - AE
        yield return () => TestAE(ModSpellPerkItemDistributor, [new(0, 0, [0])], null, []);
        yield return () => TestSEWithAEInstructions(ModSpellPerkItemDistributor, [new(0, 0, [0])], null, []);

        // Choice - SE
        yield return () => TestAEWithSEInstructions(ModSpellPerkItemDistributor, [new(0, 0, [1])], null, []);
        yield return () => TestSE(ModSpellPerkItemDistributor, [new(0, 0, [1])], null, []);

        // Preset - AE
        var presetAE = JsonDocument.Parse("[{\"name\":\"Main\",\"groups\":[{\"name\":\"DLL\",\"choices\":[{\"name\":\"SSE v1.6.629+ (\\\"Anniversary Edition\\\")\",\"idx\":0}]}]}]");
        yield return () => TestAE(ModSpellPerkItemDistributor, null, presetAE, []);
        yield return () => TestSEWithAEInstructions(ModSpellPerkItemDistributor, null, presetAE, []);

        // Preset - SE
        var presetSE = JsonDocument.Parse("[{\"name\":\"Main\",\"groups\":[{\"name\":\"DLL\",\"choices\":[{\"name\":\"SSE v1.5.97 (\\\"Special Edition\\\")\",\"idx\":1}]}]}]");
        yield return () => TestAEWithSEInstructions(ModSpellPerkItemDistributor, null, presetSE, []);
        yield return () => TestSE(ModSpellPerkItemDistributor, null, presetSE, []);
    }
}