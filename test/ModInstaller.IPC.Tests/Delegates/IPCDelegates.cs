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
    public Task<string> GetAppVersion() => Task.FromResult(_testData.AppVersion);
    public Task<string> GetCurrentGameVersion() => Task.FromResult(_testData.GameVersion);
    public Task<string> GetExtenderVersion(string extender) => Task.FromResult(_testData.ExtenderVersion);
    public Task<bool> IsExtenderPresent() => Task.FromResult(false);

    public Task<bool> CheckIfFileExists(string fileName) => Task.FromResult(File.Exists(fileName));
    public Task<byte[]> GetExistingDataFile(string dataFile) => Task.FromResult(File.ReadAllBytes(dataFile));
    public Task<string[]> GetExistingDataFileList(string folderPath, string searchFilter, bool isRecursive) => Task.FromResult(Directory.GetDirectories(folderPath, searchFilter, isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));

    // Plugin delegates
    private string[]? _activeCache;
    private string[]? _presentCache;

    public Task<string[]> GetAllPlugins(bool activeOnly) => Task.FromResult(_testData.InstalledPlugins.ToArray());

    public async Task<bool> IsPluginActive(string pluginName)
    {
        _activeCache ??= await GetAllPlugins(true);
        return _activeCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default;
    }

    public async Task<bool> IsPluginPresent(string pluginName)
    {
        _presentCache ??= await GetAllPlugins(false);
        return _presentCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default;
    }

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