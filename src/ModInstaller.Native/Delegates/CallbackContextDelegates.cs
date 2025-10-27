using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BUTR.NativeAOT.Shared;
using FomodInstaller.Interface;

namespace ModInstaller.Native.Adapters;

internal class CallbackContextDelegates : ContextDelegates
{
    private readonly unsafe param_ptr* _pOwner;
    private readonly N_Context_GetAppVersion _getAppVersion;
    private readonly N_Context_GetCurrentGameVersion _getCurrentGameVersion;
    private readonly N_Context_GetExtenderVersion _getExtenderVersion;
    private readonly N_Context_IsExtenderPresent _isExtenderPresent;
    private readonly N_Context_CheckIfFileExists _checkIfFileExists;
    private readonly N_Context_GetExistingDataFile _getExistingDataFile;
    private readonly N_Context_GetExistingDataFileList _getExistingDataFileList;

    public unsafe CallbackContextDelegates(param_ptr* pOwner,
        N_Context_GetAppVersion getAppVersion,
        N_Context_GetCurrentGameVersion getCurrentGameVersion,
        N_Context_GetExtenderVersion getExtenderVersion,
        N_Context_IsExtenderPresent isExtenderPresent,
        N_Context_CheckIfFileExists checkIfFileExists,
        N_Context_GetExistingDataFile getExistingDataFile,
        N_Context_GetExistingDataFileList getExistingDataFileList)
    {
        _pOwner = pOwner;
        _getAppVersion = getAppVersion;
        _getCurrentGameVersion = getCurrentGameVersion;
        _getExtenderVersion = getExtenderVersion;
        _isExtenderPresent = isExtenderPresent;
        _checkIfFileExists = checkIfFileExists;
        _getExistingDataFile = getExistingDataFile;
        _getExistingDataFileList = getExistingDataFileList;
    }

    public override async Task<string> GetAppVersion()
    {
        var tcs = new TaskCompletionSource<string>();
        GetAppVersionNative(tcs);
        return await tcs.Task;
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GetAppVersionCallback(param_ptr* pOwner, return_value_string* pResult)
    {
        Logger.LogCallbackInput(pResult);

        if (pOwner == null)
        {
            Logger.LogException(new ArgumentNullException(nameof(pOwner)));
            return;
        }

        if (GCHandle.FromIntPtr((IntPtr) pOwner) is not { Target: TaskCompletionSource<string?> tcs } handle)
        {
            Logger.LogException(new InvalidOperationException("Invalid GCHandle."));
            return;
        }

        using var result = SafeStructMallocHandle.Create(pResult, true);
        result.SetAsString(tcs);
        handle.Free();

        Logger.LogOutput();
    }

    private unsafe void GetAppVersionNative(TaskCompletionSource<string> tcs)
    {
        Logger.LogInput();

        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        try
        {
            using var result = SafeStructMallocHandle.Create(_getAppVersion(_pOwner, (param_ptr*) GCHandle.ToIntPtr(handle), &GetAppVersionCallback), true);
            result.ValueAsVoid();

            Logger.LogOutput();
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            tcs.TrySetException(e);
            handle.Free();
        }
    }

    public override async Task<string> GetCurrentGameVersion()
    {
        var tcs = new TaskCompletionSource<string>();
        GetCurrentGameVersionNative(tcs);
        return await tcs.Task;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GetCurrentGameVersionCallback(param_ptr* pOwner, return_value_string* pResult)
    {
        Logger.LogCallbackInput(pResult);
        
        if (pOwner == null)
        {
            Logger.LogException(new ArgumentNullException(nameof(pOwner)));
            return;
        }
        
        if (GCHandle.FromIntPtr((IntPtr) pOwner) is not { Target: TaskCompletionSource<string?> tcs } handle)
        {
            Logger.LogException(new InvalidOperationException("Invalid GCHandle."));
            return;
        }
        
        using var result = SafeStructMallocHandle.Create(pResult, true);
        result.SetAsString(tcs);
        handle.Free();
        
        Logger.LogOutput();
    }
    
    private unsafe void GetCurrentGameVersionNative(TaskCompletionSource<string> tcs)
    {
        Logger.LogInput();

        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        try
        {
            using var result = SafeStructMallocHandle.Create(_getCurrentGameVersion(_pOwner, (param_ptr*) GCHandle.ToIntPtr(handle), &GetCurrentGameVersionCallback), true);
            result.ValueAsVoid();

            Logger.LogOutput();
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            tcs.TrySetException(e);
            handle.Free();
        }
    }

    public override async Task<string> GetExtenderVersion(string extender)
    {
        var tcs = new TaskCompletionSource<string>();
        GetExtenderVersionNative(extender, tcs);
        return await tcs.Task;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GetExtenderVersionCallback(param_ptr* pOwner, return_value_string* pResult)
    {
        Logger.LogCallbackInput(pResult);

        if (pOwner == null)
        {
            Logger.LogException(new ArgumentNullException(nameof(pOwner)));
            return;
        }
        
        if (GCHandle.FromIntPtr((IntPtr) pOwner) is not { Target: TaskCompletionSource<string?> tcs } handle)
        {
            Logger.LogException(new InvalidOperationException("Invalid GCHandle."));
            return;
        }
        
        using var result = SafeStructMallocHandle.Create(pResult, true);
        result.SetAsString(tcs);
        handle.Free();
        
        Logger.LogOutput();
    }
    
    private unsafe void GetExtenderVersionNative(ReadOnlySpan<char> extender, TaskCompletionSource<string> tcs)
    {
        Logger.LogInput();

        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        fixed (char* pExtender = extender)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_getExtenderVersion(_pOwner, (param_string*) pExtender, (param_ptr*) GCHandle.ToIntPtr(handle), &GetExtenderVersionCallback), true);
                result.ValueAsVoid();

                Logger.LogOutput();
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                tcs.TrySetException(e);
                handle.Free();
            } 
        }
    }

