using FomodInstaller.Interface;

namespace ModInstaller.Adaptor.Typed.Tests.Delegates;

internal class CallbackContextDelegates : ContextDelegates
{
    private readonly Func<Task<string>> _getAppVersionFunc;
    private readonly Func<Task<string>> _getCurrentGameVersionFunc;
    private readonly Func<string, Task<string>> _getExtenderVersionFunc;
    private readonly Func<Task<bool>> _isExtenderPresentFunc;
    private readonly Func<string, Task<bool>> _checkIfFileExistsFunc;
    private readonly Func<string, Task<byte[]>> _getExistingDataFileFunc;
    private readonly Func<string, string, bool, Task<string[]>> _getExistingDataFileListFunc;
    
    public CallbackContextDelegates(
        Func<Task<string>> getAppVersionFunc,
        Func<Task<string>> getCurrentGameVersionFunc,
        Func<string, Task<string>> getExtenderVersionFunc,
        Func<Task<bool>> isExtenderPresentFunc,
        Func<string, Task<bool>> checkIfFileExistsFunc,
        Func<string, Task<byte[]>> getExistingDataFileFunc,
        Func<string, string, bool, Task<string[]>> getExistingDataFileListFunc)
    {
        _getAppVersionFunc = getAppVersionFunc;
        _getCurrentGameVersionFunc = getCurrentGameVersionFunc;
        _getExtenderVersionFunc = getExtenderVersionFunc;
        _isExtenderPresentFunc = isExtenderPresentFunc;
        _checkIfFileExistsFunc = checkIfFileExistsFunc;
        _getExistingDataFileFunc = getExistingDataFileFunc;
        _getExistingDataFileListFunc = getExistingDataFileListFunc;
    }
    
    public override async Task<string> GetAppVersion() => await _getAppVersionFunc();
    public override async Task<string> GetCurrentGameVersion() => await _getCurrentGameVersionFunc();
    public override async Task<string> GetExtenderVersion(string extender) => await _getExtenderVersionFunc(extender);
    public override async Task<bool> IsExtenderPresent() => await _isExtenderPresentFunc();
    public override async Task<bool> CheckIfFileExists(string fileName) => await _checkIfFileExistsFunc(fileName);
    public override async Task<byte[]> GetExistingDataFile(string dataFile) => await _getExistingDataFileFunc(dataFile);
    public override async Task<string[]> GetExistingDataFileList(string folderPath, string searchFilter, bool isRecursive) => await _getExistingDataFileListFunc(folderPath, searchFilter, isRecursive);
}