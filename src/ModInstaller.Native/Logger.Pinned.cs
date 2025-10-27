using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ModInstaller.Native;

public static partial class Logger
{
    [Conditional("LOGGING")]
    public static unsafe void LogPinned(char* param1, [CallerMemberName] string? caller = null)
    {
        var p1 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param1);
        Log($"{caller} - Pinned: {p1}");
    }
    [Conditional("LOGGING")]
    public static unsafe void LogPinned(char* param1, char* param2, [CallerMemberName] string? caller = null)
    {
        var p1 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param1);
        var p2 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param2);
        Log($"{caller} - Pinned: {p1}; {p2}");
    }
    [Conditional("LOGGING")]
    public static unsafe void LogPinned(char* param1, char* param2, char* param3, [CallerMemberName] string? caller = null)
    {
        var p1 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param1);
        var p2 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param2);
        var p3 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param3);
        Log($"{caller} - Pinned: {p1}; {p2}; {p3}");
    }
    [Conditional("LOGGING")]
    public static unsafe void LogPinned(char* param1, char* param2, char* param3, char* param4, [CallerMemberName] string? caller = null)
    {
        var p1 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param1);
        var p2 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param2);
        var p3 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param3);
        var p4 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(param4);
        Log($"{caller} - Pinned: {p1}; {p2}; {p3}; {p4}");
    }
}