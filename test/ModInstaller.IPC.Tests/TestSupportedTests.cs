using ModInstaller.IPC.Tests.Utils;
using TestData;

namespace ModInstaller.IPC.Tests;

using TestClass = IEnumerable<Func<TestSupportData>>;

public class TestSupportedTests
{
    public static TestClass BasicData() => TestSupportDataSource.BasicData();
    public static TestClass XmlData() => TestSupportDataSource.XmlData();
    public static TestClass LiteData() => TestSupportDataSource.LiteData();

    [Test]
    [MethodDataSource(nameof(BasicData))]
    [MethodDataSource(nameof(XmlData))]
    [MethodDataSource(nameof(LiteData))]
    public async Task Test(TestSupportData data)
    {
        await using var harness = await new IPCTestHarness().InitializeAsync();

        var result = await harness.TestSupportedAsync(data.ModArchiveFileList, data.AllowedTypes);

        await Assert.That(result.Supported).IsEqualTo(data.Supported);
        await Assert.That(result.RequiredFiles.Order()).IsEquivalentTo(data.RequiredFiles.Order());
    }
}
