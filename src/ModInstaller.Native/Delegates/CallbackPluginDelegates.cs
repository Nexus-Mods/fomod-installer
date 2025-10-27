using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BUTR.NativeAOT.Shared;
using FomodInstaller.Interface;

namespace ModInstaller.Native.Adapters;

internal class CallbackPluginDelegates : PluginDelegates
{
    private readonly unsafe param_ptr* _pOwner;
    private readonly N_Plugins_GetAll _getAll;
    
    private string[]? mActiveCache;
    private string[]? mPresentCache;
    
    public unsafe CallbackPluginDelegates(param_ptr* pOwner,
        N_Plugins_GetAll getAll)
    {
        _pOwner = pOwner;
        _getAll = getAll;
    }
   
    public override async Task<string[]> GetAll(bool activeOnly)
    {
        var tcs = new TaskCompletionSource<string[]>();
        GetAllNative(activeOnly, tcs);
        return await tcs.Task;
    }

    public override async Task<bool> IsActive(string pluginName)
    {
        mActiveCache ??= await GetAll(true);
        return mActiveCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default;
    }

    public override async Task<bool> IsPresent(string pluginName)
    {
        mPresentCache ??= await GetAll(false);
        return mPresentCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default;
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void GetAllCallback(param_ptr* pOwner, return_value_json* pResult)
    {
        Logger.LogCallbackInput(pResult);

        if (pOwner == null)
        {
            Logger.LogException(new ArgumentNullException(nameof(pOwner)));
            return;
        }

        if (GCHandle.FromIntPtr((IntPtr) pOwner) is not { Target: TaskCompletionSource<string[]?> tcs } handle)
        {
            Logger.LogException(new InvalidOperationException("Invalid GCHandle."));
            return;
        }

        using var result = SafeStructMallocHandle.Create(pResult, true);
        result.SetAsJson(tcs, SourceGenerationContext.Default.StringArray);
        handle.Free();

        Logger.LogOutput();
    }

    private unsafe void GetAllNative(bool activeOnly, TaskCompletionSource<string[]> tcs)
    {
        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        try
        {
            using var result = SafeStructMallocHandle.Create(_getAll(_pOwner, activeOnly, (param_ptr*) GCHandle.ToIntPtr(handle), &GetAllCallback), true);
            result.ValueAsVoid();

            Logger.LogOutput();
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            tcs.TrySetException(e);
            handle.Free();
        }
    }
}