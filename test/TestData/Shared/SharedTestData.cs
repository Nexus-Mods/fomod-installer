using SharpCompress.Archives;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestData.Shared;

/// <summary>
/// Shared test data structures that match the JSON schema.
/// Both C# and TypeScript tests use the same per-game JSON files.
/// </summary>

public sealed record SharedSelectedOption
{
    [JsonPropertyName("stepId")]
    public int StepId { get; init; }

    [JsonPropertyName("groupId")]
    public int GroupId { get; init; }

    [JsonPropertyName("pluginIds")]
    public int[] PluginIds { get; init; } = [];
}

public sealed record SharedInstruction
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("destination")]
    public string? Destination { get; init; }

    [JsonPropertyName("section")]
    public string? Section { get; init; }

    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("priority")]
    public int Priority { get; init; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }
}

public sealed record SharedTestCaseRaw
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("mod")]
    public string Mod { get; init; } = "";

    [JsonPropertyName("archiveFile")]
    public string ArchiveFile { get; init; } = "";

    [JsonPropertyName("pluginPath")]
    public string PluginPath { get; init; } = "";

    [JsonPropertyName("appVersion")]
    public string AppVersion { get; init; } = "1.0.0";

    [JsonPropertyName("gameVersion")]
    public string GameVersion { get; init; } = "";

    [JsonPropertyName("extenderVersion")]
    public string ExtenderVersion { get; init; } = "";

    [JsonPropertyName("installedPlugins")]
    public List<string> InstalledPlugins { get; init; } = [];

    [JsonPropertyName("dialogChoices")]
    public List<SharedSelectedOption>? DialogChoices { get; init; }

    [JsonPropertyName("preset")]
    public JsonElement? Preset { get; init; }

    [JsonPropertyName("validate")]
    public bool Validate { get; init; } = true;

    [JsonPropertyName("installerType")]
    public string? InstallerType { get; init; }

    [JsonPropertyName("expectedMessage")]
    public string ExpectedMessage { get; init; } = "Installation successful";

    [JsonPropertyName("expectedInstructions")]
    public List<SharedInstruction> ExpectedInstructions { get; init; } = [];
}

public sealed record SharedTestCase
{
    public string Name { get; init; } = "";
    public string Game { get; init; } = "";
    public string Mod { get; init; } = "";
    public string ArchiveFile { get; init; } = "";
    public List<string> StopPatterns { get; init; } = [];
    public string PluginPath { get; init; } = "";
    public string AppVersion { get; init; } = "1.0.0";
    public string GameVersion { get; init; } = "";
    public string ExtenderVersion { get; init; } = "";
    public List<string> InstalledPlugins { get; init; } = [];
    public List<SharedSelectedOption>? DialogChoices { get; init; }
    public JsonElement? Preset { get; init; }
    public bool Validate { get; init; } = true;
    public string? InstallerType { get; init; }
    public string ExpectedMessage { get; init; } = "Installation successful";
    public List<SharedInstruction> ExpectedInstructions { get; init; } = [];
}

public sealed record GameTestFile
{
    [JsonPropertyName("game")]
    public string Game { get; init; } = "";

    [JsonPropertyName("stopPatterns")]
    public List<string> StopPatterns { get; init; } = [];

    [JsonPropertyName("testCases")]
    public List<SharedTestCaseRaw> TestCases { get; init; } = [];
}

public static class SharedTestDataLoader
{
    private static List<SharedTestCase>? _cachedTestCases;
    private static readonly object _lock = new();

    // List of game files to load
    private static readonly string[] GameFiles =
    [
        "fallout4.json",
        "skyrim.json",
        "falloutnv.json",
        "fomod-compliance.json",
        "csharp-script.json"
    ];

