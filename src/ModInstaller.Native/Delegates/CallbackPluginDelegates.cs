using BUTR.NativeAOT.Shared;

using FomodInstaller.Interface;

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
        Logger.LogInput();
        try
        {
            using var result = SafeStructMallocHandle.Create(_getAll(_pOwner, activeOnly), true);
            var value = result.ValueAsJson(SourceGenerationContext.Default.StringArray);

            Logger.LogOutput();
            
            return value;
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            throw;
        }
    }

    public override bool IsActive(string pluginName)
    {
        mActiveCache ??= GetAll(true);
        return mActiveCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default;
    }

    public override bool IsPresent(string pluginName)
    {
        mPresentCache ??= GetAll(false);
        return mPresentCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default;
    }
}