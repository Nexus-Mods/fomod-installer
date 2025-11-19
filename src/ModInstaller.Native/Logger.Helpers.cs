using Microsoft.Extensions.Logging;

using System;

using ZLogger;

namespace ModInstaller.Native;

internal static partial class LoggerHelperMethod
{
    [ZLoggerMessage(LogLevel.Error, "{caller} - Exception")]
    public static partial void LogException(this ILogger logger, string? caller, Exception exception);
    
    [ZLoggerMessage(LogLevel.Information, "{caller} - Started")]
    public static partial void LogStarted(this ILogger logger, string? caller);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Parameters: {p1}")]
    public static partial void LogParameters1(this ILogger logger, string? caller, string p1);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Parameters: {p1}; {p2}")]
    public static partial void LogParameters2(this ILogger logger, string? caller, string p1, string p2);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Parameters: {p1}; {p2}; {p3}")]
    public static partial void LogParameters3(this ILogger logger, string? caller, string p1, string p2, string p3);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Parameters: {p1}; {p2}; {p3}; {p4}")]
    public static partial void LogParameters4(this ILogger logger, string? caller, string p1, string p2, string p3, string p4);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Parameters: {p1}; {p2}; {p3}; {p4}; {p5}")]
    public static partial void LogParameters5(this ILogger logger, string? caller, string p1, string p2, string p3, string p4, string p5);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Parameters: {p1}; {p2}; {p3}; {p4}; {p5}; {p6}")]
    public static partial void LogParameters6(this ILogger logger, string? caller, string p1, string p2, string p3, string p4, string p5, string p6);
    
    [ZLoggerMessage(LogLevel.Information, "{caller} - {message}")]
    public static partial void LogMessage(this ILogger logger, string? caller, string? message);

    [ZLoggerMessage(LogLevel.Information, "{caller} - Result: {result}")]
    public static partial void LogResult1(this ILogger logger, string? caller, string? result);
    
    [ZLoggerMessage(LogLevel.Information, "{caller} - Finished")]
    public static partial void LogFinished(this ILogger logger, string? caller);
}

internal class WrapperLogger : ILogger
{
    private readonly ILogger _loggerImplementation;
    private readonly string type;
    
    public WrapperLogger(ILogger loggerImplementation, string type)
    {
        _loggerImplementation = loggerImplementation;
        this.type = type;
    }
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _loggerImplementation.Log(logLevel, eventId, state, exception, ((state_, exception_) => $"[{type}] {formatter(state_, exception_)}"));
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _loggerImplementation.IsEnabled(logLevel);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _loggerImplementation.BeginScope(state);
    }
}