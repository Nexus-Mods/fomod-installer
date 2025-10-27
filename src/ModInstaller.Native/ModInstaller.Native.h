
#ifndef SRC_BINDINGS_H_
#define SRC_BINDINGS_H_

#ifndef __cplusplus

#include <stdlib.h>
#include <stdint.h>

#else

#include <memory>
#include <string>
#include <cstdint>
#include <cuchar>

namespace ModInstaller::Native
{
    extern "C"
    {
#endif

#ifndef __cplusplus
        typedef char16_t wchar_t;
#endif
        typedef char16_t param_string;
        typedef char16_t param_json;
        typedef uint8_t param_data;
        typedef uint8_t param_bool;
        typedef int32_t param_int;
        typedef uint32_t param_uint;
        typedef void param_ptr;
        typedef struct param_callback
        {
            void *p_callback_ptr;
            void(__cdecl *p_callback)(param_ptr *, param_ptr *);
        } param_callback;

        typedef struct return_value_void
        {
            param_string *const error;
        } return_value_void;
        typedef struct return_value_string
        {
            param_string *const error;
            param_string *const value;
        } return_value_string;
        typedef struct return_value_json
        {
            param_string *const error;
            param_json *const value;
        } return_value_json;
        typedef struct return_value_data
        {
            param_string *const error;
            param_data *const value;
            param_int length;
        } return_value_data;
        typedef struct return_value_bool
        {
            param_string *const error;
            param_bool const value;
        } return_value_bool;
        typedef struct return_value_int32
        {
            param_string *const error;
            param_int const value;
        } return_value_int32;
        typedef struct return_value_uint32
        {
            param_string *const error;
            param_uint const value;
        } return_value_uint32;
        typedef struct return_value_ptr
        {
            param_string *const error;
            param_ptr *const value;
        } return_value_ptr;
        typedef struct return_value_async
        {
            param_string *const error;
        } return_value_async;

    void* __cdecl common_alloc(size_t size);
    int32_t __cdecl common_alloc_alive_count();
    void __cdecl common_dealloc(param_ptr* ptr);
    return_value_ptr* __cdecl create_handler(param_ptr* p_owner, return_value_void* (__cdecl p_plugins_get_all)(param_ptr*, param_bool, param_ptr*, void (__cdecl )(param_ptr*, return_value_json*)) , return_value_void* (__cdecl p_context_get_app_version)(param_ptr*, param_ptr*, void (__cdecl )(param_ptr*, return_value_string*)) , return_value_void* (__cdecl p_context_get_current_game_version)(param_ptr*, param_ptr*, void (__cdecl )(param_ptr*, return_value_string*)) , return_value_void* (__cdecl p_context_get_extender_version)(param_ptr*, param_string*, param_ptr*, void (__cdecl )(param_ptr*, return_value_string*)) , return_value_void* (__cdecl p_ui_start_dialog)(param_ptr*, param_string*, param_json*, param_ptr*, void (__cdecl )(param_ptr*, param_int, param_int, param_json*, return_value_void*), void (__cdecl )(param_ptr*, param_bool, param_int, return_value_void*), void (__cdecl )(param_ptr*, return_value_void*)) , return_value_void* (__cdecl p_ui_end_dialog)(param_ptr*) , return_value_void* (__cdecl p_ui_update_state)(param_ptr*, param_json*, param_int) );
    return_value_ptr* __cdecl create_handler_with_fs(param_ptr* p_owner, return_value_void* (__cdecl p_plugins_get_all)(param_ptr*, param_bool, param_ptr*, void (__cdecl )(param_ptr*, return_value_json*)) , return_value_void* (__cdecl p_context_get_app_version)(param_ptr*, param_ptr*, void (__cdecl )(param_ptr*, return_value_string*)) , return_value_void* (__cdecl p_context_get_current_game_version)(param_ptr*, param_ptr*, void (__cdecl )(param_ptr*, return_value_string*)) , return_value_void* (__cdecl p_context_get_extender_version)(param_ptr*, param_string*, param_ptr*, void (__cdecl )(param_ptr*, return_value_string*)) , return_value_void* (__cdecl p_ui_start_dialog)(param_ptr*, param_string*, param_json*, param_ptr*, void (__cdecl )(param_ptr*, param_int, param_int, param_json*, return_value_void*), void (__cdecl )(param_ptr*, param_bool, param_int, return_value_void*), void (__cdecl )(param_ptr*, return_value_void*)) , return_value_void* (__cdecl p_ui_end_dialog)(param_ptr*) , return_value_void* (__cdecl p_ui_update_state)(param_ptr*, param_json*, param_int) , return_value_data* (__cdecl p_read_file_content)(param_ptr*, param_string*, param_int, param_int) , return_value_json* (__cdecl p_read_directory_file_list)(param_ptr*, param_string*, param_string*, param_int) , return_value_json* (__cdecl p_read_directory_list)(param_ptr*, param_string*) );
    return_value_void* __cdecl dispose_handler(param_ptr* p_handle);
    return_value_async* __cdecl install(param_ptr* p_handle, param_json* p_mod_archive_file_list, param_json* p_stop_patterns, param_string* p_plugin_path, param_string* p_script_path, param_json* p_preset, param_bool validate, param_ptr* p_callback_handler, void (__cdecl p_callback)(param_ptr*, return_value_json*) );
    return_value_json* __cdecl test_supported(param_json* p_mod_archive_file_list, param_json* p_allowed_types);


#ifdef __cplusplus
    }
}
#endif

#endif
