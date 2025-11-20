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
        using var logger = LogMethod(size);
#endif
        
        try
        {
            var result = Allocator.Alloc(size);
#if DEBUG
            logger.LogResult(new IntPtr(result), "x16");
#endif
            return result;
        }
        catch (Exception e)
        {
#if DEBUG
            logger.LogException(e);
#else
            LogMethod(size).LogException(e);
#endif
            return null;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "common_dealloc", CallConvs = [typeof(CallConvCdecl)])]
    public static void CommonDealloc([IsConst<IsPtrConst>] param_ptr* ptr)
    {
#if DEBUG
        using var logger = LogMethod(ptr);
#endif
        
        try
        {
            Allocator.Free(ptr);
        }
        catch (Exception e)
        {
#if DEBUG
            logger.LogException(e);
#else
            LogMethod(ptr).LogException(e);
#endif
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "common_alloc_alive_count", CallConvs = [typeof(CallConvCdecl)])]
    public static int CommonAllocAliveCount()
    {
#if DEBUG
        using var logger = LogMethod();
#endif
        
        try
        {
#if TRACK_ALLOCATIONS
            var result = Allocator.GetCurrentAllocations();
#else
            var result = 0;
#endif

#if DEBUG
            logger.LogResult(result);
#endif
            return result;
        }
        catch (Exception e)
        {
#if DEBUG
            logger.LogException(e);
#else
            LogMethod().LogException(e);
#endif
            return -1;
        }
    }
}