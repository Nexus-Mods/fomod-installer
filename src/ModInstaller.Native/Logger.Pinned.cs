using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ZLogger;

namespace ModInstaller.Native;

internal static partial class LoggerHelperPinned
{
    [ZLoggerMessage(LogLevel.Information, "{caller} - Pinned: {p1}")]
    public static partial void LogPinned1(this ILogger logger, string? caller, string? p1);
    
    [ZLoggerMessage(LogLevel.Information, "{caller} - Pinned: {p1}; {p2}")]
    public static partial void LogPinned2(this ILogger logger, string? caller, string? p1, string? p2);
    
    [ZLoggerMessage(LogLevel.Information, "{caller} - Pinned: {p1}; {p2}; {p3}")]
    public static partial void LogPinned3(this ILogger logger, string? caller, string? p1, string? p2, string? p3);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Pinned: {p1}; {p2}; {p3}; {p4}")]
    public static partial void LogPinned4(this ILogger logger, string? caller, string? p1, string? p2, string? p3, string? p4);
}

static partial class Logger
{
    [Conditional("LOGGING")]
    public static unsafe void LogPinned(char* param1, [CallerMemberName] string? caller = null)
    {
        var p1 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param1);
        NativeInstance?.LogPinned1(caller, p1.ToString());
    }
    [Conditional("LOGGING")]
    public static unsafe void LogPinned(char* param1, char* param2, [CallerMemberName] string? caller = null)
    {
        var p1 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param1);
        var p2 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param2);
        NativeInstance?.LogPinned2(caller, p1.ToString(), p2.ToString());
    }
    [Conditional("LOGGING")]
    public static unsafe void LogPinned(char* param1, char* param2, char* param3, [CallerMemberName] string? caller = null)
    {
        var p1 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param1);
        var p2 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param2);
        var p3 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param3);
        NativeInstance?.LogPinned3(caller, p1.ToString(), p2.ToString(), p3.ToString());
    }
    [Conditional("LOGGING")]
    public static unsafe void LogPinned(char* param1, char* param2, char* param3, char* param4, [CallerMemberName] string? caller = null)
    {
        var p1 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param1);
        var p2 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param2);
        var p3 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param3);
        var p4 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param4);
        NativeInstance?.LogPinned4(caller, p1.ToString(), p2.ToString(), p3.ToString(), p4.ToString());
    }
}