using BUTR.NativeAOT.Shared;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ModInstaller.Native;

public static partial class Logger
{
    [Conditional("LOGGING")]
    public static void LogMessage(string message, [CallerMemberName] string? caller = null)
    {
        Log($"{caller} - {message}");
    }

    [Conditional("LOGGING")]
    public static void LogInput([CallerMemberName] string? caller = null)
    {
        Log($"{caller} - Starting");
    }
    [Conditional("LOGGING")]
    public static void LogAsyncInput(string caller)
    {
        Log($"{caller} - Starting");
    }
    [Conditional("LOGGING")]
    public static void LogAsyncInput(string str, string caller)
    {
        Log($"{caller} - Starting: {str}");
    }
    [Conditional("LOGGING")]
    public static void LogInput<T1>(T1 param1, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
    {
        Log($"{caller} - Starting: {param1.ToString(null, CultureInfo.InvariantCulture)}");
    }
    [Conditional("LOGGING")]
    public static void LogInput<T1, T2>(T1 param1, T2 param2, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
    {
        Log($"{caller} - Starting: {param1.ToString(null, CultureInfo.InvariantCulture)}; {param2.ToString(null, CultureInfo.InvariantCulture)}");
    }
    [Conditional("LOGGING")]
    public static void LogInput<T1, T2, T3>(T1 param1, T2 param2, T3 param3, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
        where T3 : IFormattable
    {
        Log($"{caller} - Starting: {param1.ToString(null, CultureInfo.InvariantCulture)}; {param2.ToString(null, CultureInfo.InvariantCulture)}; {param3.ToString(null, CultureInfo.InvariantCulture)}");
    }
    [Conditional("LOGGING")]
    public static void LogInput<T1, T2, T3, T4>(T1 param1, T2 param2, T3 param3, T4 param4, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
        where T3 : IFormattable
        where T4 : IFormattable
    {
        Log($"{caller} - Starting: {param1.ToString(null, CultureInfo.InvariantCulture)}; {param2.ToString(null, CultureInfo.InvariantCulture)}; {param3.ToString(null, CultureInfo.InvariantCulture)}; {param4.ToString(null, CultureInfo.InvariantCulture)}");
    }
    [Conditional("LOGGING")]
    public static void LogInput<T1, T2, T3, T4, T5>(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
        where T3 : IFormattable
        where T4 : IFormattable
        where T5 : IFormattable
    {
        Log($"{caller} - Starting: {param1.ToString(null, CultureInfo.InvariantCulture)}; {param2.ToString(null, CultureInfo.InvariantCulture)}; {param3.ToString(null, CultureInfo.InvariantCulture)}; {param4.ToString(null, CultureInfo.InvariantCulture)}; {param5.ToString(null, CultureInfo.InvariantCulture)}");
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
        Log($"{caller} - Starting: {param1.ToString(null, CultureInfo.InvariantCulture)}; {param2.ToString(null, CultureInfo.InvariantCulture)}; {param3.ToString(null, CultureInfo.InvariantCulture)}; {param4.ToString(null, CultureInfo.InvariantCulture)}; {param5.ToString(null, CultureInfo.InvariantCulture)}; {param6.ToString(null, CultureInfo.InvariantCulture)}");
    }
    [Conditional("LOGGING")]
    public static unsafe void LogInput<T1>(T1* param1, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
    {
        Log($"{caller} - Starting: {T1.ToSpan(param1)}");
    }
    [Conditional("LOGGING")]
    public static unsafe void LogInput<T1, T2>(T1* param1, T2* param2, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
    {
        Log($"{caller} - Starting: {T1.ToSpan(param1)}; {T2.ToSpan(param2)}");
    }
    [Conditional("LOGGING")]
    public static unsafe void LogInput<T1, T2, T3>(T1* param1, T2* param2, T3* param3, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
    {
        Log($"{caller} - Starting: {T1.ToSpan(param1)}; {T2.ToSpan(param2)}; {T3.ToSpan(param3)}");
    }
    [Conditional("LOGGING")]
    public static unsafe void LogInput<T1, T2, T3, T4>(T1* param1, T2* param2, T3* param3, T4* param4, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
        where T4 : unmanaged, IParameterSpanFormattable<T4>
    {
        Log($"{caller} - Starting: {T1.ToSpan(param1)}; {T2.ToSpan(param2)}; {T3.ToSpan(param3)}; {T4.ToSpan(param4)}");
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
        Log($"{caller} - Starting: {T1.ToSpan(param1)}; {T2.ToSpan(param2)}; {T3.ToSpan(param3)}; {T4.ToSpan(param4)}; {T5.ToSpan(param5)}; {T6.ToSpan(param6)}");
    }
}