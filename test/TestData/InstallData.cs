using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using FomodInstaller.Interface;
using SharpCompress.Archives;

namespace TestData;

public sealed record SelectedOption
{
    public required int StepId { get; init; }
    public required int GroupId { get; init; }
    public required int[] PluginIds { get; init; }
    
    public SelectedOption() { }
    
    [SetsRequiredMembers]
    public SelectedOption(int stepId, int groupId, int[] pluginIds)
    {
        StepId = stepId;
        GroupId = groupId;
        PluginIds = pluginIds;
    }
}

public sealed record InstallData
{
    public required string Game { get; init; }
    public required string Mod { get; init; }
    public required IArchive ModArchive { get; init; }
    public required List<string> StopPatterns { get; init; }
    public required string PluginPath { get; init; }
    public required IEnumerable<SelectedOption>? DialogChoices { get; init; }
    public required JsonDocument? Preset { get; init; }
    public required List<string> InstalledPlugins { get; init; }
    public required string AppVersion { get; init; }
    public required string GameVersion { get; init; }
    public required string ExtenderVersion { get; init; }
    public required bool Validate { get; init; }
    public required string Message { get; init; }
    public required List<InstallInstruction> Instructions { get; init; }

    public override string ToString()
    {
        var type = "[NONE]";
        if (DialogChoices != null)
            type = $"[DC: {string.Join(", ", DialogChoices.Select(x => $"{x.StepId};{x.GroupId};{string.Join(",", x.PluginIds)}"))}]";
        if (Preset != null)
            type = $"[P: {ToJsonString(Preset)}]";
        
        return $"{Game}({GameVersion}) - {Mod} - {type}";
    }
    
    private static string ToJsonString(JsonDocument jdoc)
    {
        using var stream = new MemoryStream();
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });
        jdoc.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /*
    public override string ToString()
    {
        return $"[{string.Join(", ", ModArchiveFileList)}] - [{string.Join(", ", StopPatterns)}] => {Supported} - [{string.Join(", ", RequiredFiles)}]";
    }
    */
}