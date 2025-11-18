using BUTR.NativeAOT.Shared;

using Microsoft.Extensions.Logging;

using ModInstaller.Native.Adapters;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ModInstaller.Native;

public static unsafe partial class Bindings
{
    [UnmanagedCallersOnly(EntryPoint = "set_default_logging_callbacks", CallConvs = [typeof(CallConvCdecl)])]
    public static return_value_void* SetLoggingCallbacks()
    {
        Logger.LogInput();
        try
        {
            Logger.CreateDefault();

            return return_value_void.AsValue(false);
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return return_value_void.AsException(e, false);
        }
        finally
        {
            Logger.LogOutput();
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "set_logging_callbacks", CallConvs = [typeof(CallConvCdecl)])]
    public static return_value_void* SetLoggingCallbacks(param_ptr* p_owner,
        delegate* unmanaged[Cdecl]<param_ptr*, param_int, param_string*, param_int> p_log
    )
    {
        Logger.LogInput();
        try
        {
            var loggerDelegate = new CallbackLogger(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_Log>(new IntPtr(p_log))
            );

            Logger.Create(loggerDelegate);

            return return_value_void.AsValue(false);
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return return_value_void.AsException(e, false);
        }
        finally
        {
            Logger.LogOutput();
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "dispose_default_logger", CallConvs = [typeof(CallConvCdecl)]), IsNotConst<IsPtrConst>]
    public static return_value_void* DisposeDefaultLogger()
    {
        try
        {
            Logger.Dispose();
            
            return return_value_void.AsValue(false);
        }
        catch (Exception e)
        {
            return return_value_void.AsException(e, false);
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "log", CallConvs = [typeof(CallConvCdecl)]), IsNotConst<IsPtrConst>]
    public static void Log(param_int level, [IsConst<IsPtrConst>] param_string* message)
    {
        try
        {
            var messageStr = new string(param_string.ToSpan(message));

            Logger.Log((LogLevel) (int) level, messageStr);
        }
        catch (Exception e)
        {
            //Logger.LogException(e);
        }
    }
}