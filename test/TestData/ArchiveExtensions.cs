using System.Collections.Generic;
using System.Linq;
using FomodInstaller.Interface;
using SharpCompress.Archives;

namespace TestData;

public static class ArchiveExtensions
{
    public static string? GetNormalizedName(this IArchiveEntry entry)
    {
        return (entry.IsDirectory ? $"{entry.Key}/" : entry.Key)?.Replace("/", "\\");
    }
    
    public static List<Instruction> Order(this IEnumerable<Instruction> instructions)
    {
        return instructions.OrderBy(x => x.type).ThenBy(x => x.source).ThenBy(x => x.destination).ToList();
    }
}