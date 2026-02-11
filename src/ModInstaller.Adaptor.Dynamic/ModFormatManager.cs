using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FomodInstaller.Scripting;
using FomodInstaller.Scripting.XmlScript;
using Utils;

namespace FomodInstaller.ModInstaller
{
    [Serializable]
    public class UnsupportedException : Exception
    {
        public UnsupportedException()
        { }
    }

    public class ModFormatManager
    {

        #region Fields

        private static string FomodRoot = "fomod";
        private static ISet<string> RequiredExtensions = new HashSet<string> { ".gif", ".ico", ".jpeg", ".jpg", ".png", ".txt", ".xml", ".xsd" };
        #endregion

        #region Properties

        public IScriptTypeRegistry CurrentScriptTypeRegistry;

        #endregion

        #region Costructors

        public ModFormatManager()
        {
            // ??? Dummy path
        }

        #endregion

        public async Task<IList<string>> GetRequirements(IList<string> modFiles, bool includeAssets, IList<string> allowedTypes)
        {
            CurrentScriptTypeRegistry = new ScriptTypeRegistry();
#if USE_CSHARP_SCRIPT
            CurrentScriptTypeRegistry.RegisterType(new FomodInstaller.Scripting.CSharpScript.CSharpScriptType());
#endif
            CurrentScriptTypeRegistry.RegisterType(new XmlScriptType());
            // CurrentScriptTypeRegistry = await ScriptTypeRegistry.DiscoverScriptTypes(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            // TODO: I don't think there is a good way to determine which image files are referenced by the installer script without
            //   unpacking it first, right?
            IList<string> RequiredFiles = includeAssets
                ? modFiles.Where(path => RequiredExtensions.Contains(Path.GetExtension(path))).ToList()
                : new List<string>();

            return await Task.Run(() =>
            {
                bool HasFoundScriptType = false;
                foreach (IScriptType scriptType in CurrentScriptTypeRegistry.Types)
                {
                    if ((allowedTypes != null) && !allowedTypes.Contains(scriptType.TypeId))
                    {
                        continue;
                    }

                    if (scriptType.FileNames != null)
                    {
                        foreach (string scriptFile in scriptType.FileNames)
                        {
                            string omodMatch = modFiles.Where(x => x.Equals("script") || x.Equals("script.txt")).FirstOrDefault();
                            bool isOmod = false;
                            try
                            {
                                isOmod = (!string.IsNullOrEmpty(omodMatch) && scriptType.ValidateScript(scriptType.LoadScript(omodMatch, true)));
                            } catch (Exception) {
                                // don't care
                            }

                            if (isOmod)
                            {
                                HasFoundScriptType = true;
                                RequiredFiles.Add(omodMatch);
                            } else
                            {
                                string fomodMatch = modFiles.Where(x => scriptMatch(x, scriptFile)).FirstOrDefault();
                                if (!string.IsNullOrEmpty(fomodMatch))
                                {
                                    HasFoundScriptType = true;
                                    RequiredFiles.Add(fomodMatch);
                                }
                            }
                        }
                    }

                    if (HasFoundScriptType)
                        break;
                }

                if (!HasFoundScriptType)
                {
                    throw new UnsupportedException();
                }

                return RequiredFiles;
            });
        }

        private bool scriptMatch(string filePath, string scriptFile)
        {
            return Path.GetFileName(filePath).Contains(scriptFile, StringComparison.OrdinalIgnoreCase)
              && Path.GetFileName(Path.GetDirectoryName(filePath)).Contains(FomodRoot, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IScriptType> GetScriptType(IList<string> modFiles, string extractedFilePath = null)
        {
            CurrentScriptTypeRegistry = new ScriptTypeRegistry();
#if USE_CSHARP_SCRIPT
            CurrentScriptTypeRegistry.RegisterType(new FomodInstaller.Scripting.CSharpScript.CSharpScriptType());
#endif
            CurrentScriptTypeRegistry.RegisterType(new XmlScriptType());
            // CurrentScriptTypeRegistry = await ScriptTypeRegistry.DiscoverScriptTypes(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            IScriptType FoundScriptType = null;

            string omodMatch = modFiles.Where(x => x.Equals("script") || x.Equals("script.txt")).FirstOrDefault();

            await Task.Run(() =>
            {
                foreach (IScriptType scriptType in CurrentScriptTypeRegistry.Types)
                {
                    bool HasFoundScriptType = false;
                    if (scriptType.FileNames != null)
                    {
                        foreach (string scriptFile in scriptType.FileNames)
                        {
                            string scriptDataString = "";
                            if (!string.IsNullOrEmpty(omodMatch))
                            {
                                byte[] scriptData = FileSystem.ReadAllBytes(Path.Combine(extractedFilePath, omodMatch));
                                scriptDataString = TextUtil.ByteToStringWithoutBOM(scriptData);
                            }

                            if (!string.IsNullOrEmpty(omodMatch) && scriptType.ValidateScript(scriptType.LoadScript(scriptDataString, true)))
                            {
                                HasFoundScriptType = true;
                                FoundScriptType = scriptType;
                            } else
                            {
                                string fomodMatch = modFiles.Where(x => scriptMatch(x, scriptFile)).FirstOrDefault();
                                if (!string.IsNullOrEmpty(fomodMatch))
                                {
                                    HasFoundScriptType = true;
                                    FoundScriptType = scriptType;
                                }
                            }
                        }
                    }

                    if (HasFoundScriptType)
                        break;
                }
            });

            return FoundScriptType;
        }
    }
}
