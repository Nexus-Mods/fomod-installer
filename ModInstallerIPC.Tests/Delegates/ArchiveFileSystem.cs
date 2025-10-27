using SharpCompress.Archives;
using TestData;
using Utils;

namespace ModInstallerIPC.Tests.Delegates;

public class ArchiveFileSystem : IFileSystem
{
    private readonly IArchive _mod;

    public ArchiveFileSystem(IArchive mod)
    {
        _mod = mod;
    }

    public byte[]? ReadFileContent(string path, int offset, int length)
    {
        // Normalize the requested path to use backslashes
        var normalizedPath = path.Replace("/", "\\");
        var entry = _mod.Entries.FirstOrDefault(x => x.GetNormalizedName() == normalizedPath);
        if (entry == null) return null;

        Stream? stream = null;
        try
        {
            stream = entry.OpenEntryStream();

            if (length == -1) length = (int) entry.Size;

            if (length == 0)
            {
                return [];
            }

            if (offset > 0)
            {
                var discard = new byte[offset];
                stream.ReadExactly(discard);
            }

            var buffer = new byte[length];
            stream.ReadExactly(buffer);
            return buffer;
        }
        catch
        {
            return null;
        }
        finally
        {
            stream?.Dispose();
        }
    }

    public string[] ReadDirectoryFileList(string directoryPath, string pattern, SearchOption searchOption)
    {
        return _mod.Entries
            .Where(x => !x.IsDirectory && x.GetNormalizedName()?.StartsWith(directoryPath) == true)
            .Select(x => x.Key)
            .OfType<string>()
            .ToArray();
    }

    public string[] ReadDirectoryList(string directoryPath)
    {
        return _mod.Entries
            .Where(x => x.IsDirectory && x.GetNormalizedName()?.StartsWith(directoryPath) == true)
            .Select(x => x.Key)
            .OfType<string>()
            .ToArray();
    }
}
