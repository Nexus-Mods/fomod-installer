using BUTR.NativeAOT.Shared;

using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

using ZLogger;

namespace ModInstaller.Native;

internal static partial class LoggerHelperInput
{
    [ZLoggerMessage(LogLevel.Information, "{caller} - Starting")]
    public static partial void LogStarting0(this ILogger logger, string? caller);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Starting: {p1}")]
    public static partial void LogStarting1(this ILogger logger, string? caller, string p1);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Starting: {p1}; {p2}")]
    public static partial void LogStarting2(this ILogger logger, string? caller, string p1, string p2);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Starting: {p1}; {p2}; {p3}")]
    public static partial void LogStarting3(this ILogger logger, string? caller, string p1, string p2, string p3);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Starting: {p1}; {p2}; {p3}; {p4}")]
    public static partial void LogStarting4(this ILogger logger, string? caller, string p1, string p2, string p3, string p4);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Starting: {p1}; {p2}; {p3}; {p4}; {p5}")]
    public static partial void LogStarting5(this ILogger logger, string? caller, string p1, string p2, string p3, string p4, string p5);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Starting: {p1}; {p2}; {p3}; {p4}; {p5}; {p6}")]
    public static partial void LogStarting6(this ILogger logger, string? caller, string p1, string p2, string p3, string p4, string p5, string p6);
}

static partial class Logger
{
    [Conditional("LOGGING")]
    public static void LogInput([CallerMemberName] string? caller = null)
    {
        NativeInstance?.LogStarting0(caller);
    }
    [Conditional("LOGGING")]
    public static void LogAsyncInput(string caller)
    {
        NativeInstance?.LogStarting0(caller);
    }
    [Conditional("LOGGING")]
    public static void LogInput<T1>(T1 param1, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
    {
        NativeInstance?.LogStarting1(caller, param1.ToString(null, CultureInfo.InvariantCulture));
    }
    [Conditional("LOGGING")]
    public static void LogInput<T1, T2>(T1 param1, T2 param2, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
    {
        NativeInstance?.LogStarting2(caller, param1.ToString(null, CultureInfo.InvariantCulture), param2.ToString(null, CultureInfo.InvariantCulture));
    }
    [Conditional("LOGGING")]
    public static void LogInput<T1, T2, T3>(T1 param1, T2 param2, T3 param3, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
        where T3 : IFormattable
    {
        NativeInstance?.LogStarting3(caller, param1.ToString(null, CultureInfo.InvariantCulture), param2.ToString(null, CultureInfo.InvariantCulture), param3.ToString(null, CultureInfo.InvariantCulture));
    }
    [Conditional("LOGGING")]
    public static void LogInput<T1, T2, T3, T4>(T1 param1, T2 param2, T3 param3, T4 param4, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
        where T3 : IFormattable
        where T4 : IFormattable
    {
        NativeInstance?.LogStarting4(caller, param1.ToString(null, CultureInfo.InvariantCulture), param2.ToString(null, CultureInfo.InvariantCulture), param3.ToString(null, CultureInfo.InvariantCulture), param4.ToString(null, CultureInfo.InvariantCulture));
    }
    [Conditional("LOGGING")]
    public static void LogInput<T1, T2, T3, T4, T5>(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
        where T3 : IFormattable
        where T4 : IFormattable
        where T5 : IFormattable
    {
        NativeInstance?.LogStarting5(caller, param1.ToString(null, CultureInfo.InvariantCulture), param2.ToString(null, CultureInfo.InvariantCulture), param3.ToString(null, CultureInfo.InvariantCulture), param4.ToString(null, CultureInfo.InvariantCulture), param5.ToString(null, CultureInfo.InvariantCulture));
    }
    [Conditional("LOGGING")]
    public static void LogInput<T1, T2, T3, T4, T5, T6>(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
        where T3 : IFormattable
        where T4 : IFormattable
        where T5 : IFormattable
        where T6 : IFormattable
    {
        NativeInstance?.LogStarting6(caller, param1.ToString(null, CultureInfo.InvariantCulture), param2.ToString(null, CultureInfo.InvariantCulture), param3.ToString(null, CultureInfo.InvariantCulture), param4.ToString(null, CultureInfo.InvariantCulture), param5.ToString(null, CultureInfo.InvariantCulture), param6.ToString(null, CultureInfo.InvariantCulture));
    }
    [Conditional("LOGGING")]
    public static unsafe void LogInput<T1>(T1* param1, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
    {
        NativeInstance?.LogStarting1(caller, T1.ToSpan(param1).ToString());
    }
    [Conditional("LOGGING")]
    public static unsafe void LogInput<T1, T2>(T1* param1, T2* param2, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
    {
        NativeInstance?.LogStarting2(caller, T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString());
    }
    [Conditional("LOGGING")]
    public static unsafe void LogInput<T1, T2, T3>(T1* param1, T2* param2, T3* param3, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
    {
        NativeInstance?.LogStarting3(caller, T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString(), T3.ToSpan(param3).ToString());
    }
    [Conditional("LOGGING")]
    public static unsafe void LogInput<T1, T2, T3, T4>(T1* param1, T2* param2, T3* param3, T4* param4, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
        where T4 : unmanaged, IParameterSpanFormattable<T4>
    {
        NativeInstance?.LogStarting4(caller, T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString(), T3.ToSpan(param3).ToString(), T4.ToSpan(param4).ToString());
    }
    [Conditional("LOGGING")]
    public static unsafe void LogInput<T1, T2, T3, T4, T5>(T1* param1, T2* param2, T3* param3, T4* param4, T5* param5, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
        where T4 : unmanaged, IParameterSpanFormattable<T4>
        where T5 : unmanaged, IParameterSpanFormattable<T5>
    {
        NativeInstance?.LogStarting5(caller, T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString(), T3.ToSpan(param3).ToString(), T4.ToSpan(param4).ToString(), T5.ToSpan(param5).ToString());
    }
    [Conditional("LOGGING")]
    public static unsafe void LogInput<T1, T2, T3, T4, T5, T6>(T1* param1, T2* param2, T3* param3, T4* param4, T5* param5, T6* param6, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
        where T4 : unmanaged, IParameterSpanFormattable<T4>
        where T5 : unmanaged, IParameterSpanFormattable<T5>
        where T6 : unmanaged, IParameterSpanFormattable<T6>
    {
        NativeInstance?.LogStarting6(caller, T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString(), T3.ToSpan(param3).ToString(), T4.ToSpan(param4).ToString(), T5.ToSpan(param5).ToString(), T6.ToSpan(param6).ToString());
    }
}