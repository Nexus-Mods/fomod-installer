using FomodInstaller.Interface;
using FomodInstaller.ModInstaller;

using ModInstaller.Adaptor.Tests.Shared.Delegates;

using TestData;

namespace ModInstaller.Adaptor.Dynamic.Tests;

using TestClass = IEnumerable<Func<InstallData>>;

public class InstallTests
{
    public static TestClass SkyrimData() => InstallDataSource.SkyrimData();
    public static TestClass Fallout4Data() => InstallDataSource.Fallout4Data();
    public static TestClass FalloutNVData() => InstallDataSource.FalloutNVData();
    public static TestClass FomodComplianceTestsData() => InstallDataSource.FomodComplianceTestsData();
    public static TestClass CSharpTestCaseData() => InstallDataSource.CSharpTestCaseData();

    [Test]
    [MethodDataSource(nameof(SkyrimData))]
    [MethodDataSource(nameof(Fallout4Data))]
    //[MethodDataSource(nameof(FalloutNVData))]
    [MethodDataSource(nameof(FomodComplianceTestsData))]
    [MethodDataSource(nameof(CSharpTestCaseData))]
    [NotInParallel]
    public async Task Test(InstallData data)
    {
        var coreDelegates = new TestCoreDelegates(
            new CallbackPluginDelegates(
                _ => data.InstalledPlugins.ToArray()
            ),
            new CallbackIniDelegates(null!, null!),
            new CallbackContextDelegates(
                () => data.AppVersion,
                () => data.GameVersion,
                (_) => data.ExtenderVersion,
                () => Task.FromResult(false),
                (path) => Task.FromResult(File.Exists(path)),
                (path) => Task.FromResult(File.ReadAllBytes(path)),
                (folderPath, searchFilter, isRecursive) => Task.FromResult(Directory.GetDirectories(folderPath, searchFilter, isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            ),
            new DeterministicUIContext(data.DialogChoices)
        );

        // Extract archive to temp directory so ModInstallerIPC can read files
        var tempDir = Path.Combine(Path.GetTempPath(), $"FomodTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        
        //data.ModArchive.ExtractToDirectory(tempDir);
        // Extract all files from archive
        foreach (var entry in data.ModArchive.Entries.Where(e => !e.IsDirectory))
        {
            var normalizedPath = entry.GetNormalizedName();
            if (normalizedPath == null) continue;

            var destPath = Path.Combine(tempDir, normalizedPath);
            var destDir = Path.GetDirectoryName(destPath);
            if (destDir != null && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            await using var stream = entry.OpenEntryStream();
            await using var fileStream = File.Create(destPath);
            await stream.CopyToAsync(fileStream);
        }
        
        var progressDelegate = new ProgressDelegate((perc) => { });
        var installer = new Installer();
        var result = await installer.Install(
            data.ModArchive.Entries.Select(x => x.GetNormalizedName()).ToList(),
            data.StopPatterns,
            data.PluginPath,
            tempDir,
            null,
            data.Validate,
            progressDelegate,
            coreDelegates
        );

        await Assert.That(((List<Instruction>) result["instructions"]).Order()).IsEquivalentTo(data.Instructions.Order());
        await Assert.That(result["message"]).IsEquivalentTo(data.Message);
    }   
}
