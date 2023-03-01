namespace FomodInstaller.Interface
{
    public interface IPluginDelegates
    {
        Task<string[]> GetAll(bool activeOnly);
        Task<bool> IsActive(string pluginName);
        Task<bool> IsPresent(string pluginName);
    }
}