using FomodInstaller.ModInstaller;

using ModInstaller.Adaptor.Tests.Shared.Delegates;
using ModInstaller.Lite;

using TestData;

using Utils;

namespace ModInstaller.Adaptor.Typed.Tests;

using TestClass = IEnumerable<Func<InstallData>>;

public class InstallTests
{
    public static TestClass SkyrimData() => InstallDataSource.SkyrimData();
    public static TestClass Fallout4Data() => InstallDataSource.Fallout4Data();
    public static TestClass FalloutNVData() => InstallDataSource.FalloutNVData();
    public static TestClass FomodComplianceTestsData() => InstallDataSource.FomodComplianceTestsData();

    [Test]
    [MethodDataSource(nameof(SkyrimData))]
    [MethodDataSource(nameof(Fallout4Data))]
    //[MethodDataSource(nameof(FalloutNVData))]
    [MethodDataSource(nameof(FomodComplianceTestsData))]
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
            false,
            data.Validate,
            progressDelegate,
            coreDelegates);

        await Assert.That(result.Instructions.Order()).IsEquivalentTo(data.Instructions.Order());
        await Assert.That(result.Message).IsEquivalentTo(data.Message);
    }

    [Test]
    [MethodDataSource(nameof(SkyrimData))]
    [MethodDataSource(nameof(Fallout4Data))]
    //[MethodDataSource(nameof(FalloutNVData))]
    [MethodDataSource(nameof(FomodComplianceTestsData))]
    [NotInParallel]
    public async Task TestPreselect(InstallData data)
    {
        if (data.Preset is null)
            return;

        var preselectUI = new PreselectUIContext();
        var coreDelegates = new TestCoreDelegates(
            new CallbackPluginDelegates(
                _ => data.InstalledPlugins.ToArray()
            ),
            new CallbackIniDelegates(null!, null!),
            new CallbackContextDelegates(
                () => data.AppVersion,
                () => data.GameVersion,
                (_) => data.ExtenderVersion,
                null!, null!, null!, null!
            ),
            preselectUI
        );

        FileSystem.Instance = new ArchiveFileSystem(data.ModArchive);

        var progressDelegate = new ProgressDelegate((perc) => { });
        var result = await Installer.Install(
            data.ModArchive.Entries.Select(x => x.GetNormalizedName()).ToList(),
            data.StopPatterns,
            data.PluginPath,
            "",
            data.Preset,
            true,
            data.Validate,
            progressDelegate,
            coreDelegates);

        // The dialog should have been shown (at least one step snapshot captured)
        await Assert.That(preselectUI.StepSnapshots.Count).IsGreaterThan(0);

        // Parse the preset to know which options should be pre-selected
        var presetSteps = data.Preset.RootElement.EnumerateArray().ToList();
        foreach (var snapshot in preselectUI.StepSnapshots)
        {
            // Find preset entry for this step (by name). Use TryGetProperty so
            // malformed preset entries (missing keys) don't crash the test — the
            // production code tolerates them and the test should too.
            var matchingPresetStep = presetSteps
                .Where(ps => ps.TryGetProperty("name", out var n) && n.GetString() == snapshot.StepName)
                .ToList();

            if (matchingPresetStep.Count == 0)
                continue;

            foreach (var presetStep in matchingPresetStep)
            {
                if (!presetStep.TryGetProperty("groups", out var presetGroups))
                    continue;
                foreach (var presetGroup in presetGroups.EnumerateArray())
                {
                    if (!presetGroup.TryGetProperty("name", out var groupNameProp))
                        continue;
                    var groupName = groupNameProp.GetString();
                    if (!presetGroup.TryGetProperty("choices", out var presetChoices))
                        continue;
                    var presetChoiceNames = presetChoices.EnumerateArray()
                        .Where(c => c.TryGetProperty("name", out _))
                        .Select(c => c.GetProperty("name").GetString())
                        .ToHashSet();

                    // Verify options from this group in the snapshot
                    var groupOptions = snapshot.Options.Where(o => o.GroupName == groupName).ToList();
                    foreach (var opt in groupOptions)
                    {
                        if (presetChoiceNames.Contains(opt.OptionName))
                        {
                            // Preset option should be selected and marked as preset
                            await Assert.That(opt.Selected)
                                .IsTrue()
                                .Because($"Preset option '{opt.OptionName}' in group '{groupName}' step '{snapshot.StepName}' should be selected");
                            await Assert.That(opt.Preset)
                                .IsTrue()
                                .Because($"Preset option '{opt.OptionName}' in group '{groupName}' step '{snapshot.StepName}' should be marked as preset");
                        }
                    }
                }
            }
        }

        // The final result should match the headless preset result
        await Assert.That(result.Instructions.Order()).IsEquivalentTo(data.Instructions.Order());
        await Assert.That(result.Message).IsEquivalentTo(data.Message);
    }

    [Test]
    [MethodDataSource(nameof(SkyrimData))]
    [MethodDataSource(nameof(Fallout4Data))]
    //[MethodDataSource(nameof(FalloutNVData))]
    [MethodDataSource(nameof(FomodComplianceTestsData))]
    [NotInParallel]
    public async Task TestCancellation(InstallData data)
    {
        if (data.Preset is not null)
            return;

        var cancellingUIContext = new CancellingUIContext();
        var coreDelegates = new TestCoreDelegates(
            new CallbackPluginDelegates(
                _ => data.InstalledPlugins.ToArray()
            ),
            new CallbackIniDelegates(null!, null!),
            new CallbackContextDelegates(
                () => data.AppVersion,
                () => data.GameVersion,
                (_) => data.ExtenderVersion,
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
                false,
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
