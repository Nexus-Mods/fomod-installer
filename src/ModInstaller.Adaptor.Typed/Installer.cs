using FomodInstaller.Interface;
using FomodInstaller.ModInstaller;
using FomodInstaller.Scripting;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ModInstaller.Lite;

public static class Installer
{
    /// <summary>
    /// This will determine whether the program can handle the specific archive.
    /// </summary>
    /// <param name="modArchiveFileList">The list of files inside the mod archive.</param>
    public static SupportedResult TestSupported(
        List<string> modArchiveFileList,
        List<string> allowedTypes)
    {
        var test = true;
        var requiredFiles = new List<string>();

        if (modArchiveFileList == null || modArchiveFileList.Count == 0)
        {
            test = false;
        }
        else
        {
            try
            {
                requiredFiles = GetRequirements(modArchiveFileList, true, allowedTypes);
            }
            catch (UnsupportedException)
            {
                requiredFiles = new List<string>();
                // files without an installer script are still supported by this
                if (!allowedTypes.Contains("Basic"))
                {
                    test = false;
                }
            }

        }

        if (!test)
        {
            return SupportedResult.AsNotSupported;
        }

        return SupportedResult.AsSupported with
        {
            RequiredFiles = requiredFiles.ToList(),
        };
    }

    /// <summary>
    /// This will simulate the mod installation and decide installation choices and files final paths.
    /// </summary>
    /// <param name="modArchiveFileList">The list of files inside the mod archive.</param>
    /// <param name="stopPatterns">patterns matching files or directories that should be at the top of the directory structure.</param>
    /// <param name="scriptPath">The path to the uncompressed install script file, if any.</param>
    /// <param name="preset">preset of options to suggest or activate automatically. The structure of this object depends on the installer format</param>
    /// <param name="progressDelegate">A delegate to provide progress feedback.</param>
    /// <param name="coreDelegate">A delegate for all the interactions with the js core.</param>
    public static async Task<InstallResult> Install(
        List<string> modArchiveFileList,
        List<string> stopPatterns,
        string pluginPath,
        string scriptPath,
        JsonDocument? preset,
        bool validate,
        ProgressDelegate progressDelegate,
        CoreDelegates coreDelegate)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        var instructions = new List<Instruction>();
        string scriptFilePath = null;

        try
        {
            scriptFilePath = new List<string>(GetRequirements(modArchiveFileList, false, null)).FirstOrDefault();
        }
        catch (UnsupportedException)
        {
            // not an error, this can handle mods without an installer script (see BasicModInstall)
        }
        var scriptType = GetScriptType(modArchiveFileList, scriptPath);
        var modToInstall = new Mod(modArchiveFileList, stopPatterns, scriptFilePath, scriptPath, scriptType);
        await modToInstall.Initialize(validate);

        progressDelegate(50);

        if (modToInstall.HasInstallScript)
        {
            instructions = await ScriptedModInstall(modToInstall, preset, coreDelegate) ??
                           Instruction.InstallErrorList("warning", "Installer failed (it should have reported an error message)");
        }
        else
        {
            instructions = BasicModInstall(modArchiveFileList, stopPatterns);
        }

        // f***ing ugly hack, but this is in NMM so...
        if (pluginPath != null)
        {
            var pattern = pluginPath + Path.DirectorySeparatorChar;
            instructions = instructions.Select(instruction =>
            {
                var output = instruction;
                if (output.type == "copy" && output.destination.StartsWith(pattern, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    output.destination = output.destination.Substring(pattern.Length);
                }
                return output;
            }).ToList();
        }

        progressDelegate(100);

        return new InstallResult
        {
            Message = "Installation successful",
            Instructions = instructions,
        };
    }

    /// <summary>
    /// This function will return the list of files requirements to complete this mod's installation.
    /// <param name="modFiles">The list of files inside the mod archive.</param>
    /// <param name="includeAssets">If true, the result will also include all assets required by the
    ///   installer (i.e. screenshots). Otherwise the result should only be one file which is the
    ///   installer script</param>
    /// </summary>
    private static List<string> GetRequirements(IList<string> modFiles, bool includeAssets, IList<string> allowedTypes)
    {
        var formatManager = new ModFormatManager();

        return formatManager.GetRequirements(modFiles, includeAssets, allowedTypes);
    }

    /// <summary>
    /// This function will return the list of files requirements to complete this mod's installation.
    /// <param name="modFiles">The list of files inside the mod archive.</param>
    /// </summary>
    private static IScriptType GetScriptType(IList<string> modFiles, string extractedFilePath = null)
    {
        var formatManager = new ModFormatManager();

        return formatManager.GetScriptType(modFiles, extractedFilePath);
    }

    /// <summary>
    /// This will assign all files to the proper destination.
    /// </summary>
    /// <param name="fileList">The list of files inside the mod archive.</param>
    /// <param name="stopPatterns">patterns matching files or directories that should be at the top of the directory structure.</param>
    private static List<Instruction> BasicModInstall(
        List<string> fileList,
        List<string> stopPatterns)
    {
        var filesToInstall = new List<Instruction>();
        var prefix = ArchiveStructure.FindPathPrefix(fileList, stopPatterns);

        foreach (var ArchiveFile in fileList)
        {
            if (ArchiveFile.EndsWith($"{Path.DirectorySeparatorChar}"))
            {
                // don't include directories, only files
                continue;
            }

            var destination = ArchiveFile.StartsWith(prefix) ? ArchiveFile.Substring(prefix.Length) : ArchiveFile;
            filesToInstall.Add(Instruction.CreateCopy(ArchiveFile, destination, 0));
            // Progress should increase.	
        }

        return filesToInstall;
    }

    /// <summary>
    /// This will assign all files to the proper destination.
    /// </summary>
    /// <param name="modArchive">The list of files inside the mod archive.</param>
    /// <param name="coreDelegate">A delegate for all the interactions with the js core.</param>
    private static async Task<List<Instruction>> ScriptedModInstall(
        Mod modArchive,
        JsonDocument? preset,
        CoreDelegates coreDelegate)
    {
        var presetExpando = preset is not null ? JsonUtils.ParseJsonArray(preset) : null;

        var sexScript = modArchive.InstallScript.Type.CreateExecutor(modArchive, coreDelegate);
        return (await sexScript.Execute(modArchive.InstallScript, modArchive.TempPath, presetExpando)).ToList();
    }
}

file static class JsonUtils
{
    public static IEnumerable<Dictionary<string, object>> ParseJsonArray(JsonDocument document)
    {
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("JSON root must be an array.");

        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("Each array element must be an object.");

            yield return ReadObject(element);
        }
    }

    private static Dictionary<string, object> ReadObject(JsonElement element)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = ConvertValue(property.Value);
        }

        return dict;
    }

    private static object? ConvertValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt64(out var l) ? l :
                value.TryGetDouble(out var d) ? d : (object?) null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => ReadObject(value),
            JsonValueKind.Array => ReadArray(value),
            _ => null
        };
    }

    private static List<object?> ReadArray(JsonElement array)
    {
        var list = new List<object?>();
        foreach (var item in array.EnumerateArray())
        {
            list.Add(ConvertValue(item));
        }
        return list;
    }
}