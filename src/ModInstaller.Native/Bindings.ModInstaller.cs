using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BUTR.NativeAOT.Shared;
using FomodInstaller.ModInstaller;
using ModInstaller.Lite;
using ModInstaller.Native.Adapters;
using Utils;

namespace ModInstaller.Native;

public static unsafe partial class Bindings
{
    // Simplified version for the XML installer
    [UnmanagedCallersOnly(EntryPoint = "create_handler", CallConvs = [typeof(CallConvCdecl)])]
    public static return_value_ptr* CreateHandler(param_ptr* p_owner,
        // PluginDelegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_bool, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_json*, void>, return_value_void*> p_plugins_get_all,
        // ContextDelegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void>, return_value_void*> p_context_get_app_version,
        delegate* unmanaged[Cdecl]<param_ptr*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void>, return_value_void*> p_context_get_current_game_version,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void>, return_value_void*> p_context_get_extender_version,
        // UI Delegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_json*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, param_int, param_int, param_json*, return_value_void*, void>, delegate* unmanaged[Cdecl]<param_ptr*, param_bool, param_int, return_value_void*, void>, delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*, void>, return_value_void*> p_ui_start_dialog,
        delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*> p_ui_end_dialog,
        delegate* unmanaged[Cdecl]<param_ptr*, param_json*, param_int, return_value_void*> p_ui_update_state
        )
    {
        Logger.LogInput();
        try
        {
            var pluginsDelegate = new CallbackPluginDelegates(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_Plugins_GetAll>(new IntPtr(p_plugins_get_all))
            );
            
            var iniDelegate = new CallbackIniDelegates(p_owner,
                null!,
                null!
            );

            var contextDelegate = new CallbackContextDelegates(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_Context_GetAppVersion>(new IntPtr(p_context_get_app_version)),
                Marshal.GetDelegateForFunctionPointer<N_Context_GetCurrentGameVersion>(new IntPtr(p_context_get_current_game_version)),
                Marshal.GetDelegateForFunctionPointer<N_Context_GetExtenderVersion>(new IntPtr(p_context_get_extender_version)),
                null!,
                null!,
                null!,
                null!
            );
            
            var uiDelegate = new CallbackUIDelegates(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_UI_StartDialog>(new IntPtr(p_ui_start_dialog)),
                Marshal.GetDelegateForFunctionPointer<N_UI_EndDialog>(new IntPtr(p_ui_end_dialog)),
                Marshal.GetDelegateForFunctionPointer<N_UI_UpdateState>(new IntPtr(p_ui_update_state)),
                null!
            );
            
            var coreDelegates = new NativeCoreDelegatesHandler(p_owner, pluginsDelegate, contextDelegate, iniDelegate, uiDelegate);
            
            Logger.LogOutput();
            return return_value_ptr.AsValue(coreDelegates.HandlePtr, false);
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return return_value_ptr.AsException(e, false);
        }
    }
    
    // Simplified version for the XML installer
    [UnmanagedCallersOnly(EntryPoint = "create_handler_with_fs", CallConvs = [typeof(CallConvCdecl)])]
    public static return_value_ptr* CreateHandlerWithFS(param_ptr* p_owner,
        // PluginDelegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_bool, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_json*, void>, return_value_void*> p_plugins_get_all,
        // ContextDelegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void>, return_value_void*> p_context_get_app_version,
        delegate* unmanaged[Cdecl]<param_ptr*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void>, return_value_void*> p_context_get_current_game_version,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void>, return_value_void*> p_context_get_extender_version,
        // UI Delegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_json*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, param_int, param_int, param_json*, return_value_void*, void>, delegate* unmanaged[Cdecl]<param_ptr*, param_bool, param_int, return_value_void*, void>, delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*, void>, return_value_void*> p_ui_start_dialog,
        delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*> p_ui_end_dialog,
        delegate* unmanaged[Cdecl]<param_ptr*, param_json*, param_int, return_value_void*> p_ui_update_state,
        // FileSystem Delegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_int, param_int, return_value_data*> p_read_file_content,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_string*, param_int, return_value_json*> p_read_directory_file_list,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, return_value_json*> p_read_directory_list
        )
    {
        Logger.LogInput();
        try
        {
            var pluginsDelegate = new CallbackPluginDelegates(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_Plugins_GetAll>(new IntPtr(p_plugins_get_all))
            );
            
            var iniDelegate = new CallbackIniDelegates(p_owner,
                null!,
                null!
            );

            var contextDelegate = new CallbackContextDelegates(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_Context_GetAppVersion>(new IntPtr(p_context_get_app_version)),
                Marshal.GetDelegateForFunctionPointer<N_Context_GetCurrentGameVersion>(new IntPtr(p_context_get_current_game_version)),
                Marshal.GetDelegateForFunctionPointer<N_Context_GetExtenderVersion>(new IntPtr(p_context_get_extender_version)),
                null!,
                null!,
                null!,
                null!
            );
            
            var uiDelegate = new CallbackUIDelegates(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_UI_StartDialog>(new IntPtr(p_ui_start_dialog)),
                Marshal.GetDelegateForFunctionPointer<N_UI_EndDialog>(new IntPtr(p_ui_end_dialog)),
                Marshal.GetDelegateForFunctionPointer<N_UI_UpdateState>(new IntPtr(p_ui_update_state)),
                null!
            );
            
            var coreDelegates = new NativeCoreDelegatesHandler(p_owner, pluginsDelegate, contextDelegate, iniDelegate, uiDelegate);

            var fileSystemDelegate = new CallbackFileSystem(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_ReadFileContentDelegate>(new IntPtr(p_read_file_content)),
                Marshal.GetDelegateForFunctionPointer<N_ReadDirectoryFileList>(new IntPtr(p_read_directory_file_list)),
                Marshal.GetDelegateForFunctionPointer<N_ReadDirectoryList>(new IntPtr(p_read_directory_list))
            );

            FileSystem.Instance = fileSystemDelegate;
            
            Logger.LogOutput();
            return return_value_ptr.AsValue(coreDelegates.HandlePtr, false);
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return return_value_ptr.AsException(e, false);
        }
    }
    
