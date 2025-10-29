using System;
using System.Linq;
using System.Threading.Tasks;

namespace FomodInstaller.Interface;

public class DynamicContextDelegates : ContextDelegates
{
    private Func<object, Task<object>> mGetAppVersion;
    private Func<object, Task<object>> mGetCurrentGameVersion;
    private Func<object, Task<object>> mGetExtenderVersion;
    private Func<object, Task<object>> mIsExtenderPresent;
    private Func<object, Task<object>> mCheckIfFileExists;
    private Func<object, Task<object>> mGetExistingDataFile;
    private Func<object[], Task<object>> mGetExistingDataFileList;

    public DynamicContextDelegates(dynamic source)
    {
        mGetAppVersion = source.getAppVersion;
        mGetCurrentGameVersion = source.getCurrentGameVersion;
        mCheckIfFileExists = source.checkIfFileExists;
        mGetExtenderVersion = source.getExtenderVersion;
        mIsExtenderPresent = source.isExtenderPresent;
        mGetExistingDataFile = source.getExistingDataFile;
        mGetExistingDataFileList = source.getExistingDataFileList;
    }

    public override async Task<string> GetAppVersion()
    {
        object res = await Shared.TimeoutRetry(() => mGetAppVersion(null));
        return (string)res;
    }

    public override async Task<string> GetCurrentGameVersion()
    {
        object res = await Shared.TimeoutRetry(() => mGetCurrentGameVersion(null));
        return (string)res;
    }

    public override async Task<string> GetExtenderVersion(string extender)
    {
        object res = await Shared.TimeoutRetry(() => mGetExtenderVersion(extender));
        return (string)res;
    }

    public override async Task<bool> IsExtenderPresent()
    {
        object res = await Shared.TimeoutRetry(() => mIsExtenderPresent(null));
        return (bool)res;
    }

    public override async Task<bool> CheckIfFileExists(string fileName)
    {
        object res = await Shared.TimeoutRetry(() => mCheckIfFileExists(fileName));
        return (bool)res;
    }

    public override async Task<byte[]> GetExistingDataFile(string dataFile)
    {
        object res = await Shared.TimeoutRetry(() => mGetExistingDataFile(dataFile));
        return (byte[])res;
    }

    public override async Task<string[]> GetExistingDataFileList(string folderPath, string searchFilter, bool isRecursive)
    {
        object[] Params = new object[] { folderPath, searchFilter, isRecursive };
        object res = await Shared.TimeoutRetry(() => mGetExistingDataFileList(Params));
        return ((object[])res).Select(iter => (string)iter).ToArray();
    }
}