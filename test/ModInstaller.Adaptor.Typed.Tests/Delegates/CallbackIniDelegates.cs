using FomodInstaller.Interface;

namespace ModInstaller.Adaptor.Typed.Tests.Delegates;

internal class CallbackIniDelegates : IniDelegates
{
    private readonly Func<string, string, string, Task<string>> _getIniStringFunc;
    private readonly Func<string, string, string, Task<int>> _getIniIntFunc;

    public CallbackIniDelegates(
        Func<string, string, string, Task<string>> getIniStringFunc,
        Func<string, string, string, Task<int>> getIniIntFunc)
    {
        _getIniStringFunc = getIniStringFunc;
        _getIniIntFunc = getIniIntFunc;
    }

    public override async Task<string> GetIniString(string iniFileName, string iniSection, string iniKey) => await _getIniStringFunc(iniFileName, iniSection, iniKey);
    public override async Task<int> GetIniInt(string iniFileName, string iniSection, string iniKey) => await _getIniIntFunc(iniFileName, iniSection, iniKey);
}