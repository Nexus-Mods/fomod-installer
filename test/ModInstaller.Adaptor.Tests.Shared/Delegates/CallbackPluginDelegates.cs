using FomodInstaller.Interface;

namespace ModInstaller.Adaptor.Tests.Shared.Delegates;

public class CallbackPluginDelegates : PluginDelegates
{
    private readonly Func<bool, string[]> _getAllFunc;

    private string[]? mActiveCache;
    private string[]? mPresentCache;

    public CallbackPluginDelegates(
        Func<bool, string[]> getAllFunc)
    {
        _getAllFunc = getAllFunc;
    }

    public override string[] GetAll(bool activeOnly) => _getAllFunc(activeOnly);

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