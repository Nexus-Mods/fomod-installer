using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BUTR.NativeAOT.Shared;
using FomodInstaller.Interface;

namespace ModInstaller.Native.Adapters;

internal class CallbackIniDelegates : IniDelegates
{
    private readonly unsafe param_ptr* _pOwner;
    private readonly N_Ini_GetIniString _getIniString;
    private readonly N_Ini_GetIniInt _getIniInt;

    public unsafe CallbackIniDelegates(param_ptr* pOwner,
        N_Ini_GetIniString getIniString,
        N_Ini_GetIniInt getIniInt)
    {
        _pOwner = pOwner;
        _getIniString = getIniString;
        _getIniInt = getIniInt;
    }
    
    public override async Task<string> GetIniString(string iniFilename, string iniSection, string iniKey)
    {
        var tcs = new TaskCompletionSource<string>();
        GetIniStringNative(iniFilename, iniSection, iniKey, tcs);
        return await tcs.Task;
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GetIniStringCallback(param_ptr* pOwner, return_value_string* pResult)
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

    private unsafe void GetIniStringNative(ReadOnlySpan<char> iniFilename, ReadOnlySpan<char> iniSection, ReadOnlySpan<char> iniKey, TaskCompletionSource<string> tcs)
    {
        Logger.LogInput();

        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        fixed (char* pIniFilename = iniFilename)
        fixed (char* pIniSection = iniSection)
        fixed (char* pIniKey = iniKey)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_getIniString(_pOwner, (param_string*) pIniFilename, (param_string*) pIniSection, (param_string*) pIniKey, (param_ptr*) GCHandle.ToIntPtr(handle), &GetIniStringCallback), true);
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

    public override async Task<int> GetIniInt(string iniFilename, string iniSection, string iniKey)
    {
        var tcs = new TaskCompletionSource<int>();
        GetIniIntNative(iniFilename, iniSection, iniKey, tcs);
        return await tcs.Task;
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GetIniIntCallback(param_ptr* pOwner, return_value_int32* pResult)
    {
        Logger.LogCallbackInput(pResult);

        if (pOwner == null)
        {
            Logger.LogException(new ArgumentNullException(nameof(pOwner)));
            return;
        }

        if (GCHandle.FromIntPtr((IntPtr) pOwner) is not { Target: TaskCompletionSource<int> tcs } handle)
        {
            Logger.LogException(new InvalidOperationException("Invalid GCHandle."));
            return;
        }

        using var result = SafeStructMallocHandle.Create(pResult, true);
        result.SetAsInt32(tcs);
        handle.Free();

        Logger.LogOutput();
    }

    private unsafe void GetIniIntNative(ReadOnlySpan<char> iniFilename, ReadOnlySpan<char> iniSection, ReadOnlySpan<char> iniKey, TaskCompletionSource<int> tcs)
    {
        Logger.LogInput();

        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        fixed (char* pIniFilename = iniFilename)
        fixed (char* pIniSection = iniSection)
        fixed (char* pIniKey = iniKey)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_getIniInt(_pOwner, (param_string*) pIniFilename, (param_string*) pIniSection, (param_string*) pIniKey, (param_ptr*) GCHandle.ToIntPtr(handle), &GetIniIntCallback), true);
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