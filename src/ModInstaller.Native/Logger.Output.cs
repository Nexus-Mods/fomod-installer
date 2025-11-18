using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using ZLogger;

namespace ModInstaller.Native;

internal static partial class LoggerHelperOutput
{
    [ZLoggerMessage(LogLevel.Information, "{caller} - Finished")]
    public static partial void LogOutput0(this ILogger logger, string? caller);
    
    [ZLoggerMessage(LogLevel.Information, "{caller} - Finished: {param}")]
    public static partial void LogOutput1(this ILogger logger, string? caller, string? param);
}

static partial class Logger
{
    [Conditional("LOGGING")]
    public static void LogOutput([CallerMemberName] string? caller = null)
    {
        NativeInstance?.LogOutput0(caller);
    }
    [Conditional("LOGGING")]
    public static void LogOutput(string param, [CallerMemberName] string? caller = null)
    {
        NativeInstance?.LogOutput1(caller, param);
    }
    [Conditional("LOGGING")]
    public static void LogOutput(ReadOnlySpan<char> param, [CallerMemberName] string? caller = null)
    {
        NativeInstance?.LogOutput1(caller, param.ToString());
    }
    [Conditional("LOGGING")]
    public static void LogOutput<T1>(T1 result, [CallerMemberName] string? caller = null)
    {
        if (Bindings.CustomSourceGenerationContext.GetTypeInfo(typeof(T1)) is JsonTypeInfo<T1> jsonTypeInfo)
            NativeInstance?.LogOutput1(caller, JsonSerializer.Serialize(result, jsonTypeInfo));
        else
            NativeInstance?.LogOutput1(caller, result?.ToString());
    }
    [Conditional("LOGGING")]
    public static void LogOutput(bool result, [CallerMemberName] string? caller = null)
    {
        NativeInstance?.LogOutput1(caller, result.ToString());
    }
}