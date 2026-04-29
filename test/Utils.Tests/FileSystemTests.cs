using System.Threading.Tasks;

namespace Utils.Tests;

public class FileSystemTests
{
    // Linux: '../' uses DirectorySeparatorChar '/' - traversal detected - blocked
    [Test]
    public async Task ForwardSlashTraversalIsBlocked()
    {
        await Assert.That(Utils.FileSystem.IsSafeFilePath("../foo")).IsEqualTo(false);
    }

    // Linux: '..\' uses backslash which is NOT a separator on Linux.
    // Backslash is a valid filename character on ext4.
    // IsSafeFilePath correctly returns true - this is intentional platform-correct behavior.
    [Test]
    public async Task BackslashIsNotTraversalOnLinux()
    {
        await Assert.That(Utils.FileSystem.IsSafeFilePath("..\\foo")).IsEqualTo(true);
    }

    // Embedded forward-slash traversal mid-path
    [Test]
    public async Task EmbeddedForwardSlashTraversalIsBlocked()
    {
        await Assert.That(Utils.FileSystem.IsSafeFilePath("foo/../bar")).IsEqualTo(false);
    }

    // Embedded backslash sequence mid-path - valid filename on Linux
    [Test]
    public async Task EmbeddedBackslashIsNotTraversalOnLinux()
    {
        await Assert.That(Utils.FileSystem.IsSafeFilePath("foo/..\\bar")).IsEqualTo(true);
    }

    // Rooted absolute path - always blocked
    [Test]
    public async Task RootedPathIsBlocked()
    {
        await Assert.That(Utils.FileSystem.IsSafeFilePath("/absolute/path")).IsEqualTo(false);
    }

    // Normal relative path - allowed
    [Test]
    public async Task NormalRelativePathIsAllowed()
    {
        await Assert.That(Utils.FileSystem.IsSafeFilePath("Data/Textures/foo.dds")).IsEqualTo(true);
    }
}
