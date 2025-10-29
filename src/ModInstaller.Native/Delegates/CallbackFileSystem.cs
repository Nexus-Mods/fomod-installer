using BUTR.NativeAOT.Shared;

using System;
using System.IO;

using Utils;

namespace ModInstaller.Native.Adapters;

internal class CallbackFileSystem : IFileSystem
{
    private readonly unsafe param_ptr* _pOwner;
    private readonly N_ReadFileContentDelegate _readFileContent;
    private readonly N_ReadDirectoryFileList _readDirectoryFileList;
    private readonly N_ReadDirectoryList _readDirectoryList;

    public unsafe CallbackFileSystem(param_ptr* pOwner,
        N_ReadFileContentDelegate readFileContent,
        N_ReadDirectoryFileList readDirectoryFileList,
        N_ReadDirectoryList readDirectoryList)
    {
        _pOwner = pOwner;
        _readFileContent = readFileContent;
        _readDirectoryFileList = readDirectoryFileList;
        _readDirectoryList = readDirectoryList;
    }

    public unsafe byte[]? ReadFileContent(string filePath, int offset, int length)
    {
        Logger.LogInput(offset, length);

        fixed (char* pFilePath = filePath)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_readFileContent(_pOwner, (param_string*) pFilePath, offset, length), true);
                using var data = result.ValueAsData();
                return data.ToSpan().ToArray();

                Logger.LogOutput();
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return null;
            }
        }
    }

    public unsafe string[]? ReadDirectoryFileList(string directoryPath, string pattern, SearchOption searchOption)
    {
        //Logger.LogInput(directoryPath, pattern, searchOption);

        fixed (char* pDirectoryPath = directoryPath)
        fixed (char* pPattern = pattern)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_readDirectoryFileList(_pOwner, (param_string*) pDirectoryPath, (param_string*) pPattern, (param_int) (int) searchOption), true);
                return result.ValueAsJson(Bindings.CustomSourceGenerationContext.StringArray);

                Logger.LogOutput();
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return null;
            }
        }
    }

    public unsafe string[]? ReadDirectoryList(string directoryPath)
    {
        Logger.LogInput(directoryPath);

        fixed (char* pDirectoryPath = directoryPath)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_readDirectoryList(_pOwner, (param_string*) pDirectoryPath), true);
                return result.ValueAsJson(Bindings.CustomSourceGenerationContext.StringArray);

                Logger.LogOutput();
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return null;
            }
        }
    }
}