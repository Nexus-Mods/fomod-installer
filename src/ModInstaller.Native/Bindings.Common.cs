using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace ModInstaller.Native;

public static partial class Bindings
{
    private static readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        IncludeFields = false,
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin),
        Converters =
        {
            //new InstallInstructionJsonConverter(),
            //new JsonStringEnumConverter<DialogType>(),
        }
    };

    internal static readonly SourceGenerationContext CustomSourceGenerationContext = new(_options);
}