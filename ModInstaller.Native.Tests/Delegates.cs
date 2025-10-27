using System.Runtime.InteropServices;
using BUTR.NativeAOT.Shared;

namespace ModInstaller.Native.Tests;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate return_value_json* PluginsGetAllDelegate(param_ptr* handler, param_bool includeDisabled);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate return_value_void* ContextGetAppVersionDelegate(param_ptr* handler, param_ptr* p_context, delegate* unmanaged[Cdecl] <param_ptr*, return_value_string*, void> p_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate return_value_void* ContextGetCurrentGameVersionDelegate(param_ptr* handler, param_ptr* p_context, delegate* unmanaged[Cdecl] <param_ptr*, return_value_string*, void> p_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate return_value_void* ContextGetExtenderVersionDelegate(param_ptr* handler, param_string* p_extender_name, param_ptr* p_context, delegate* unmanaged[Cdecl] <param_ptr*, return_value_string*, void> p_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate return_value_void* UIStartDialogDelegate(param_ptr* handler, param_string* p_module_name, param_json* p_image, param_ptr* p_context, delegate* unmanaged[Cdecl] <param_ptr*, param_int, param_int, param_json*, return_value_void*, void> p_select_callback, delegate* unmanaged[Cdecl] <param_ptr*, param_bool, param_int, return_value_void*, void> p_cont_callback, delegate* unmanaged[Cdecl] <param_ptr*, return_value_void*, void> p_cancel_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate return_value_void* UIEndDialogDelegate(param_ptr* handler, param_ptr* p_context, delegate* unmanaged[Cdecl] <param_ptr*, return_value_void*, void> p_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate return_value_void* UIUpdateStateDelegate(param_ptr* handler, param_json* p_install_steps, param_int current_step, param_ptr* p_context, delegate* unmanaged[Cdecl] <param_ptr*, return_value_void*, void> p_callback);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate return_value_data* ReadFileContentDelegate(param_ptr* handler, param_string* pFilePath, param_int offset, param_int length);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate return_value_json* ReadDirectoryFileListDelegate(param_ptr* handler, param_string* pDirectoryPath, param_string* pPattern, param_int searchOption);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate return_value_json* ReadDirectoryListDelegate(param_ptr* handler, param_string* pDirectoryPath);
