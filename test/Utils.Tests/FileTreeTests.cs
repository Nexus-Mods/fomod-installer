using System.Linq;
using System.Threading.Tasks;

namespace Utils.Tests;

public class FileTreeTests
{
    [Test]
    public async Task ParsesInputTree()
    {
        var tree = new FileTree(
        [
            @"top\middle\end\",
            @"top\middle\end\somefile.txt",
            @"toplevel.txt",
        ]);

        await Assert.That(tree.Files).IsEquivalentTo(["toplevel.txt"]);
        await Assert.That(tree.SubDirectories.Select(dir => dir.Name)).IsEquivalentTo(["top"]);
        await Assert.That(tree.SubDirectories.First().Files).IsEmpty();
        await Assert.That(tree.SubDirectories.First().SubDirectories.Select(dir => dir.Name)).IsEquivalentTo(["middle"]);
    }

    [Test]
    public async Task AlsoWorksWithSlashes()
    {
        var tree = new FileTree(
        [
            @"top/middle/end/",
            @"top/middle/end/somefile.txt",
            @"toplevel.txt",
        ]);

        await Assert.That(tree.Files).IsEquivalentTo(["toplevel.txt"]);
        await Assert.That(tree.SubDirectories.Select(dir => dir.Name)).IsEquivalentTo(["top"]);
    }

    [Test]
    public async Task CanSelectToplevel()
    {
        var tree = new FileTree(
        [
            @"top\middle\end\",
            @"top\middle\end\somefile.txt",
            @"toplevel.txt",
        ]);

        await Assert.That(tree.SelectDirectory("/").Files).IsEquivalentTo(["toplevel.txt"]);
    }


    [Test]
    public async Task CanSelectAnywhere()
    {
        var tree = new FileTree(
        [
            @"top\middle\end\",
            @"top\middle\end\somefile.txt",
            @"toplevel.txt",
        ]);

        await Assert.That(tree.SelectDirectory("/top/middle").SubDirectories.Select(node => node.Name)).IsEquivalentTo(["end"]);
    }

    [Test]
    public async Task DirectoriesDontHaveToExist()
    {
        var tree = new FileTree(
        [
            @"top/middle/end/somefile.txt",
        ]);

        await Assert.That(tree.SelectDirectory("/top/middle/end").Files).IsEquivalentTo(["somefile.txt"]);
    }
}