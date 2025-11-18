using BUTR.NativeAOT.Shared;

using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Runtime.CompilerServices;

using ZLogger;

namespace ModInstaller.Native;

internal static partial class LoggerHelperCallback
{
    [ZLoggerMessage(LogLevel.Information, "{caller} - Starting: {param1}")]
    public static partial void LogCallbackInput(this ILogger logger, string? caller, string param1);
}

static partial class Logger
{
    [Conditional("LOGGING")]
    public static unsafe void LogCallbackInput<T1>(T1* param1, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IReturnValueSpanFormattable<T1>
    {
        NativeInstance?.LogCallbackInput(caller, T1.ToSpan(param1).ToString());
    }
}