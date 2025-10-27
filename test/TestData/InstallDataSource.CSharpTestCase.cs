using System;
using System.Collections.Generic;
using System.Text.Json;
using FomodInstaller.Interface;
using SharpCompress.Archives;

namespace TestData;

using static CSharpTestCaseTestData;

file class CSharpTestCaseTestData
{
    public record CSharpTestCaseMod
    {
        public required string ModFile { get; init; }
        public required List<Instruction> DefaultInstructions { get; init; }
    }

    public static List<string> StopPatterns =
    [
        "(^|/)fomod(/|$)",
    ];

    public static CSharpTestCaseMod ModCSharpTestCase = new()
    {
        ModFile = "CSharpTestCase.zip",
        DefaultInstructions =
        [
            // C# script generates a dummy.esp file with content "abc" (bytes 0x61, 0x62, 0x63)
            new()
            {
                type = @"generatefile",
                destination = @"dummy.esp",
                value = "YWJj", // Base64 encoded "abc" bytes
            },
            new()
            {
                type = @"enableallplugins",
            },
        ],
    };

    public static IArchive GetCSharpTestCaseMod(string mod)
    {
        var resource = typeof(InstallDataSource).Assembly.GetManifestResourceStream($"TestData.Data.{mod}")!;
        return ArchiveFactory.Open(resource);
    }

    public static InstallData BaseTest(CSharpTestCaseMod mod, List<Instruction> instructions, IEnumerable<SelectedOption>? dialogChoices, JsonDocument? preset, List<string> installedPlugins) => new()
    {
        Game = "C#",
        Mod = "C#",
        ModArchive = GetCSharpTestCaseMod(mod.ModFile),
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
    public static IEnumerable<Func<InstallData>> CSharpTestCaseData()
    {
        // C# script execution - no choices needed
        yield return () => BaseTest(ModCSharpTestCase, ModCSharpTestCase.DefaultInstructions, null, null, []);
    }
}