    /// <summary>
    /// Loads all test cases from all per-game JSON files.
    /// </summary>
    public static List<SharedTestCase> LoadAll()
    {
        lock (_lock)
        {
            if (_cachedTestCases != null)
                return _cachedTestCases;

            _cachedTestCases = [];
            var assembly = typeof(SharedTestDataLoader).Assembly;
            var assemblyDir = Path.GetDirectoryName(assembly.Location)!;

            foreach (var gameFile in GameFiles)
            {
                var gameData = LoadGameFile(assembly, assemblyDir, gameFile);
                if (gameData != null)
                {
                    // Expand test cases with game and stopPatterns
                    foreach (var tc in gameData.TestCases)
                    {
                        _cachedTestCases.Add(new SharedTestCase
                        {
                            Name = tc.Name,
                            Game = gameData.Game,
                            Mod = tc.Mod,
                            ArchiveFile = tc.ArchiveFile,
                            StopPatterns = gameData.StopPatterns,
                            PluginPath = tc.PluginPath,
                            AppVersion = tc.AppVersion,
                            GameVersion = tc.GameVersion,
                            ExtenderVersion = tc.ExtenderVersion,
                            InstalledPlugins = tc.InstalledPlugins,
                            DialogChoices = tc.DialogChoices,
                            Preset = tc.Preset,
                            Validate = tc.Validate,
                            InstallerType = tc.InstallerType,
                            ExpectedMessage = tc.ExpectedMessage,
                            ExpectedInstructions = tc.ExpectedInstructions
                        });
                    }
                }
            }

            return _cachedTestCases;
        }
    }

    private static GameTestFile? LoadGameFile(Assembly assembly, string assemblyDir, string filename)
    {
        // Try embedded resource first
        var resourceName = $"TestData.Shared.{filename}";
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream != null)
        {
            return JsonSerializer.Deserialize<GameTestFile>(stream);
        }

        // Try file path as fallback
        var filePath = Path.Combine(assemblyDir, "..", "..", "..", "..", "Shared", filename);
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<GameTestFile>(json);
        }

        return null;
    }

    /// <summary>
    /// Gets the stop patterns for a test case.
    /// </summary>
    public static List<string> GetStopPatterns(SharedTestCase testCase)
    {
        return testCase.StopPatterns;
    }

    /// <summary>
    /// Converts a SharedTestCase to the existing InstallData format for compatibility.
    /// </summary>
    public static InstallData ToInstallData(SharedTestCase testCase, Func<string, string, IArchive> archiveLoader)
    {
        return new InstallData
        {
            Game = testCase.Game,
            Mod = testCase.Mod,
            ModArchive = archiveLoader(testCase.Game, testCase.ArchiveFile),
            StopPatterns = testCase.StopPatterns,
            PluginPath = testCase.PluginPath,
            AppVersion = testCase.AppVersion,
            GameVersion = testCase.GameVersion,
            ExtenderVersion = testCase.ExtenderVersion,
            InstalledPlugins = testCase.InstalledPlugins,
            DialogChoices = testCase.DialogChoices?.Select(dc => new SelectedOption(dc.StepId, dc.GroupId, dc.PluginIds)),
            Preset = testCase.Preset.HasValue ? JsonDocument.Parse(testCase.Preset.Value.GetRawText()) : null,
            Validate = testCase.Validate,
            Message = testCase.ExpectedMessage,
            Instructions = testCase.ExpectedInstructions.Select(i => new InstallInstruction
            {
                type = i.Type,
                source = i.Source,
                destination = i.Destination,
                section = i.Section,
                key = i.Key,
                value = i.Value,
                priority = i.Priority,
                data = i.Data.HasValue ? JsonDocument.Parse(i.Data.Value.GetRawText()) : null
            }).ToList()
        };
    }

    /// <summary>
    /// Gets all test cases that use .zip archives (for environments without 7z support).
    /// </summary>
    public static IEnumerable<SharedTestCase> GetZipTestCases()
    {
        return LoadAll().Where(tc => tc.ArchiveFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all test cases that use .7z archives.
    /// </summary>
    public static IEnumerable<SharedTestCase> Get7zTestCases()
    {
        return LoadAll().Where(tc => tc.ArchiveFile.EndsWith(".7z", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all test cases (both .zip and .7z).
    /// </summary>
    public static IEnumerable<SharedTestCase> GetAllTestCases()
    {
        return LoadAll();
    }

    /// <summary>
    /// Gets test cases filtered by game.
    /// </summary>
    public static IEnumerable<SharedTestCase> GetTestCasesByGame(string game)
    {
        return LoadAll().Where(tc => tc.Game.Equals(game, StringComparison.OrdinalIgnoreCase));
    }
}
