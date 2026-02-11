using FomodInstaller.Scripting;
using FomodInstaller.Scripting.XmlScript;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Utils;

namespace ModInstaller.Lite;

public class UnsupportedException : Exception;

public class ModFormatManager
{
    #region Fields

    private static string FomodRoot = "fomod";
    private static ISet<string> RequiredExtensions = new HashSet<string> { ".gif", ".ico", ".jpeg", ".jpg", ".png", ".txt", ".xml", ".xsd" };
    #endregion

    #region Properties

    public IScriptTypeRegistry CurrentScriptTypeRegistry;

    #endregion

    public List<string> GetRequirements(IList<string> modFiles, bool includeAssets, IList<string> allowedTypes)
    {
        CurrentScriptTypeRegistry = new ScriptTypeRegistry();
        CurrentScriptTypeRegistry.RegisterType(new XmlScriptType());
        // CurrentScriptTypeRegistry = await ScriptTypeRegistry.DiscoverScriptTypes(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        // TODO: I don't think there is a good way to determine which image files are referenced by the installer script without
        //   unpacking it first, right?
        var RequiredFiles = includeAssets
            ? modFiles.Where(path => RequiredExtensions.Contains(Path.GetExtension(path))).ToList()
            : new List<string>();

        var HasFoundScriptType = false;
        foreach (var scriptType in CurrentScriptTypeRegistry.Types)
        {
            if ((allowedTypes != null) && !allowedTypes.Contains(scriptType.TypeId))
            {
                continue;
            }

            if (scriptType.FileNames != null)
            {
                foreach (var scriptFile in scriptType.FileNames)
                {
                    var fomodMatch = modFiles.FirstOrDefault(x => ScriptMatch(x, scriptFile));
                    if (!string.IsNullOrEmpty(fomodMatch))
                    {
                        HasFoundScriptType = true;
                        RequiredFiles.Add(fomodMatch);
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
    }

    private static bool ScriptMatch(string filePath, string scriptFile)
    {
        return Path.GetFileName(filePath).Contains(scriptFile, StringComparison.OrdinalIgnoreCase)
               && Path.GetFileName(Path.GetDirectoryName(filePath))?.Contains(FomodRoot, StringComparison.OrdinalIgnoreCase) == true;
    }

    public IScriptType GetScriptType(IList<string> modFiles, string extractedFilePath = null)
    {
        CurrentScriptTypeRegistry = new ScriptTypeRegistry();
        CurrentScriptTypeRegistry.RegisterType(new XmlScriptType());
        // CurrentScriptTypeRegistry = await ScriptTypeRegistry.DiscoverScriptTypes(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        IScriptType FoundScriptType = null;

        var omodMatch = modFiles.FirstOrDefault(x => x.Equals("script") || x.Equals("script.txt"));

        foreach (var scriptType in CurrentScriptTypeRegistry.Types)
        {
            var HasFoundScriptType = false;
            if (scriptType.FileNames != null)
            {
                foreach (var scriptFile in scriptType.FileNames)
                {
                    var scriptDataString = "";
                    if (!string.IsNullOrEmpty(omodMatch))
                    {
                        var scriptData = FileSystem.ReadAllBytes(Path.Combine(extractedFilePath, omodMatch));
                        scriptDataString = TextUtil.ByteToStringWithoutBOM(scriptData);
                    }

                    if (!string.IsNullOrEmpty(omodMatch) && scriptType.ValidateScript(scriptType.LoadScript(scriptDataString, true)))
                    {
                        HasFoundScriptType = true;
                        FoundScriptType = scriptType;
                    }
                    else
                    {
                        var fomodMatch = modFiles.FirstOrDefault(x => ScriptMatch(x, scriptFile));
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

        return FoundScriptType;
    }
}