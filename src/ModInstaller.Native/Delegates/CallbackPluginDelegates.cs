using BUTR.NativeAOT.Shared;

using FomodInstaller.Interface;

using System;
using System.Linq;

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

    public override unsafe string[] GetAll(bool activeOnly)
    {
#if DEBUG
        using var logger = LogMethod(activeOnly.ToFormattable());
#else
        using var logger = LogMethod();
#endif

        try
        {
            using var result = SafeStructMallocHandle.Create(_getAll(_pOwner, activeOnly), true);
            return result.ValueAsJson(SourceGenerationContext.Default.StringArray) ?? [];
        }
        catch (Exception e)
        {
            logger.LogException(e);
            throw;
        }
    }

    public override bool IsActive(string pluginName)
    {
        mActiveCache ??= GetAll(true);
        return mActiveCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != null;
    }

    public override bool IsPresent(string pluginName)
    {
        mPresentCache ??= GetAll(false);
        return mPresentCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != null;
    }
}