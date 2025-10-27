﻿using FomodInstaller.ModInstaller;
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
    public static TestClass CSharpTestCaseData() => InstallDataSource.CSharpTestCaseData();

    [Test]
    [MethodDataSource(nameof(SkyrimData))]
    [MethodDataSource(nameof(Fallout4Data))]
    [MethodDataSource(nameof(FomodComplianceTestsData))]
    //[MethodDataSource(nameof(CSharpTestCaseData))]
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
}