    /*
    [UnmanagedCallersOnly(EntryPoint = "ve_create_handler", CallConvs = [typeof(CallConvCdecl)])]
    public static return_value_ptr* CreateHandler(param_ptr* p_owner,
        // PluginDelegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_bool, param_json*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*, void>, return_value_void*> p_plugins_get_all,
        // IniDelegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_string*, param_string*, param_uint, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*, void>, return_value_void*> p_ini_get_ini_string,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_string*, param_string*, param_json*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void>, return_value_void*> p_ini_fet_ini_int,
        // ContextDelegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void>, return_value_void*> p_context_get_app_version,
        delegate* unmanaged[Cdecl]<param_ptr*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void>, return_value_void*> p_context_get_current_game_version,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_string*, void>, return_value_void*> p_context_get_extender_version,
        delegate* unmanaged[Cdecl]<param_ptr*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_bool*, void>, return_value_void*> p_context_is_extender_present,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_bool*, void>, return_value_void*> p_context_check_if_file_exists,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_data*, void>, return_value_void*> p_context_get_existing_data_file,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_string*, param_bool, param_ptr*, delegate* unmanaged[Cdecl]<param_ptr*, return_value_json*, void>, return_value_void*> p_context_get_existing_data_file_list,
        // UI Delegates
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_json, delegate* unmanaged[Cdecl]<param_ptr*, param_int, param_int, param_json*, return_value_void*, void>, delegate* unmanaged[Cdecl]<param_ptr*, param_bool, param_int, return_value_void*, void>, delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*, void>, return_value_void*> p_ui_start_dialog,
        delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*, void> p_ui_end_dialog,
        delegate* unmanaged[Cdecl]<param_ptr*, param_json*, param_int, return_value_void*, void> p_ui_update_state,
        delegate* unmanaged[Cdecl]<param_ptr*, param_string*, param_string*, param_string*, return_value_void*, void> p_ui_report_error
        )
    {
        Logger.LogInput();
        try
        {
            var pluginsDelegate = new CallbackPluginDelegates(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_Plugins_GetAll>(new IntPtr(p_plugins_get_all))
            );

            var iniDelegate = new CallbackIniDelegates(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_Ini_GetIniString>(new IntPtr(p_ini_get_ini_string)),
                Marshal.GetDelegateForFunctionPointer<N_Ini_GetIniInt>(new IntPtr(p_ini_fet_ini_int))
            );
            
            var contextDelegate = new CallbackContextDelegates(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_Context_GetAppVersion>(new IntPtr(p_context_get_app_version)),
                Marshal.GetDelegateForFunctionPointer<N_Context_GetCurrentGameVersion>(new IntPtr(p_context_get_current_game_version)),
                Marshal.GetDelegateForFunctionPointer<N_Context_GetExtenderVersion>(new IntPtr(p_context_get_extender_version)),
                Marshal.GetDelegateForFunctionPointer<N_Context_IsExtenderPresent>(new IntPtr(p_context_is_extender_present)),
                Marshal.GetDelegateForFunctionPointer<N_Context_CheckIfFileExists>(new IntPtr(p_context_check_if_file_exists)),
                Marshal.GetDelegateForFunctionPointer<N_Context_GetExistingDataFile>(new IntPtr(p_context_get_existing_data_file)),
                Marshal.GetDelegateForFunctionPointer<N_Context_GetExistingDataFileList>(new IntPtr(p_context_get_existing_data_file_list))
            );
            
            var uiDelegate = new CallbackUIDelegates(p_owner,
                Marshal.GetDelegateForFunctionPointer<N_UI_StartDialog>(new IntPtr(p_ui_start_dialog)),
                Marshal.GetDelegateForFunctionPointer<N_UI_EndDialog>(new IntPtr(p_ui_end_dialog)),
                Marshal.GetDelegateForFunctionPointer<N_UI_UpdateState>(new IntPtr(p_ui_update_state)),
                Marshal.GetDelegateForFunctionPointer<N_UI_ReportError>(new IntPtr(p_ui_report_error))
            );
            
            var coreDelegates = new NativeCoreDelegatesHandler(p_owner, pluginsDelegate, contextDelegate, iniDelegate, uiDelegate);

            Logger.LogOutput();
            return return_value_ptr.AsValue(coreDelegates.HandlePtr, false);
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return return_value_ptr.AsException(e, false);
        }
    }
    */
    
