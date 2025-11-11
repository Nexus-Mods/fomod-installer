using TestData;

namespace ModInstaller.IPC.Tests.Delegates;

/// <summary>
/// Wraps all delegate implementations for IPC testing
/// </summary>
internal class IPCDelegates
{
    private readonly InstallData _testData;

    public IPCDelegates(InstallData testData)
    {
        _testData = testData;
    }

    // Context delegates
    public string GetAppVersion() => _testData.AppVersion;
    public string GetCurrentGameVersion() => _testData.GameVersion;
    public string GetExtenderVersion(string extender) => _testData.ExtenderVersion;
    public Task<bool> IsExtenderPresent() => Task.FromResult(false);

    public Task<bool> CheckIfFileExists(string fileName) => Task.FromResult(File.Exists(fileName));
    public Task<byte[]> GetExistingDataFile(string dataFile) => Task.FromResult(File.ReadAllBytes(dataFile));
    public Task<string[]> GetExistingDataFileList(string folderPath, string searchFilter, bool isRecursive) => Task.FromResult(Directory.GetDirectories(folderPath, searchFilter, isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));

    public string[] GetAllPlugins(bool activeOnly) => _testData.InstalledPlugins.ToArray();

    // INI delegates (not commonly used in tests)
    public Task<string> GetIniString(string iniFileName, string section, string key)
    {
        return Task.FromResult("");
    }

    public Task<int> GetIniInt(string iniFileName, string section, string key)
    {
        return Task.FromResult(0);
    }
}