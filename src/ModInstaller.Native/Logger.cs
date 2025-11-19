using Microsoft.Extensions.Logging;

using ModInstaller.Native.Adapters;

using System;

using ZLogger;
using ZLogger.Providers;

namespace ModInstaller.Native;

internal static partial class Logger
{
    private static string[] _logLevel4Letters =
    [
        "DEBG", // Trace
        "DEBG", // Debug
        "INFO", // Information
        "WARN", // Warning
        "ERRO", // Error
        "ERRO", // Critical
        "DEBG", // None
    ];
    
    private static readonly string _logFilePathBase = $"{Environment.GetEnvironmentVariable("APPDATA")}";
    private static ILoggerFactory? Factory { get; set; }
    private static ILogger? NativeInstance { get; set; }
    private static ILogger? ExternalInstance { get; set; }

    public static void CreateDefault()
    {
        Factory?.Dispose();
        
        Factory = LoggerFactory.Create(logging =>
        {
#if DEBUG_
            logging.SetMinimumLevel(LogLevel.Trace);
            var configure = (ZLoggerRollingFileOptions options) =>
            {
                options.RollingSizeKB = 8 * 1024 * 1024;
                options.FileShared = false;
                options.FilePathSelector = (date, sequence) => $"{_logFilePathBase}\\vortex_devel\\FOMOD.ModInstaller{sequence}.log";
                options.FullMode = BackgroundBufferFullMode.Grow;
                options.CaptureThreadInfo = true;
                options.IncludeScopes = true;
                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter($"[{2}] {0:yyyy-MM-dd'T'HH:mm:ss.fff'Z'} [{1}] ", (in template, in info) => template.Format(info.Timestamp.Utc, _logLevel4Letters[(int) info.LogLevel], info.Category.Name));
                    formatter.SetExceptionFormatter((writer, ex) => Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));
                });
            };
#else
            logging.SetMinimumLevel(LogLevel.Information);
            var configure = (ZLoggerRollingFileOptions options) =>
            {
                options.RollingSizeKB = 8 * 1024 * 1024;
                options.FileShared = false;
                options.FilePathSelector = (date, sequence) => $"{_logFilePathBase}\\Vortex\\FOMOD.ModInstaller{sequence}.log";
                options.FullMode = BackgroundBufferFullMode.Block;
                options.CaptureThreadInfo = true;
                options.IncludeScopes = true;
                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter($"[{2}] {0:yyyy-MM-dd'T'HH:mm:ss.fff'Z'} [{1}] ", (in template, in info) => template.Format(info.Timestamp.Utc, _logLevel4Letters[(int) info.LogLevel], info.Category.Name));
                    formatter.SetExceptionFormatter((writer, ex) => Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));
                });
            };
#endif
            logging.AddZLoggerRollingFile(configure);
        });
        
        NativeInstance   = Factory.CreateLogger("C# ");
        ExternalInstance = Factory.CreateLogger("C++");
    }

    public static void Create(CallbackLogger logger)
    {
        Factory?.Dispose();
        
        NativeInstance   = new WrapperLogger(logger, "FOMOD C# ");
        ExternalInstance = new WrapperLogger(logger, "FOMOD C++");
    }

    public static void ExternalLog(LogLevel level, string message)
    {
        ExternalInstance?.Log(level, message, null!);
    }
    
    public static void Dispose()
    {
        Factory?.Dispose();
    }
}