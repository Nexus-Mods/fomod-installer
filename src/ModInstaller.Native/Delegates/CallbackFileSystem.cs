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
#if DEBUG
        using var logger = LogMethod(filePath.ToFormattable(), offset, length);
#else
        using var logger = LogMethod();
#endif

        fixed (char* pFilePath = filePath)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_readFileContent(_pOwner, (param_string*) pFilePath, offset, length), true);
                logger.LogResult(result);
                using var data = result.ValueAsData();
                return data.ToSpan().ToArray();
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }

    public unsafe string[]? ReadDirectoryFileList(string directoryPath, string pattern, SearchOption searchOption)
    {
#if DEBUG
        using var logger = LogMethod(directoryPath.ToFormattable(), pattern.ToFormattable(), searchOption);
#else
        using var logger = LogMethod();
#endif

        fixed (char* pDirectoryPath = directoryPath)
        fixed (char* pPattern = pattern)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_readDirectoryFileList(_pOwner, (param_string*) pDirectoryPath, (param_string*) pPattern, (param_int) (int) searchOption), true);
                logger.LogResult(result);
                return result.ValueAsJson(Bindings.CustomSourceGenerationContext.StringArray);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }

    public unsafe string[]? ReadDirectoryList(string directoryPath)
    {
#if DEBUG
        using var logger = LogMethod(directoryPath.ToFormattable());
#else
        using var logger = LogMethod();
#endif

        fixed (char* pDirectoryPath = directoryPath)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_readDirectoryList(_pOwner, (param_string*) pDirectoryPath), true);
                logger.LogResult(result);
                return result.ValueAsJson(Bindings.CustomSourceGenerationContext.StringArray);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}