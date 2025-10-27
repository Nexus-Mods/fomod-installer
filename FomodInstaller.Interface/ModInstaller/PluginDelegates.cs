using System.Threading.Tasks;

namespace FomodInstaller.Interface
{
    public abstract class PluginDelegates
    {
        /// <summary>
        /// get full list of plugins
        /// </summary>
        /// <param name="activeOnly"></param>
        /// <returns>list of names</returns>
        public abstract Task<string[]> GetAll(bool activeOnly);
        
        /// <summary>
        /// is plugin active
        /// </summary>
        /// <param name="pluginName">filename</param>
        /// <returns></returns>
        public abstract Task<bool> IsActive(string pluginName);
        
        /// <summary>
        /// is plugin present
        /// </summary>
        /// <param name="pluginName"></param>
        /// <returns></returns>
        public abstract Task<bool> IsPresent(string pluginName);
    }
}