using System.Runtime.InteropServices;

using BUTR.NativeAOT.Shared;

namespace ModInstaller.Native;

// PluginDelegates
// string[]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_Plugins_GetAll(param_ptr* p_owner,
    param_bool active_only,
    param_ptr* p_callback_handler,
    delegate* unmanaged[Cdecl]<param_ptr*, return_value_json*, void> p_callback);

// IniDelegates
// Async?
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_Ini_GetIniString(param_ptr* p_owner,
    param_string* p_ini_filename,
    param_string* p_ini_section,
    param_string* p_ini_key,
    param_ptr* p_callback_handler,
    delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void> p_callback);

// Async?
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_Ini_GetIniInt(param_ptr* p_owner,
    param_string* p_ini_filename,
    param_string* p_ini_section,
    param_string* p_ini_key,
    param_ptr* p_callback_handler,
    delegate* unmanaged[Cdecl]<param_ptr*, return_value_int32*, void> p_callback);

// UIDelegates
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_UI_StartDialog(param_ptr* p_owner,
    param_string* p_module_name,
    param_json* p_image,
    param_ptr* p_callback_handler,
    delegate* unmanaged[Cdecl]<param_ptr*, param_int, param_int, param_json*, return_value_void*, void> p_select_callback,
    delegate* unmanaged[Cdecl]<param_ptr*, param_bool, param_int, return_value_void*, void> p_cont_callback,
    delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*, void> p_cancel_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_UI_EndDialog(param_ptr* p_owner);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_UI_UpdateState(param_ptr* p_owner,
    param_json* p_install_steps,
    param_int current_step);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_UI_ReportError(param_ptr* p_owner,
    param_string* p_title,
    param_string* p_message,
    param_string* p_details,
    param_ptr* p_callback_handler,
    delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*, void> p_callback);

// ContextDelegates
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_Context_GetAppVersion(param_ptr* p_owner,
    param_ptr* p_callback_handler,
    delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void> p_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_Context_GetCurrentGameVersion(param_ptr* p_owner,
    param_ptr* p_callback_handler,
    delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void> p_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_Context_GetExtenderVersion(param_ptr* p_owner,
    param_string* p_extender,
    param_ptr* p_callback_handler,
    delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void> p_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_Context_IsExtenderPresent(param_ptr* p_owner,
    param_ptr* p_callback_handler,
    delegate* unmanaged[Cdecl]<param_ptr*, return_value_bool*, void> p_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_Context_CheckIfFileExists(param_ptr* p_owner,
    param_string* p_filename,
    param_ptr* p_callback_handler,
    delegate* unmanaged[Cdecl]<param_ptr*, return_value_bool*, void> p_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_Context_GetExistingDataFile(param_ptr* p_owner,
    param_string* p_datafile,
    param_ptr* p_callback_handler,
    delegate* unmanaged[Cdecl]<param_ptr*, return_value_data*, void> p_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_void* N_Context_GetExistingDataFileList(param_ptr* p_owner,
    param_string* p_folder_path,
    param_string* p_search_filter,
    param_bool is_recursive,
    param_ptr* p_callback_handler,
    delegate* unmanaged[Cdecl]<param_ptr*, return_value_json*, void> p_callback);
    
// FileSystem Delegates
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_data* N_ReadFileContentDelegate(param_ptr* p_owner,
    param_string* p_file_path,
    param_int offset,
    param_int length);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_json* N_ReadDirectoryFileList(param_ptr* p_owner,
    param_string* p_directory_path,
    param_string* p_pattern,
    param_int search_type);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate return_value_json* N_ReadDirectoryList(param_ptr* p_owner,
    param_string* p_directory_path);
