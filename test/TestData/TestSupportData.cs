using System.Collections.Generic;

namespace TestData;

public sealed record TestSupportData(
    List<string> ModArchiveFileList,
    List<string> AllowedTypes,
    bool Supported,
    List<string> RequiredFiles)
{
    public override string ToString()
    {
        return $"[{string.Join(", ", ModArchiveFileList)}] - [{string.Join(", ", AllowedTypes)}] => {Supported} - [{string.Join(", ", RequiredFiles)}]";
    }
}