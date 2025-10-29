using ModInstaller.IPC.Tests.Utils;
using TestData;

namespace ModInstaller.IPC.Tests;

using TestClass = IEnumerable<Func<TestSupportData>>;

public class TestSupportedTests
{
    public static TestClass SkyrimData() => TestSupportDataSource.SkyrimData();
    public static TestClass BasicData() => TestSupportDataSource.BasicData();
    public static TestClass XmlData() => TestSupportDataSource.XmlData();
    public static TestClass LiteData() => TestSupportDataSource.LiteData();

    [Test]
    [MethodDataSource(nameof(SkyrimData))]
    [MethodDataSource(nameof(BasicData))]
    [MethodDataSource(nameof(XmlData))]
    [MethodDataSource(nameof(LiteData))]
    [NotInParallel]
    public async Task Test(TestSupportData data)
    {
        await using var harness = await new IPCTestHarness(data).InitializeAsync();

        var result = await harness.TestSupportedAsync(data.ModArchiveFileList, data.AllowedTypes);

        await Assert.That(result.Supported).IsEqualTo(data.Supported);
        await Assert.That(result.RequiredFiles.Order()).IsEquivalentTo(data.RequiredFiles.Order());
    }
}
