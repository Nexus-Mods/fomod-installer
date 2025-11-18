using BUTR.NativeAOT.Shared;

using Microsoft.Extensions.Logging;

using System;

namespace ModInstaller.Native.Adapters;

internal class CallbackLogger : ILogger
{
    private readonly unsafe param_ptr* _pOwner;
    private readonly N_Log _log;
    
    public unsafe CallbackLogger(param_ptr* pOwner,
        N_Log log)
    {
        _pOwner = pOwner;
        _log = log;
    }

    public unsafe void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        if (exception != null)
            message += Environment.NewLine + exception;
        
        fixed (char* pMessage = message)
        {
            try
            {
                var result = _log(_pOwner, (param_int) (int) logLevel, (param_string*) pMessage);
                if (result != 0)
                    throw new Exception($"Failed to log message: {message}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}