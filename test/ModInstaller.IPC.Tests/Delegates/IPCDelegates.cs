using TestData;
using Utils;

namespace ModInstaller.IPC.Tests.Delegates;

/// <summary>
/// Wraps all delegate implementations for IPC testing
/// </summary>
internal class IPCDelegates
{
    private readonly IFileSystem _fileSystem;
    private readonly InstallData _testData;
    private readonly IEnumerable<SelectedOption>? _dialogChoices;

    public IPCDelegates(IFileSystem fileSystem, InstallData testData, IEnumerable<SelectedOption>? dialogChoices = null)
    {
        _fileSystem = fileSystem;
        _testData = testData;
        _dialogChoices = dialogChoices;
    }

    // Context delegates
    public Task<string> GetAppVersion() => Task.FromResult(_testData.AppVersion);
    public Task<string> GetCurrentGameVersion() => Task.FromResult(_testData.GameVersion);
    public Task<string> GetExtenderVersion(string extender) => Task.FromResult(_testData.ExtenderVersion);
    public Task<bool> IsExtenderPresent() => Task.FromResult(false);

    public Task<bool> CheckIfFileExists(string fileName)
    {
        var content = _fileSystem.ReadFileContent(fileName, 0, -1);
        return Task.FromResult(content != null);
    }

    public Task<byte[]> GetExistingDataFile(string dataFile)
    {
        var content = _fileSystem.ReadFileContent(dataFile, 0, -1);
        return Task.FromResult(content ?? Array.Empty<byte>());
    }

    public Task<string[]> GetExistingDataFileList(string folderPath, string searchFilter, bool isRecursive)
    {
        var searchOption = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = _fileSystem.ReadDirectoryFileList(folderPath, searchFilter, searchOption) ?? Array.Empty<string>();
        return Task.FromResult(files);
    }

    // Plugin delegates
    private string[]? _activeCache;
    private string[]? _presentCache;

    public Task<string[]> GetAllPlugins(bool activeOnly)
    {
        return Task.FromResult(_testData.InstalledPlugins.ToArray());
    }

    public async Task<bool> IsPluginActive(string pluginName)
    {
        if (_activeCache == null)
        {
            _activeCache = await GetAllPlugins(true);
        }
        return _activeCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default;
    }

    public async Task<bool> IsPluginPresent(string pluginName)
    {
        if (_presentCache == null)
        {
            _presentCache = await GetAllPlugins(false);
        }
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

    // UI delegates - for now we'll implement a simple auto-continue pattern
    // In the future, this could be enhanced to handle DialogChoices like DeterministicUIContext
    public Task StartDialog(string moduleName, object image, Action<int, int, int[]> select, Action<bool, int> cont, Action cancel)
    {
        // Auto-continue for unattended mode
        return Task.CompletedTask;
    }

    public Task EndDialog()
    {
        return Task.CompletedTask;
    }

    public Task UpdateState(object[] installSteps, int currentStep)
    {
        // For now, just auto-continue
        // TODO: Implement DialogChoices handling like DeterministicUIContext
        return Task.CompletedTask;
    }

    public Task ReportError(string title, string message, string details)
    {
        throw new Exception($"{title}: {message}\n{details}");
    }
}
