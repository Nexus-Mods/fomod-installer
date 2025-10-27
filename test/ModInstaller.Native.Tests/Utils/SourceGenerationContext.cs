using System.Text.Json;
using System.Text.Json.Serialization;
using FomodInstaller.Interface.ui;
using ModInstaller.Lite;

namespace ModInstaller.Native.Tests.Utils;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    IgnoreReadOnlyFields = true,
    IgnoreReadOnlyProperties = true,
    IncludeFields = false,
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]

[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(SupportedResult))]
[JsonSerializable(typeof(InstallResult))]
[JsonSerializable(typeof(JsonDocument))]
[JsonSerializable(typeof(InstallerStep[]))]
internal partial class SourceGenerationContext : JsonSerializerContext;