using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Runtime.CompilerServices;

using ZLogger;

namespace ModInstaller.Native;

internal static partial class LoggerHelperMessage
{
    [ZLoggerMessage(LogLevel.Information, "{caller} - {message}")]
    public static partial void LogMessage(this ILogger logger, string? caller, string? message);
}

static partial class Logger
{
    [Conditional("LOGGING")]
    public static void LogMessage(string message, [CallerMemberName] string? caller = null)
    {
        NativeInstance?.LogMessage(caller, message);
    }
}