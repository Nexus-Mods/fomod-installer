using FomodInstaller.ModInstaller;

using ModInstaller.Adaptor.Typed.Tests.Delegates;
using ModInstaller.Lite;

using TestData;

using Utils;

namespace ModInstaller.Adaptor.Typed.Tests;

using TestClass = IEnumerable<Func<InstallData>>;

public class InstallTests
{
    public static TestClass SkyrimData() => InstallDataSource.SkyrimData();
    public static TestClass Fallout4Data() => InstallDataSource.Fallout4Data();
    public static TestClass FomodComplianceTestsData() => InstallDataSource.FomodComplianceTestsData();

    [Test]
    [MethodDataSource(nameof(SkyrimData))]
    [MethodDataSource(nameof(Fallout4Data))]
    [MethodDataSource(nameof(FomodComplianceTestsData))]
    [NotInParallel]
    public async Task Test(InstallData data)
    {
        var coreDelegates = new TestCoreDelegates(
            new CallbackPluginDelegates(
                _ => Task.FromResult(data.InstalledPlugins.ToArray())
            ),
            new CallbackIniDelegates(null!, null!),
            new CallbackContextDelegates(
                () => Task.FromResult(data.AppVersion),
                () => Task.FromResult(data.GameVersion),
                (_) => Task.FromResult(data.ExtenderVersion),
                null!, null!, null!, null!
            ),
            new DeterministicUIContext(data.DialogChoices)
        );

        FileSystem.Instance = new ArchiveFileSystem(data.ModArchive);

        var progressDelegate = new ProgressDelegate((perc) => { });
        var result = await Installer.Install(
            data.ModArchive.Entries.Select(x => x.GetNormalizedName()).ToList(),
            data.StopPatterns,
            data.PluginPath,
            "",
            data.Preset,
            data.Validate,
            progressDelegate,
            coreDelegates);

        await Assert.That(result.Instructions.Order()).IsEquivalentTo(data.Instructions.Order());
        await Assert.That(result.Message).IsEquivalentTo(data.Message);
    }

    [Test]
    [MethodDataSource(nameof(SkyrimData))]
    [MethodDataSource(nameof(Fallout4Data))]
    [MethodDataSource(nameof(FomodComplianceTestsData))]
    [NotInParallel]
    public async Task TestCancellation(InstallData data)
    {
        if (data.Preset is not null)
            return;
        
        var cancellingUIContext = new CancellingUIContext();
        var coreDelegates = new TestCoreDelegates(
            new CallbackPluginDelegates(
                _ => Task.FromResult(data.InstalledPlugins.ToArray())
            ),
            new CallbackIniDelegates(null!, null!),
            new CallbackContextDelegates(
                () => Task.FromResult(data.AppVersion),
                () => Task.FromResult(data.GameVersion),
                (_) => Task.FromResult(data.ExtenderVersion),
                null!, null!, null!, null!
            ),
            cancellingUIContext
        );

        FileSystem.Instance = new ArchiveFileSystem(data.ModArchive);

        var progressDelegate = new ProgressDelegate((perc) => { });
        try
        {
            var result = await Installer.Install(
                data.ModArchive.Entries.Select(x => x.GetNormalizedName()).ToList(),
                data.StopPatterns,
                data.PluginPath,
                "",
                data.Preset,
                data.Validate,
                progressDelegate,
                coreDelegates);
            
            Assert.Fail("Should have thrown TaskCanceledException");
        }
        catch (TaskCanceledException) { }
        catch (Exception e)
        {
            Assert.Fail("Should not throw exception, cancellation is handled internally. Threw: " + e);
        }

        await Assert.That(cancellingUIContext.CancelWasCalled).IsTrue();
    }
}