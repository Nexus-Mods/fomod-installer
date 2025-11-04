using System.Threading.Tasks;

namespace FomodInstaller.Interface
{
    public abstract class ContextDelegates
    {
        /// <summary>
        /// app version
        /// </summary>
        /// <returns>version string of running application</returns>
        public abstract Task<string> GetAppVersion();
        
        /// <summary>
        /// game version
        /// </summary>
        /// <returns>version string of the game being managed</returns>
        public abstract Task<string> GetCurrentGameVersion();
        
        /// <summary>
        /// script extender version
        /// </summary>
        /// <param name="extender">extender id</param>
        /// <returns>version string of the extender, if installed</returns>
        public abstract Task<string> GetExtenderVersion(string extender);
        
        /// <summary>
        /// is extender present.
        /// C# only
        /// </summary>
        public abstract Task<bool> IsExtenderPresent();
        
        /// <summary>
        /// file exists.
        /// C# only
        /// </summary>
        /// <param name="fileName">filepath</param>
        public abstract Task<bool> CheckIfFileExists(string fileName);
        
        /// <summary>
        /// get data file content
        /// C# only
        /// </summary>
        /// <param name="dataFile">filepath</param>
        public abstract Task<byte[]> GetExistingDataFile(string dataFile);
        
        /// <summary>
        /// get data file list
        /// C# only
        /// </summary>
        /// <param name="folderPath">path</param>
        /// <param name="searchFilter">filter</param>
        /// <param name="isRecursive">is recursive</param>
        /// <returns>list of names</returns>
        public abstract Task<string[]> GetExistingDataFileList(string folderPath, string searchFilter, bool isRecursive);
    }
}