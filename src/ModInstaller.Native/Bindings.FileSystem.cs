using BUTR.NativeAOT.Shared;

using ModInstaller.Native.Adapters;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Utils;

namespace ModInstaller.Native;

public static unsafe partial class Bindings
{
    [UnmanagedCallersOnly(EntryPoint = "set_default_file_system_callbacks", CallConvs = [typeof(CallConvCdecl)])]
    public static int SetDefaultFileSystemCallbacks()
    {
        Logger.LogInput();
        try
        {
            FileSystem.Instance = new DefaultFileSystem();

            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            Logger.LogException(e);
            return -1;
        }
        finally
        {
            Logger.LogOutput();
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "set_file_system_callbacks", CallConvs = [typeof(CallConvCdecl)])]
    public static int SetFileSystemCallbacks(param_ptr* p_owner,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_int, param_int, return_value_data*> p_read_file_content,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_string*, param_int, return_value_json*> p_read_directory_file_list,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, return_value_json*> p_read_directory_list
    )
    {
        Logger.LogInput();
        try
        {
            var fileSystemDelegate = new CallbackFileSystem(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_ReadFileContentDelegate>(new IntPtr(p_read_file_content)),
                Marshal.GetDelegateForFunctionPointer<N_ReadDirectoryFileList>(new IntPtr(p_read_directory_file_list)),
                Marshal.GetDelegateForFunctionPointer<N_ReadDirectoryList>(new IntPtr(p_read_directory_list))
            );

            FileSystem.Instance = fileSystemDelegate;

            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            Logger.LogException(e);
            return -1;
        }
        finally
        {
            Logger.LogOutput();
        }
    }
}