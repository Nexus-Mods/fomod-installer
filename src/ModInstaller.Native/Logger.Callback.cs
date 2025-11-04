using BUTR.NativeAOT.Shared;

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ModInstaller.Native;

public static partial class Logger
{
    [Conditional("LOGGING")]
    public static unsafe void LogCallbackInput<T1>(T1* param1, [CallerMemberName] string? caller = null)
        where T1 : unmanaged, IReturnValueSpanFormattable<T1>
    {
        Log($"{caller} - Starting: {T1.ToSpan(param1)}");
    }
}