using System.Collections.Generic;

namespace ModInstaller.Lite;

public record SupportedResult
{
    public static SupportedResult AsNotSupported { get; } = new()
    {
        Supported = false,
        RequiredFiles = new()
    };
    public static SupportedResult AsSupported { get; } = new()
    {
        Supported = true,
        RequiredFiles = new()
    };
    public static SupportedResult AsSupportedWithFiles(List<string> requiredFiles) => new()
    {
        Supported = true,
        RequiredFiles = requiredFiles
    };

    public bool Supported { get; set; }
    public List<string> RequiredFiles { get; set; } = new();
}