using BUTR.NativeAOT.Shared;

using FluentAssertions;

using ModInstaller.Lite;
using ModInstaller.Native.Tests.Extensions;

using NUnit.Framework;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

using TestData;

using static ModInstaller.Native.Tests.Utils.Utils2;

namespace ModInstaller.Native.Tests;

using TestClass = IEnumerable<InstallData>;

public sealed partial class InstallTests : BaseTests
{
        [LibraryImport(DllPath), UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe partial return_value_void* set_file_system_callbacks(
        param_ptr* p_owner,
        // FileSystem Delegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_int, param_int, return_value_data*> p_read_file_content,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_string*, param_int, return_value_json*> p_read_directory_file_list,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, return_value_json*> p_read_directory_list);

    
    [LibraryImport(DllPath), UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe partial return_value_ptr* create_handler(
        param_ptr* p_owner,
        // PluginDelegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_bool, return_value_json*> p_plugins_get_all,
        // ContextDelegates
        delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*> p_context_get_app_version,
        delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*> p_context_get_current_game_version,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, return_value_string*> p_context_get_extender_version,
        // UI Delegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_json*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, param_int, param_int, param_json*, return_value_void*, void>, delegate* unmanaged[Cdecl]<param_ptr*, param_bool, param_int, return_value_void*, void>, delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*, void>, return_value_void*> p_ui_start_dialog,
        delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*> p_ui_end_dialog,
        delegate* unmanaged[Cdecl]<param_ptr*, param_json*, param_int, return_value_void*> p_ui_update_state);

    [LibraryImport(DllPath), UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe partial return_value_void* dispose_handler(
        param_ptr* p_handle);

    [LibraryImport(DllPath), UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe partial return_value_void* install(
        param_ptr* p_handle,
        param_json* p_mod_archive_file_list,
        param_json* p_stop_patterns,
        param_string* p_plugin_path,
        param_string* p_script_path,
        param_json* p_preset,
        param_bool validate,
        param_ptr* p_callback_handler,
        delegate* unmanaged[Cdecl]<param_ptr*, return_value_json*, void> p_callback);

    public static TestClass SkyrimData() => InstallDataSource.SkyrimData().NUnit();
    public static TestClass Fallout4Data() => InstallDataSource.Fallout4Data().NUnit();
    public static TestClass FalloutNVData() => InstallDataSource.FalloutNVData().NUnit();
    public static TestClass FomodComplianceTestsData() => InstallDataSource.FomodComplianceTestsData().NUnit();
    public static TestClass CSharpTestCaseData() => InstallDataSource.CSharpTestCaseData().NUnit();


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void InstallCallback(param_ptr* owner, return_value_json* result)
    {
        var tcs = (TaskCompletionSource<InstallResult?>) GCHandle.FromIntPtr((IntPtr) owner).Target!;
        var tcs2 = new TaskCompletionSource<InstallResult?>();
        GetResult(result, tcs2);
        tcs.SetResult(tcs2.Task.Result);
    }

    private static unsafe (IntPtr, IntPtr) Setup(InstallData data)
    {
        var wrapper = new ModInstallerWrapper(data);
        var handle = GCHandle.ToIntPtr(GCHandle.Alloc(wrapper, GCHandleType.Normal));

        GetResult(set_file_system_callbacks((param_ptr*) handle.ToPointer(),
            p_read_file_content: &ModInstallerWrapper.ReadFileContent,
            p_read_directory_file_list: &ModInstallerWrapper.ReadDirectoryFileList,
            p_read_directory_list: &ModInstallerWrapper.ReadDirectoryList));
        
        var ptr = GetResult(create_handler((param_ptr*) handle.ToPointer(),
            p_plugins_get_all: &ModInstallerWrapper.PluginsGetAll,

            p_context_get_app_version: &ModInstallerWrapper.ContextGetAppVersion,
            p_context_get_current_game_version: &ModInstallerWrapper.ContextGetCurrentGameVersion,
            p_context_get_extender_version: &ModInstallerWrapper.ContextGetExtenderVersion,

            p_ui_start_dialog: &ModInstallerWrapper.UiStartDialog,
            p_ui_end_dialog: &ModInstallerWrapper.UiEndDialog,
            p_ui_update_state: &ModInstallerWrapper.UiUpdateState));

        return ((IntPtr) ptr, handle);
    }

    private static unsafe void Setdown(IntPtr ptr)
    {
        GetResult(dispose_handler((param_ptr*) ptr));
    }

    private static unsafe void Call(InstallData data, IntPtr ptr, IntPtr tcsPtr)
    {
        using var files = ToJson(data.ModArchive.Entries.Select(x => x.GetNormalizedName()).ToList());
        using var stopList = ToJson(data.StopPatterns);

        using var pluginPath = BUTR.NativeAOT.Shared.Utils.Copy(data.PluginPath, true);
        using var scriptPath = BUTR.NativeAOT.Shared.Utils.Copy("", true);
        using var preset = ToJson(data.Preset ?? JsonDocument.Parse("[]"));

        LibraryAliveCount().Should().Be(5);
        GetResult(install(
            (param_ptr*) ptr,
            files,
            stopList,
            pluginPath,
            scriptPath,
            data.Preset is null ? (param_json*) null : preset,
            data.Validate,
            (param_ptr*) tcsPtr,
            &InstallCallback));
    }

    [Test]
    [TestCaseSource(nameof(SkyrimData))]
    [TestCaseSource(nameof(Fallout4Data))]
    //[TestCaseSource(nameof(FalloutNVData))]
    [TestCaseSource(nameof(FomodComplianceTestsData))]
    //[TestCaseSource(nameof(CSharpTestCaseData))]
    [NonParallelizable]
    public async Task Test(InstallData data)
    {
        {
            var (ptr, handle) = Setup(data);

            var tcs = new TaskCompletionSource<InstallResult?>();
            var tcsPtr = GCHandle.ToIntPtr(GCHandle.Alloc(tcs, GCHandleType.Normal));

            Call(data, ptr, tcsPtr);

            var result = await tcs.Task;
            LibraryAliveCount().Should().Be(0);

            GCHandle.FromIntPtr(handle).Free();
            GCHandle.FromIntPtr(tcsPtr).Free();

            Setdown(ptr);

            result.Should().NotBeNull();
            result.Instructions.Order().Should().BeEquivalentTo(data.Instructions.Order());
            result.Message.Order().Should().BeEquivalentTo(result.Message);
        }

        LibraryAliveCount().Should().Be(0);
    }

    /*
    [Test]
    [TestCaseSource(nameof(SkyrimData))]
    [TestCaseSource(nameof(Fallout4Data))]
    [TestCaseSource(nameof(FalloutNVData))]
    [TestCaseSource(nameof(FomodComplianceTestsData))]
    [NonParallelizable]
    public async Task TestCancellation(InstallData data)
    {
        {
            var cancellingData = data with { DialogChoices = null };
            var (ptr, handle) = Setup(cancellingData);

            var tcs = new TaskCompletionSource<InstallResult?>();
            var tcsPtr = GCHandle.ToIntPtr(GCHandle.Alloc(tcs, GCHandleType.Normal));

            Call(cancellingData, ptr, tcsPtr);

            var result = await tcs.Task;
            LibraryAliveCount().Should().Be(0);

            var wrapper = (ModInstallerWrapper)GCHandle.FromIntPtr(handle).Target!;
            wrapper.CancelWasCalled.Should().BeTrue();

            GCHandle.FromIntPtr(handle).Free();
            GCHandle.FromIntPtr(tcsPtr).Free();

            Setdown(ptr);

            result.Should().NotBeNull();
            result.Instructions.Should().BeEmpty();
        }

        LibraryAliveCount().Should().Be(0);
    }
    */
}