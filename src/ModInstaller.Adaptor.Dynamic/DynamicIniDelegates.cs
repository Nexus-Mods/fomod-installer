using System;
using System.Threading.Tasks;

namespace FomodInstaller.Interface;

public class DynamicIniDelegates : IniDelegates
{
    private Func<object[], Task<object>> mGetIniString;
    private Func<object[], Task<object>> mGetIniInt;

    public DynamicIniDelegates(dynamic source)
    {
        mGetIniString = source.getIniString;
        mGetIniInt = source.getIniInt;
    }

    public override async Task<string> GetIniString(string iniFileName, string iniSection, string iniKey)
    {
        string[] Params = new string[] { iniFileName, iniSection, iniKey };
        object res = await mGetIniString(Params).WaitAsync(TimeSpan.FromMilliseconds(Shared.TIMEOUT_MS));
        if (res != null)
        {
            return res.ToString();
        }
        else
            return string.Empty;
    }

    public override async Task<int> GetIniInt(string iniFileName, string iniSection, string iniKey)
    {
        string[] Params = new string[] { iniFileName, iniSection, iniKey };
        object res = await mGetIniInt(Params).WaitAsync(TimeSpan.FromMilliseconds(Shared.TIMEOUT_MS));
        if (res != null)
        {
            return (int)res;
        }
        else
            return -1;
    }
}