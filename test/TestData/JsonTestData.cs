using SharpCompress.Archives;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestData;

/// <summary>
/// JSON schema types for loading shared test data from JSON files.
/// These match the install-test-schema.json format.
/// </summary>

public record JsonSelectedOption
{
    [JsonPropertyName("stepId")]
    public int StepId { get; init; }

    [JsonPropertyName("groupId")]
    public int GroupId { get; init; }

    [JsonPropertyName("pluginIds")]
    public int[] PluginIds { get; init; } = [];
}

public record JsonInstruction
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

public record JsonTestCase
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
    public string? AppVersion { get; init; }

    [JsonPropertyName("gameVersion")]
    public string? GameVersion { get; init; }

    [JsonPropertyName("extenderVersion")]
    public string? ExtenderVersion { get; init; }

    [JsonPropertyName("installedPlugins")]
    public string[]? InstalledPlugins { get; init; }

    [JsonPropertyName("dialogChoices")]
    public JsonSelectedOption[]? DialogChoices { get; init; }

    [JsonPropertyName("preset")]
    public JsonElement? Preset { get; init; }

    [JsonPropertyName("validate")]
    public bool? Validate { get; init; }

    [JsonPropertyName("installerType")]
    public string? InstallerType { get; init; }

    [JsonPropertyName("expectedMessage")]
    public string? ExpectedMessage { get; init; }

    [JsonPropertyName("expectedInstructions")]
    public JsonInstruction[] ExpectedInstructions { get; init; } = [];
}

public record JsonGameTestFile
{
    [JsonPropertyName("game")]
    public string Game { get; init; } = "";

    [JsonPropertyName("stopPatterns")]
    public string[] StopPatterns { get; init; } = [];

    [JsonPropertyName("testCases")]
    public JsonTestCase[] TestCases { get; init; } = [];
}

/// <summary>
/// Loader for shared JSON test data files.
/// </summary>
public static class JsonTestDataLoader
{
    private static readonly string SharedDataDir = Path.Combine(
        Path.GetDirectoryName(typeof(JsonTestDataLoader).Assembly.Location)!,
        "Shared");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Loads a game test file from the Shared directory.
    /// </summary>
    public static JsonGameTestFile? LoadGameFile(string filename)
    {
        var filePath = Path.Combine(SharedDataDir, filename);
        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<JsonGameTestFile>(json, JsonOptions);
    }

    /// <summary>
    /// Gets the archive for a test case from embedded resources.
    /// </summary>
    public static IArchive GetArchive(string game, string archiveFile)
    {
        // Try game-specific path first
        var resourceName = $"TestData.Data.{game}.{archiveFile}";
        var stream = typeof(JsonTestDataLoader).Assembly.GetManifestResourceStream(resourceName);

        // Fall back to root Data folder
        if (stream == null)
        {
            resourceName = $"TestData.Data.{archiveFile}";
            stream = typeof(JsonTestDataLoader).Assembly.GetManifestResourceStream(resourceName);
        }

        if (stream == null)
            throw new FileNotFoundException($"Archive not found in embedded resources: {archiveFile} (tried {resourceName})");

        return ArchiveFactory.Open(stream);
    }

    /// <summary>
    /// Converts a JsonTestCase to InstallData for use in tests.
    /// </summary>
    public static InstallData ToInstallData(JsonTestCase testCase, string game, List<string> stopPatterns)
    {
        var archive = GetArchive(game, testCase.ArchiveFile);

        JsonDocument? preset = null;
        if (testCase.Preset.HasValue && testCase.Preset.Value.ValueKind != JsonValueKind.Undefined)
        {
            preset = JsonDocument.Parse(testCase.Preset.Value.GetRawText());
        }

        return new InstallData
        {
            Game = game,
            Mod = testCase.Mod,
            ModArchive = archive,
            StopPatterns = stopPatterns,
            PluginPath = testCase.PluginPath,
            AppVersion = testCase.AppVersion ?? "1.0.0",
            GameVersion = testCase.GameVersion ?? "1.0.0",
            ExtenderVersion = testCase.ExtenderVersion ?? "",
            DialogChoices = testCase.DialogChoices?.Select(c => new SelectedOption(c.StepId, c.GroupId, c.PluginIds)),
            Preset = preset,
            InstalledPlugins = testCase.InstalledPlugins?.ToList() ?? [],
            Validate = testCase.Validate ?? true,
            Message = testCase.ExpectedMessage ?? "Installation successful",
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
            }).ToList(),
        };
    }

    /// <summary>
    /// Loads all test cases from a game file and converts them to InstallData.
    /// </summary>
    public static IEnumerable<Func<InstallData>> LoadTestCases(string filename)
    {
        var gameFile = LoadGameFile(filename);
        if (gameFile == null)
            yield break;

        var stopPatterns = gameFile.StopPatterns.ToList();

        foreach (var testCase in gameFile.TestCases)
        {
            // Capture for closure
            var tc = testCase;
            var game = gameFile.Game;
            yield return () => ToInstallData(tc, game, stopPatterns);
        }
    }

    /// <summary>
    /// Loads all test cases from a game file, optionally filtering by installer type.
    /// </summary>
    public static IEnumerable<Func<InstallData>> LoadTestCases(string filename, string? installerTypeFilter)
    {
        var gameFile = LoadGameFile(filename);
        if (gameFile == null)
            yield break;

        var stopPatterns = gameFile.StopPatterns.ToList();

        foreach (var testCase in gameFile.TestCases)
        {
            // Filter by installer type if specified
            if (installerTypeFilter != null && testCase.InstallerType != installerTypeFilter)
                continue;

            // Capture for closure
            var tc = testCase;
            var game = gameFile.Game;
            yield return () => ToInstallData(tc, game, stopPatterns);
        }
    }
}
