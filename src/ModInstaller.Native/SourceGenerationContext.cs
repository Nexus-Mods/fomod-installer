using FomodInstaller.Interface;
using FomodInstaller.Interface.snapshot;
using FomodInstaller.Interface.ui;

using ModInstaller.Lite;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

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
[JsonSerializable(typeof(InstallSnapshot))]
[JsonSerializable(typeof(StepSnapshot))]
[JsonSerializable(typeof(GroupSnapshot))]
[JsonSerializable(typeof(OptionSnapshot))]
[JsonSerializable(typeof(ResolverSnapshot))]
[JsonSerializable(typeof(PatternSnapshot))]
[JsonSerializable(typeof(ConditionSnapshot))]
[JsonSerializable(typeof(FlagSnapshot))]
internal partial class SourceGenerationContext : JsonSerializerContext;