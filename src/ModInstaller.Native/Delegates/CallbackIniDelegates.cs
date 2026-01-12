using BUTR.NativeAOT.Shared;

using FomodInstaller.Interface;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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

    public override Task<string> GetIniString(string iniFilename, string iniSection, string iniKey)
    {
#if DEBUG
        using var logger = LogMethod(iniFilename.ToFormattable(), iniSection.ToFormattable(), iniKey.ToFormattable());
#else
        using var logger = LogMethod();
#endif

        try
        {
            var tcs = new TaskCompletionSource<string>();
            GetIniStringNative(iniFilename, iniSection, iniKey, tcs);
            return tcs.Task;
        }
        catch (Exception e)
        {
            logger.LogException(e);
            throw;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GetIniStringCallback(param_ptr* pOwner, return_value_string* pResult)
    {
#if DEBUG
        using var logger = LogCallbackMethod(pResult);
#else
        using var logger = LogCallbackMethod(pResult);
#endif

        try
        {

            if (pOwner == null)
            {
                logger.LogException(new ArgumentNullException(nameof(pOwner)));
                return;
            }

            if (GCHandle.FromIntPtr((IntPtr) pOwner) is not {Target: TaskCompletionSource<string?> tcs} handle)
            {
                logger.LogException(new InvalidOperationException("Invalid GCHandle."));
                return;
            }

            using var result = SafeStructMallocHandle.Create(pResult, true);
            logger.LogResult(result);
            result.SetAsString(tcs);
            handle.Free();
        }
        catch (Exception e)
        {
            logger.LogException(e);
            throw;
        }
    }

    private unsafe void GetIniStringNative(ReadOnlySpan<char> iniFilename, ReadOnlySpan<char> iniSection, ReadOnlySpan<char> iniKey, TaskCompletionSource<string> tcs)
    {
#if DEBUG
        using var logger = LogMethod(iniFilename.ToString().ToFormattable(), iniSection.ToString().ToFormattable(), iniKey.ToString().ToFormattable());
#else
        using var logger = LogMethod();
#endif

        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        fixed (char* pIniFilename = iniFilename)
        fixed (char* pIniSection = iniSection)
        fixed (char* pIniKey = iniKey)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_getIniString(_pOwner, (param_string*) pIniFilename, (param_string*) pIniSection, (param_string*) pIniKey, (param_ptr*) GCHandle.ToIntPtr(handle), &GetIniStringCallback), true);
                logger.LogResult(result);
                result.ValueAsVoid();
            }
            catch (Exception e)
            {
                logger.LogException(e);
                tcs.TrySetException(e);
                handle.Free();
            }
        }
    }

    public override Task<int> GetIniInt(string iniFilename, string iniSection, string iniKey)
    {
#if DEBUG
        using var logger = LogMethod(iniFilename.ToFormattable(), iniSection.ToFormattable(), iniKey.ToFormattable());
#else
        using var logger = LogMethod();
#endif

        try
        {
            var tcs = new TaskCompletionSource<int>();
            GetIniIntNative(iniFilename, iniSection, iniKey, tcs);
            return tcs.Task;
        }
        catch (Exception e)
        {
            logger.LogException(e);
            throw;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GetIniIntCallback(param_ptr* pOwner, return_value_int32* pResult)
    {
#if DEBUG
        using var logger = LogCallbackMethod(pResult);
#else
        using var logger = LogCallbackMethod(pResult);
#endif

        try
        {

            if (pOwner == null)
            {
                logger.LogException(new ArgumentNullException(nameof(pOwner)));
                return;
            }

            if (GCHandle.FromIntPtr((IntPtr) pOwner) is not {Target: TaskCompletionSource<int> tcs} handle)
            {
                logger.LogException(new InvalidOperationException("Invalid GCHandle."));
                return;
            }
            
            using var result = SafeStructMallocHandle.Create(pResult, true);
            logger.LogResult(result);
            result.SetAsInt32(tcs);
            handle.Free();
        }
        catch (Exception e)
        {
            logger.LogException(e);
            throw;
        }
    }

    private unsafe void GetIniIntNative(ReadOnlySpan<char> iniFilename, ReadOnlySpan<char> iniSection, ReadOnlySpan<char> iniKey, TaskCompletionSource<int> tcs)
    {
#if DEBUG
        using var logger = LogMethod(iniFilename.ToString().ToFormattable(), iniSection.ToString().ToFormattable(), iniKey.ToString().ToFormattable());
#else
        using var logger = LogMethod();
#endif

        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        fixed (char* pIniFilename = iniFilename)
        fixed (char* pIniSection = iniSection)
        fixed (char* pIniKey = iniKey)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_getIniInt(_pOwner, (param_string*) pIniFilename, (param_string*) pIniSection, (param_string*) pIniKey, (param_ptr*) GCHandle.ToIntPtr(handle), &GetIniIntCallback), true);
                logger.LogResult(result);
                result.ValueAsVoid();
            }
            catch (Exception e)
            {
                logger.LogException(e);
                tcs.TrySetException(e);
                handle.Free();
            }
        }
    }
}