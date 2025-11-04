using FomodInstaller.ModInstaller;

using TestData;

namespace ModInstaller.Adaptor.Dynamic.Tests;

using TestClass = IEnumerable<Func<TestSupportData>>;

public class TestSupportedTests
{
    public static TestClass BasicData() => TestSupportDataSource.BasicData();
    public static TestClass CSharpData() => TestSupportDataSource.CSharpData();
    public static TestClass XmlData() => TestSupportDataSource.XmlData();
    public static TestClass DynamicData() => TestSupportDataSource.DynamicData();

    [Test]
    [MethodDataSource(nameof(BasicData))]
    [MethodDataSource(nameof(CSharpData))]
    [MethodDataSource(nameof(XmlData))]
    [MethodDataSource(nameof(DynamicData))]
    public async Task Test(TestSupportData data)
    {
        var installer = new Installer();
        var result = await installer.TestSupported(data.ModArchiveFileList, data.AllowedTypes);

        await Assert.That(result["supported"]).IsEquivalentTo(data.Supported);
        await Assert.That(result["requiredFiles"]).IsEquivalentTo(data.RequiredFiles);
    }
}