    public override async Task<bool> IsExtenderPresent()
    {
        var tcs = new TaskCompletionSource<bool>();
        IsExtenderPresentNative(tcs);
        return await tcs.Task;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void IsExtenderPresentCallback(param_ptr* pOwner, return_value_bool* pResult)
    {
        Logger.LogCallbackInput(pResult);
        
        if (pOwner == null)
        {
            Logger.LogException(new ArgumentNullException(nameof(pOwner)));
            return;
        }
        
        if (GCHandle.FromIntPtr((IntPtr) pOwner) is not { Target: TaskCompletionSource<bool> tcs } handle)
        {
            Logger.LogException(new InvalidOperationException("Invalid GCHandle."));
            return;
        }
        
        using var result = SafeStructMallocHandle.Create(pResult, true);
        result.SetAsBool(tcs);
        handle.Free();
        
        Logger.LogOutput();
    }
    
    private unsafe void IsExtenderPresentNative(TaskCompletionSource<bool> tcs)
    {
        Logger.LogInput();

        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        try
        {
            using var result = SafeStructMallocHandle.Create(_isExtenderPresent(_pOwner, (param_ptr*) GCHandle.ToIntPtr(handle), &IsExtenderPresentCallback), true);
            result.ValueAsVoid();

            Logger.LogOutput();
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            tcs.TrySetException(e);
            handle.Free();
        }
    }
    
    public override async Task<bool> CheckIfFileExists(string fileName)
    {
        var tcs = new TaskCompletionSource<bool>();
        CheckIfFileExistsNative(fileName, tcs);
        return await tcs.Task;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void CheckIfFileExistsCallback(param_ptr* pOwner, return_value_bool* pResult)
    {
        Logger.LogCallbackInput(pResult);

        if (pOwner == null)
        {
            Logger.LogException(new ArgumentNullException(nameof(pOwner)));
            return;
        }
        
        if (GCHandle.FromIntPtr((IntPtr) pOwner) is not { Target: TaskCompletionSource<bool> tcs } handle)
        {
            Logger.LogException(new InvalidOperationException("Invalid GCHandle."));
            return;
        }
        
        using var result = SafeStructMallocHandle.Create(pResult, true);
        result.SetAsBool(tcs);
        handle.Free();
        
        Logger.LogOutput();
    }
    
    private unsafe void CheckIfFileExistsNative(ReadOnlySpan<char> fileName, TaskCompletionSource<bool> tcs)
    {
        Logger.LogInput();

        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        fixed (char* pFileName = fileName)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_checkIfFileExists(_pOwner, (param_string*) pFileName, (param_ptr*) GCHandle.ToIntPtr(handle), &CheckIfFileExistsCallback), true);
                result.ValueAsVoid();

                Logger.LogOutput();
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                tcs.TrySetException(e);
                handle.Free();
            } 
        }
    }

    public override async Task<byte[]> GetExistingDataFile(string dataFile)
    {
        var tcs = new TaskCompletionSource<byte[]>();
        GetExistingDataFileNative(dataFile, tcs);
        return await tcs.Task;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GetExistingDataFileCallback(param_ptr* pOwner, return_value_data* pResult)
    {
        Logger.LogCallbackInput(pResult);

        if (pOwner == null)
        {
            Logger.LogException(new ArgumentNullException(nameof(pOwner)));
            return;
        }
        
        if (GCHandle.FromIntPtr((IntPtr) pOwner) is not { Target: TaskCompletionSource<byte[]?> tcs } handle)
        {
            Logger.LogException(new InvalidOperationException("Invalid GCHandle."));
            return;
        }
        
        using var result = SafeStructMallocHandle.Create(pResult, true);
        result.SetAsData(tcs);
        handle.Free();
        
        Logger.LogOutput();
    }
    
    private unsafe void GetExistingDataFileNative(ReadOnlySpan<char> dataFile, TaskCompletionSource<byte[]> tcs)
    {
        Logger.LogInput();

        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        fixed (char* pDataFile = dataFile)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_getExistingDataFile(_pOwner, (param_string*) pDataFile, (param_ptr*) GCHandle.ToIntPtr(handle), &GetExistingDataFileCallback), true);
                result.ValueAsVoid();

                Logger.LogOutput();
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                tcs.TrySetException(e);
                handle.Free();
            } 
        }
    }

    public override async Task<string[]> GetExistingDataFileList(string folderPath, string searchFilter, bool isRecursive)
    {
        var tcs = new TaskCompletionSource<string[]>();
        GetExistingDataFileListNative(folderPath, searchFilter, isRecursive, tcs);
        return await tcs.Task;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GetExistingDataFileListCallback(param_ptr* pOwner, return_value_json* pResult)
    {
        Logger.LogCallbackInput(pResult);
        
        if (pOwner == null)
        {
            Logger.LogException(new ArgumentNullException(nameof(pOwner)));
            return;
        }
        
        if (GCHandle.FromIntPtr((IntPtr) pOwner) is not { Target: TaskCompletionSource<string[]?> tcs } handle)
        {
            Logger.LogException(new InvalidOperationException("Invalid GCHandle."));
            return;
        }
        
        using var result = SafeStructMallocHandle.Create(pResult, true);
        result.SetAsJson(tcs, SourceGenerationContext.Default.StringArray);
        handle.Free();
        
        Logger.LogOutput();
    }
    
    private unsafe void GetExistingDataFileListNative(ReadOnlySpan<char> folderPath, ReadOnlySpan<char> searchFilter, bool isRecursive, TaskCompletionSource<string[]> tcs)
    {
        Logger.LogInput();

        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        fixed (char* pFolderPath = folderPath)
        fixed (char* pSearchFilter = searchFilter)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_getExistingDataFileList(_pOwner, (param_string*) pFolderPath, (param_string*) pSearchFilter, isRecursive, (param_ptr*) GCHandle.ToIntPtr(handle), &GetExistingDataFileListCallback), true);
                result.ValueAsVoid();

                Logger.LogOutput();
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                tcs.TrySetException(e);
                handle.Free();
            } 
        }
    }
}