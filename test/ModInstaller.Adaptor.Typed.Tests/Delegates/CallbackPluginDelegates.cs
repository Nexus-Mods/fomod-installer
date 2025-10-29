using FomodInstaller.Interface;

namespace ModInstaller.Adaptor.Typed.Tests.Delegates;

internal class CallbackPluginDelegates : PluginDelegates
{
    private readonly Func<bool, Task<string[]>> _getAllFunc;

    private string[]? mActiveCache;
    private string[]? mPresentCache;

    public CallbackPluginDelegates(
        Func<bool, Task<string[]>> getAllFunc)
    {
        _getAllFunc = getAllFunc;
    }

    public override async Task<string[]> GetAll(bool activeOnly) => await _getAllFunc(activeOnly);

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