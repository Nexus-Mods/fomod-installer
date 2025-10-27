using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using FomodInstaller.Interface;

namespace FomodInstaller.ModInstaller;

public class DynamicPluginDelegates : PluginDelegates
{
    private Func<object, Task<object>> mGetAll;
    private string[] mActiveCache;
    private string[] mPresentCache;

    public DynamicPluginDelegates(dynamic source)
    {
        mGetAll = source.getAll;
    }

    public override async Task<string[]> GetAll(bool activeOnly)
    {
        dynamic res = await Shared.TimeoutRetry(() => Task.Run(() => mGetAll(activeOnly)));
        if (res != null)
        {
            IEnumerable enu = res;
            return enu.Cast<object>().Select(x => x.ToString()).ToArray();
        }
        else
            return new string[0];
    }

    public override async Task<bool> IsActive(string pluginName)
    {
        if (mActiveCache == null)
        {
            mActiveCache = await GetAll(true);
        }
        return mActiveCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default;
    }

    public override async Task<bool> IsPresent(string pluginName)
    {
        if (mPresentCache == null)
        {
            mPresentCache = await GetAll(false);
        }
        return mPresentCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default;
    }
}