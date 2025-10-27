using System;
using System.Collections.Generic;
using System.Text.Json;
using FomodInstaller.Interface;
using SharpCompress.Archives;

namespace TestData;

using static FomodComplianceTestData;

file class FomodComplianceTestData
{
    public record FomodComplianceMod
    {
        public required string ModFile { get; init; }
        public required List<Instruction> DefaultInstructions { get; init; }
    }

    public static List<string> StopPatterns =
    [
        "(^|/)fomod(/|$)",
    ];

    public static FomodComplianceMod ModFomodComplianceTests = new()
    {
        ModFile = "FomodComplianceTests.zip",
        DefaultInstructions =
        [
            // ACTUAL unattended mode behavior (15 instructions total):
            // The installer selects files based on a complex priority system:
            // 1. Conditional files with no dependencies (priority 1000000000)
            // 2. Files with installIfUsable="true" from usable plugins
            // 3. Files with alwaysInstall="true"
            // 4. Files from Required plugins in SelectAtMostOne groups with multiple Required options
            // 5. Some files from Required plugins in SelectAll groups (partial selection)
            // 6. Folder contents from selected plugins
            // Note: requiredInstallFiles section is NOT installed in unattended mode!

            // Conditional files (priority 1000000000)
            new()
            {
                type = @"copy",
                source = @"conditionals\test_conditionals_ignore_name.txt",
                destination = @"test_conditionals_ignore_name.txt",
                priority = 1000000000,
            },
            new()
            {
                type = @"copy",
                source = @"conditionals\test_conditionals_ignore_priority.txt",
                destination = @"test_conditionals_ignore_priority.txt",
                priority = 1000000000,
            },
            new()
            {
                type = @"copy",
                source = @"conditionals\test_conditionals_ignore_required_priority.txt",
                destination = @"test_conditionals_ignore_required_priority.txt",
                priority = 1000000000,
            },
            // Group 1 Plugin 1 files (partial - Required plugin in SelectAll group)
            new()
            {
                type = @"copy",
                source = @"g1p1_alwayson\test_pluginorder.txt",
                destination = @"test_pluginorder.txt",
                priority = 0,
            },
            new()
            {
                type = @"copy",
                source = @"g1p1_alwayson\test_priority.txt",
                destination = @"test_priority_over_alwaysinstall.txt",
                priority = 5,
            },
            new()
            {
                type = @"copy",
                source = @"g1p1_alwayson\test_priority.txt",
                destination = @"test_priority.txt",
                priority = 5,
            },
            new()
            {
                type = @"copy",
                source = @"g1p1_alwayson\test_steps_ignore_required_priority.txt",
                destination = @"test_steps_ignore_required_priority.txt",
                priority = 0,
            },
            // Group 2 Plugin 1 files (Required plugin in SelectAll group)
            new()
            {
                type = @"copy",
                source = @"g2p1_alwayson\test_grouporder.txt",
                destination = @"test_grouporder.txt",
                priority = 0,
            },
            // Group 3 Plugin 1 - alwaysInstall file (NotUsable plugin)
            new()
            {
                type = @"copy",
                source = @"g3p1_notusable\test_alwaysinstall.txt",
                destination = @"test_alwaysinstall.txt",
                priority = 5,
            },
            // Group 4 Plugin 1 - installIfUsable file (Optional plugin in SelectAtMostOne)
            new()
            {
                type = @"copy",
                source = @"g4p1_leavedisabled\test_installifusable.txt",
                destination = @"test_installifusable.txt",
                priority = 3,
            },
            // Group 6 - Both Required plugins in SelectAtMostOne group get installed
            new()
            {
                type = @"copy",
                source = @"g6p1_selectall\test_required1.txt",
                destination = @"test_required1.txt",
                priority = 5,
            },
            new()
            {
                type = @"copy",
                source = @"g6p2_selectall\test_required2.txt",
                destination = @"test_required2.txt",
                priority = 0,
            },
            // Folder contents from Group 1 Plugin 1
            new()
            {
                type = @"copy",
                source = @"shared\folder_aaa\test_filebeforefolder.txt",
                destination = @"test_filebeforefolder.txt",
                priority = 0,
            },
            new()
            {
                type = @"copy",
                source = @"shared\folder_zzz\test_alphabetical.txt",
                destination = @"test_alphabetical.txt",
                priority = 0,
            },
            new()
            {
                type = @"enableallplugins",
                priority = 0,
            },
        ],
    };

    public static IArchive GetFomodComplianceTestsMod(string mod)
    {
        var resource = typeof(InstallDataSource).Assembly.GetManifestResourceStream($"TestData.Data.{mod}")!;
        return ArchiveFactory.Open(resource);
    }

    public static InstallData BaseTest(FomodComplianceMod mod, List<Instruction> instructions, IEnumerable<SelectedOption>? dialogChoices, JsonDocument? preset, List<string> installedPlugins) => new()
    {
        Game = "FOMOD",
        Mod = "FOMOD",
        ModArchive = GetFomodComplianceTestsMod(mod.ModFile),
        StopPatterns = StopPatterns,
        PluginPath = "Data",
        AppVersion = "1.0.0",
        GameVersion = "1.0.0",
        ExtenderVersion = "1.0.0",
        DialogChoices = dialogChoices,
        Preset = preset,
        InstalledPlugins = installedPlugins,
        Validate = true,
        Message = "Installation successful",
        Instructions = instructions,
    };
}

public partial class InstallDataSource
{
    public static IEnumerable<Func<InstallData>> FomodComplianceTestsData()
    {
        // Unattended mode - no choices, no preset
        yield return () => BaseTest(ModFomodComplianceTests, ModFomodComplianceTests.DefaultInstructions, null, null, []);

        // TODO: Add explicit choice tests to properly test FOMOD compliance features
        // - Test with specific plugin selections
        // - Test priority ordering
        // - Test conditional file installations
        // - Test alwaysInstall and installIfUsable flags
    }
}
