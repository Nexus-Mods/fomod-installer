using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using ZLogger;

namespace ModInstaller.Native;

internal static partial class LoggerHelperException
{
    [ZLoggerMessage(LogLevel.Error, "{caller} - Exception")]
    public static partial void LogException(this ILogger logger, string? caller, Exception exception);
}

static partial class Logger
{
    [Conditional("LOGGING")]
    public static void LogException(Exception e, [CallerMemberName] string? caller = null)
    {
        NativeInstance?.LogException(caller, e);
    }
}