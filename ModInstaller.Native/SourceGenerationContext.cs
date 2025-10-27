using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;
using ModInstaller.Lite;

namespace ModInstaller.Native;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    IgnoreReadOnlyFields = true,
    IgnoreReadOnlyProperties = true,
    IncludeFields = true,
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]

[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(SupportedResult))]
[JsonSerializable(typeof(Instruction))]
[JsonSerializable(typeof(InstallResult))]
[JsonSerializable(typeof(JsonDocument))]
[JsonSerializable(typeof(InstallerStep[]))]
[JsonSerializable(typeof(HeaderImage))]
internal partial class SourceGenerationContext : JsonSerializerContext;
