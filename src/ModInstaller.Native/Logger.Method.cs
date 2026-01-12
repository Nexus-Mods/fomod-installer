using BUTR.NativeAOT.Shared;

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ModInstaller.Native;

static partial class Logger
{
    public static LoggerScope LogMethod([CallerMemberName] string? caller = null)
    {
        return new LoggerScope(caller);
    }
    public static LoggerScope LogCallbackMethod(string? caller)
    {
        return new LoggerScope($"{caller}Callback");
    }
    
    public static LoggerScope LogMethod<T1>(T1 param1, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
    {
        return new LoggerScope(caller)
            .WithInput(param1.ToString(null, CultureInfo.InvariantCulture));
    }
    public static LoggerScope LogMethod<T1, T2>(T1 param1, T2 param2, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
    {
        return new LoggerScope(caller)
            .WithInput(param1.ToString(null, CultureInfo.InvariantCulture), param2.ToString(null, CultureInfo.InvariantCulture));
    }
    public static LoggerScope LogMethod<T1, T2, T3>(T1 param1, T2 param2, T3 param3, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
        where T3 : IFormattable
    {
        return new LoggerScope(caller)
            .WithInput(param1.ToString(null, CultureInfo.InvariantCulture), param2.ToString(null, CultureInfo.InvariantCulture), param3.ToString(null, CultureInfo.InvariantCulture));
    }
    public static LoggerScope LogMethod<T1, T2, T3, T4>(T1 param1, T2 param2, T3 param3, T4 param4, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
        where T3 : IFormattable
        where T4 : IFormattable
    {
        return new LoggerScope(caller)
            .WithInput(param1.ToString(null, CultureInfo.InvariantCulture), param2.ToString(null, CultureInfo.InvariantCulture), param3.ToString(null, CultureInfo.InvariantCulture), param4.ToString(null, CultureInfo.InvariantCulture));
    }
    public static LoggerScope LogMethod<T1, T2, T3, T4, T5>(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
        where T3 : IFormattable
        where T4 : IFormattable
        where T5 : IFormattable
    {
        return new LoggerScope(caller)
            .WithInput(param1.ToString(null, CultureInfo.InvariantCulture), param2.ToString(null, CultureInfo.InvariantCulture), param3.ToString(null, CultureInfo.InvariantCulture), param4.ToString(null, CultureInfo.InvariantCulture), param5.ToString(null, CultureInfo.InvariantCulture));
    }
    public static LoggerScope LogMethod<T1, T2, T3, T4, T5, T6>(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, [CallerMemberName] string? caller = null)
        where T1 : IFormattable
        where T2 : IFormattable
        where T3 : IFormattable
        where T4 : IFormattable
        where T5 : IFormattable
        where T6 : IFormattable
    {
        return new LoggerScope(caller)
            .WithInput(param1.ToString(null, CultureInfo.InvariantCulture), param2.ToString(null, CultureInfo.InvariantCulture), param3.ToString(null, CultureInfo.InvariantCulture), param4.ToString(null, CultureInfo.InvariantCulture), param5.ToString(null, CultureInfo.InvariantCulture), param6.ToString(null, CultureInfo.InvariantCulture));
    }
    
    public static unsafe LoggerScope LogCallbackMethod<TResult>(TResult* result, [CallerMemberName] string? caller = null)
        where TResult : unmanaged, IReturnValueSpanFormattable<TResult>
    {
        return new LoggerScope(caller)
            .WithResult(result);
    }
    
    public static unsafe LoggerScope LogMethod<T1>(T1* param1, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
    {
        return new LoggerScope(caller)
            .WithInput(T1.ToSpan(param1).ToString());
    }
    public static unsafe LoggerScope LogCallbackMethod<T1, TResult>(T1* param1, TResult* result, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where TResult : unmanaged, IReturnValueSpanFormattable<TResult>
    {
        return new LoggerScope(caller)
            .WithInput(T1.ToSpan(param1).ToString())
            .WithResult(result);
    }
    
    public static unsafe LoggerScope LogMethod<T1, T2>(T1* param1, T2* param2, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
    {
        return new LoggerScope(caller)
            .WithInput(T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString());
    }
    public static unsafe LoggerScope LogCallbackMethod<T1, T2, TResult>(T1* param1, T2* param2, TResult* result, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where TResult : unmanaged, IReturnValueSpanFormattable<TResult>
    {
        return new LoggerScope(caller)
            .WithInput(T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString())
            .WithResult(result);
    }
    
    public static unsafe LoggerScope LogMethod<T1, T2, T3>(T1* param1, T2* param2, T3* param3, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
    {
        return new LoggerScope(caller)
            .WithInput(T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString(), T3.ToSpan(param3).ToString());
    }
    public static unsafe LoggerScope LogCallbackMethod<T1, T2, T3, TResult>(T1* param1, T2* param2, T3* param3, TResult* result, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
        where TResult : unmanaged, IReturnValueSpanFormattable<TResult>
    {
        return new LoggerScope(caller)
            .WithInput(T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString(), T3.ToSpan(param3).ToString())
            .WithResult(result);
    }
    
    public static unsafe LoggerScope LogMethod<T1, T2, T3, T4>(T1* param1, T2* param2, T3* param3, T4* param4, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
        where T4 : unmanaged, IParameterSpanFormattable<T4>
    {
        return new LoggerScope(caller)
            .WithInput(T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString(), T3.ToSpan(param3).ToString(), T4.ToSpan(param4).ToString());
    }
    public static unsafe LoggerScope LogCallbackMethod<T1, T2, T3, T4, TResult>(T1* param1, T2* param2, T3* param3, T4* param4, TResult* result, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
        where T4 : unmanaged, IParameterSpanFormattable<T4>
        where TResult : unmanaged, IReturnValueSpanFormattable<TResult>
    {
        return new LoggerScope(caller)
            .WithInput(T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString(), T3.ToSpan(param3).ToString(), T4.ToSpan(param4).ToString())
            .WithResult(result);
    }
    
    public static unsafe LoggerScope LogMethod<T1, T2, T3, T4, T5>(T1* param1, T2* param2, T3* param3, T4* param4, T5* param5, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
        where T4 : unmanaged, IParameterSpanFormattable<T4>
        where T5 : unmanaged, IParameterSpanFormattable<T5>
    {
        return new LoggerScope(caller)
            .WithInput(T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString(), T3.ToSpan(param3).ToString(), T4.ToSpan(param4).ToString(), T5.ToSpan(param5).ToString());
    }
    public static unsafe LoggerScope LogCallbackMethod<T1, T2, T3, T4, T5, TResult>(T1* param1, T2* param2, T3* param3, T4* param4, T5* param5, TResult* result, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
        where T4 : unmanaged, IParameterSpanFormattable<T4>
        where T5 : unmanaged, IParameterSpanFormattable<T5>
        where TResult : unmanaged, IReturnValueSpanFormattable<TResult>
    {
        return new LoggerScope(caller)
            .WithInput(T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString(), T3.ToSpan(param3).ToString(), T4.ToSpan(param4).ToString(), T5.ToSpan(param5).ToString())
            .WithResult(result);
    }
    
    public static unsafe LoggerScope LogMethod<T1, T2, T3, T4, T5, T6>(T1* param1, T2* param2, T3* param3, T4* param4, T5* param5, T6* param6, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
        where T4 : unmanaged, IParameterSpanFormattable<T4>
        where T5 : unmanaged, IParameterSpanFormattable<T5>
        where T6 : unmanaged, IParameterSpanFormattable<T6>
    {
        return new LoggerScope(caller)
            .WithInput(T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString(), T3.ToSpan(param3).ToString(), T4.ToSpan(param4).ToString(), T5.ToSpan(param5).ToString(), T6.ToSpan(param6).ToString());
    }
    public static unsafe LoggerScope LogCallbackMethod<T1, T2, T3, T4, T5, T6, TResult>(T1* param1, T2* param2, T3* param3, T4* param4, T5* param5, T6* param6, TResult* result, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IParameterSpanFormattable<T1>
        where T2 : unmanaged, IParameterSpanFormattable<T2>
        where T3 : unmanaged, IParameterSpanFormattable<T3>
        where T4 : unmanaged, IParameterSpanFormattable<T4>
        where T5 : unmanaged, IParameterSpanFormattable<T5>
        where T6 : unmanaged, IParameterSpanFormattable<T6>
        where TResult : unmanaged, IReturnValueSpanFormattable<TResult>
    {
        return new LoggerScope(caller)
            .WithInput(T1.ToSpan(param1).ToString(), T2.ToSpan(param2).ToString(), T3.ToSpan(param3).ToString(), T4.ToSpan(param4).ToString(), T5.ToSpan(param5).ToString(), T6.ToSpan(param6).ToString())
            .WithResult(result);
    }
}