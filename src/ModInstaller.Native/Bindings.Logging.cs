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
    public static int SetLoggingCallbacks()
    {
        using var logger = LogMethod();
        
        try
        {
            CreateDefault();

            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            logger.LogException(e);
            return -1;
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "set_logging_callbacks", CallConvs = [typeof(CallConvCdecl)])]
    public static int SetLoggingCallbacks(param_ptr* p_owner,
        delegate* unmanaged[Cdecl]<param_ptr*, param_int, param_string*, param_int> p_log
    )
    {
        using var logger = LogMethod();
        
        try
        {
            var loggerDelegate = new CallbackLogger(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_Log>(new IntPtr(p_log))
            );

            Create(loggerDelegate);

            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            logger.LogException(e);
            return -1;
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "dispose_default_logger", CallConvs = [typeof(CallConvCdecl)]), IsNotConst<IsPtrConst>]
    public static int DisposeDefaultLogger()
    {
        using var logger = LogMethod();
        
        try
        {
            Dispose();
            
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            logger.LogException(e);
            return -1;
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "log_message", CallConvs = [typeof(CallConvCdecl)]), IsNotConst<IsPtrConst>]
    public static void LogMessage(param_int level, [IsConst<IsPtrConst>] param_string* message)
    {
        try
        {
            var messageStr = new string(param_string.ToSpan(message));

            ExternalLog((LogLevel) (int) level, messageStr);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }
}