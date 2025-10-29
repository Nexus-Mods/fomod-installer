using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ModInstaller.Native;

public static partial class Logger
{
    [Conditional("LOGGING")]
    public static void LogOutput([CallerMemberName] string? caller = null)
    {
        Log($"{caller} - Finished");
    }
    [Conditional("LOGGING")]
    public static void LogOutput(string param, [CallerMemberName] string? caller = null)
    {
        Log($"{caller} - Finished: {param}");
    }
    [Conditional("LOGGING")]
    public static void LogOutput(ReadOnlySpan<char> param, [CallerMemberName] string? caller = null)
    {
        Log($"{caller} - Finished: {param}");
    }
    [Conditional("LOGGING")]
    public static void LogOutput<T1>(T1 result, [CallerMemberName] string? caller = null)
    {
        if (Bindings.CustomSourceGenerationContext.GetTypeInfo(typeof(T1)) is JsonTypeInfo<T1> jsonTypeInfo)
            Log($"{caller} - Finished: JSON - {JsonSerializer.Serialize(result, jsonTypeInfo)}");
        else
            Log($"{caller} - Finished: RAW - {result?.ToString()}");
    }
    [Conditional("LOGGING")]
    public static void LogOutput(bool result, [CallerMemberName] string? caller = null)
    {
        Log($"{caller} - Finished: {result}");
    }
}