    [UnmanagedCallersOnly(EntryPoint = "dispose_handler", CallConvs = [typeof(CallConvCdecl)])]
    public static return_value_void* DisposeHandler(param_ptr* p_handle)
    {
        Logger.LogInput();
        try
        {
            if (p_handle is null || NativeCoreDelegatesHandler.FromPointer(p_handle) is not { } handler)
                return return_value_void.AsError(BUTR.NativeAOT.Shared.Utils.Copy("Handler is null or wrong!", false), false);

            handler.Dispose();

            Logger.LogOutput();
            return return_value_void.AsValue(false);
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return return_value_void.AsException(e, false);
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "test_supported", CallConvs = [typeof(CallConvCdecl)]), IsNotConst<IsPtrConst>]
    public static return_value_json* TestSupported(
        // param_ptr* p_handle,
        [IsConst<IsPtrConst>] param_json* p_mod_archive_file_list,
        [IsConst<IsPtrConst>] param_json* p_allowed_types)
    {
        Logger.LogInput(p_mod_archive_file_list, p_allowed_types);
        try
        {
            //if (p_handle is null || NativeCoreDelegatesHandler.FromPointer(p_handle) is not { } handler)
            //    return return_value_json.AsError(BUTR.NativeAOT.Shared.Utils.Copy("Handler is null or wrong!", false), false);
            
            var modArchiveFileList = BUTR.NativeAOT.Shared.Utils.DeserializeJson(p_mod_archive_file_list, CustomSourceGenerationContext.StringArray);
            var allowedTypes = BUTR.NativeAOT.Shared.Utils.DeserializeJson(p_allowed_types, CustomSourceGenerationContext.StringArray);

            var result = Installer.TestSupported(modArchiveFileList.ToList(), allowedTypes.ToList());

            Logger.LogOutput(result);
            return return_value_json.AsValue(result, CustomSourceGenerationContext.SupportedResult, false);
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return return_value_json.AsException(e, false);
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "install", CallConvs = [typeof(CallConvCdecl)]), IsNotConst<IsPtrConst>]
    public static return_value_async* Install(
        param_ptr* p_handle,
        [IsConst<IsPtrConst>] param_json* p_mod_archive_file_list,
        [IsConst<IsPtrConst>] param_json* p_stop_patterns,
        [IsConst<IsPtrConst>] param_string* p_plugin_path,
        [IsConst<IsPtrConst>] param_string* p_script_path,
        [IsConst<IsPtrConst>] param_json* p_preset,
        [IsConst<IsPtrConst>] param_bool validate,
        param_ptr* p_callback_handler,
        delegate* unmanaged[Cdecl]<param_ptr*, return_value_json*, void> p_callback)
    {
        Logger.LogInput(p_mod_archive_file_list, p_stop_patterns, p_plugin_path, p_script_path, p_preset, &validate);
        try
        {
            if (p_handle is null || NativeCoreDelegatesHandler.FromPointer(p_handle) is not { } handler)
                return return_value_async.AsError(BUTR.NativeAOT.Shared.Utils.Copy("Handler is null or wrong!", false), false);

            var modArchiveFileList = BUTR.NativeAOT.Shared.Utils.DeserializeJson(p_mod_archive_file_list, CustomSourceGenerationContext.StringArray);
            var stopPatterns = BUTR.NativeAOT.Shared.Utils.DeserializeJson(p_stop_patterns, CustomSourceGenerationContext.StringArray);
            var pluginPath = new string(param_string.ToSpan(p_plugin_path));
            var scriptPath = new string(param_string.ToSpan(p_script_path));
            var preset = BUTR.NativeAOT.Shared.Utils.DeserializeJson(p_preset, CustomSourceGenerationContext.JsonDocument);

            var progressDelegate = new ProgressDelegate((progress) => { });
            
            Installer.Install(modArchiveFileList.ToList(), stopPatterns.ToList(), pluginPath, scriptPath, preset, validate, progressDelegate, handler).ContinueWith(result =>
            {
                Logger.LogAsyncInput($"{nameof(Install)}_Callback");

                if (result.Exception is not null)
                {
                    p_callback(p_callback_handler, return_value_json.AsException(result.Exception, false));
                    Logger.LogException(result.Exception, $"{nameof(Install)}_Callback");
                }
                else
                {
                    p_callback(p_callback_handler, return_value_json.AsValue(result.Result, CustomSourceGenerationContext.InstallResult, false));
                    Logger.LogOutput(result.Result.Instructions.Count > 0 ? result.Result.Instructions[0].type : "", $"{nameof(Install)}_Callback");
                }
            });

            Logger.LogOutput();
            return return_value_async.AsValue(false);
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return return_value_async.AsException(e, false);
        }
    }
}