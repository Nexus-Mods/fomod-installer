using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BUTR.NativeAOT.Shared;
using FluentAssertions;
using ModInstaller.Lite;
using ModInstaller.Native.Tests.Extensions;
using NUnit.Framework;
using TestData;
using static ModInstaller.Native.Tests.Utils.Utils2;

namespace ModInstaller.Native.Tests;

using TestClass = IEnumerable<TestSupportData>;

public sealed partial class TestSupportedTests : BaseTests
{
    [LibraryImport(DllPath), UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe partial return_value_json* test_supported(param_json* p_mod_archive_file_list, param_json* p_allowed_types);
    
    public static TestClass BasicData() => TestSupportDataSource.BasicData().NUnit();
    public static TestClass XmlData() => TestSupportDataSource.XmlData().NUnit();
    public static TestClass LiteData() => TestSupportDataSource.LiteData().NUnit();

    [Test]
    [TestCaseSource(nameof(BasicData))]
    [TestCaseSource(nameof(XmlData))]
    [TestCaseSource(nameof(LiteData))]
    public unsafe void Test(TestSupportData data)
    {
        {
            using var modArchiveFileListJson = ToJson(data.ModArchiveFileList);
            using var allowedTypesJson = ToJson(data.AllowedTypes);

            var result = GetResult<SupportedResult>(test_supported(modArchiveFileListJson, allowedTypesJson));

            result.RequiredFiles.Order().Should().BeEquivalentTo(data.RequiredFiles.Order());
            result.Supported.Should().Be(result.Supported);
        }
        
        LibraryAliveCount().Should().Be(0);
    }
}