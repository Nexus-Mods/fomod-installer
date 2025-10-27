using ModInstaller.Lite;
using TestData;

namespace ModInstaller.Adaptor.Typed.Tests;

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
        var result = Installer.TestSupported(data.ModArchiveFileList, data.AllowedTypes);
        
        await Assert.That(result).IsEquivalentTo(new SupportedResult()
        {
            Supported = data.Supported,
            RequiredFiles = data.RequiredFiles
        });
    }
}