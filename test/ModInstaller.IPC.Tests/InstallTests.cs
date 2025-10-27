using ModInstaller.IPC.Tests.Delegates;
using ModInstaller.IPC.Tests.Utils;
using TestData;

namespace ModInstaller.IPC.Tests;

using TestClass = IEnumerable<Func<InstallData>>;

public class InstallTests
{
    public static TestClass SkyrimData() => InstallDataSource.SkyrimData();
    public static TestClass Fallout4Data() => InstallDataSource.Fallout4Data();
    public static TestClass FomodComplianceTestsData() => InstallDataSource.FomodComplianceTestsData();
    public static TestClass CSharpTestCaseData() => InstallDataSource.CSharpTestCaseData();

    [Test]
    [MethodDataSource(nameof(SkyrimData))]
    [MethodDataSource(nameof(Fallout4Data))]
    [MethodDataSource(nameof(FomodComplianceTestsData))]
    //[MethodDataSource(nameof(CSharpTestCaseData))]
    [NotInParallel]
    public async Task Test(InstallData data)
    {
        // Extract archive to temp directory so ModInstallerIPC can read files
        var tempDir = Path.Combine(Path.GetTempPath(), $"FomodTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
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

                using var stream = entry.OpenEntryStream();
                using var fileStream = File.Create(destPath);
                await stream.CopyToAsync(fileStream);
            }

            await using var harness = await new IPCTestHarness().InitializeAsync();

            // Create delegates that wrap the test data
            var fileSystem = new ArchiveFileSystem(data.ModArchive);
            var delegates = new IPCDelegates(fileSystem, data, data.DialogChoices);

            // Get file list from archive
            var files = data.ModArchive.Entries.Select(x => x.GetNormalizedName()).OfType<string>().ToList();

            // Perform installation through IPC
            // The scriptPath parameter should point to the temp directory where files are extracted
            var result = await harness.InstallAsync(
                files,
                data.StopPatterns,
                data.PluginPath,
                tempDir,  // scriptPath is used as the temp folder where files are extracted
                data.Preset,
                data.Validate,
                data.DialogChoices,  // Pass dialog choices for deterministic UI
                getAppVersion: delegates.GetAppVersion,
                getCurrentGameVersion: delegates.GetCurrentGameVersion,
                getExtenderVersion: delegates.GetExtenderVersion,
                isExtenderPresent: delegates.IsExtenderPresent,
                checkIfFileExists: delegates.CheckIfFileExists,
                getExistingDataFile: delegates.GetExistingDataFile,
                getExistingDataFileList: delegates.GetExistingDataFileList,
                getAllPlugins: delegates.GetAllPlugins,
                isPluginActive: delegates.IsPluginActive,
                isPluginPresent: delegates.IsPluginPresent,
                getIniString: delegates.GetIniString,
                getIniInt: delegates.GetIniInt);

            // Assert results match expected
            await Assert.That(result.Instructions.Order()).IsEquivalentTo(data.Instructions.Order());
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
