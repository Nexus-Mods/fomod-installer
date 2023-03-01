namespace FomodInstaller.Interface
{
    public interface IContextDelegates
    {
        Task<bool> CheckIfFileExists(string fileName);
        Task<string> GetAppVersion();
        Task<string> GetCurrentGameVersion();
        Task<byte[]> GetExistingDataFile(string dataFile);
        Task<string[]> GetExistingDataFileList(string folderPath, string searchFilter, bool isRecursive);
        Task<string> GetExtenderVersion(string extender);
        Task<bool> IsExtenderPresent();
    }
}