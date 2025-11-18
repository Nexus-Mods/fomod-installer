using BUTR.NativeAOT.Shared;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ModInstaller.Native;


public static unsafe partial class Bindings
{
    [UnmanagedCallersOnly(EntryPoint = "common_alloc", CallConvs = [typeof(CallConvCdecl)]), IsNotConst<IsPtrConst>]
    public static void* CommonAlloc(nuint size)
    {
#if DEBUG
        Logger.LogInput(size);
#endif
        try
        {
            var result = Allocator.Alloc(size);

#if DEBUG
            Logger.LogOutput(new IntPtr(result).ToString("x16"), nameof(CommonAlloc));
#endif
            return result;
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return null;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "common_dealloc", CallConvs = [typeof(CallConvCdecl)])]
    public static void CommonDealloc([IsConst<IsPtrConst>] param_ptr* ptr)
    {
#if DEBUG
        Logger.LogInput();
#endif
        try
        {
            Allocator.Free(ptr);

#if DEBUG
            Logger.LogOutput(new IntPtr(ptr).ToString("x16"), nameof(CommonDealloc));
#endif
        }
        catch (Exception e)
        {
            Logger.LogException(e);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "common_alloc_alive_count", CallConvs = [typeof(CallConvCdecl)])]
    public static int CommonAllocAliveCount()
    {
#if DEBUG
        Logger.LogInput();
#endif
        try
        {
#if TRACK_ALLOCATIONS
            var result = Allocator.GetCurrentAllocations();
#else
            var result = 0;
#endif

#if DEBUG
            Logger.LogOutput(result);
#endif
            return result;
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return -1;
        }
    }
}