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

    public override string[] GetAll(bool activeOnly)
    {
        dynamic res = Shared.TimeoutRetrySync(() => mGetAll(activeOnly));
        if (res != null)
        {
            IEnumerable enu = res;
            return enu.Cast<object>().Select(x => x.ToString()).ToArray();
        }
        else
            return new string[0];
    }

    public override bool IsActive(string pluginName)
    {
        if (mActiveCache == null)
        {
            mActiveCache = GetAll(true);
        }
        return mActiveCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default;
    }

    public override bool IsPresent(string pluginName)
    {
        if (mPresentCache == null)
        {
            mPresentCache = GetAll(false);
        }
        return mPresentCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default;
    }
}