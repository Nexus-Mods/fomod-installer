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
        var key = entry.Key;
        if (key == null) return null;

        // For directories, ensure trailing separator (but don't double it if already present)
        if (entry.IsDirectory && !key.EndsWith("/") && !key.EndsWith("\\"))
        {
            key += "/";
        }

        // Normalize all separators to platform-specific
        return key.Replace("/", Path.DirectorySeparatorChar.ToString())
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