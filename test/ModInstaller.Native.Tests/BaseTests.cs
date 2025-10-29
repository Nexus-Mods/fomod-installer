using FluentAssertions;
using static ModInstaller.Native.Tests.Utils.Utils2;

namespace ModInstaller.Native.Tests;

public class BaseTests : IDisposable
{
    protected BaseTests()
    {
        LibrarySetAllocator();
    }

    public void Dispose()
    {
        LibraryAliveCount().Should().Be(0);
    }
}