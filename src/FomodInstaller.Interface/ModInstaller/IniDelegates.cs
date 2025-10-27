using System.Threading.Tasks;

namespace FomodInstaller.Interface
{
    public abstract class IniDelegates
    {
        /// <summary>
        /// get ini string
        /// C# only
        /// </summary>
        public abstract Task<string> GetIniString(string iniFileName, string iniSection, string iniKey);
      
        /// <summary>
        /// get ini int
        /// C# only
        /// </summary>
        public abstract Task<int> GetIniInt(string iniFileName, string iniSection, string iniKey);
    }
}