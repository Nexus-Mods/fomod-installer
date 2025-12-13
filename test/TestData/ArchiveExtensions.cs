using FomodInstaller.Interface;

using SharpCompress.Archives;

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestData;

public static class ArchiveExtensions
{
    // Normalize archive paths to use platform-specific path separators
    // Archive entries typically use "/" but Windows code expects "\"
    public static string? GetNormalizedName(this IArchiveEntry entry)
    {
        var name = entry.IsDirectory ? $"{entry.Key}/" : entry.Key;
        return name?.Replace("/", Path.DirectorySeparatorChar.ToString())
                    .Replace("\\", Path.DirectorySeparatorChar.ToString());
    }

    public static List<Instruction> Order(this IEnumerable<Instruction> instructions)
    {
        return instructions.OrderBy(x => x.type).ThenBy(x => x.source).ThenBy(x => x.destination).ToList();
    }

    public static List<Instruction> Order(this IEnumerable<InstallInstruction> instructions)
    {
        return instructions.ToInstruction().OrderBy(x => x.type).ThenBy(x => x.source).ThenBy(x => x.destination).ToList();
    }
}