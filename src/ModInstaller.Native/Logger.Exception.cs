using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ModInstaller.Native;

public static partial class Logger
{
    [Conditional("LOGGING")]
    public static void LogException(Exception e, [CallerMemberName] string? caller = null)
    {
        Log($"{caller} - Exception: {e}");
    }
}