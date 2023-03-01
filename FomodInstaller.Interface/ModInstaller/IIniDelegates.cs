namespace FomodInstaller.Interface
{
    public interface IIniDelegates
    {
        Task<int> GetIniInt(string iniFileName, string iniSection, string iniKey);
        Task<string> GetIniString(string iniFileName, string iniSection, string iniKey);
    }
}