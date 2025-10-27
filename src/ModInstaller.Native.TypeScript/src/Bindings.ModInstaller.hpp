#ifndef VE_MODINSTALLER_GUARD_HPP_
#define VE_MODINSTALLER_GUARD_HPP_

#include "utils.hpp"
#include "Utils.Callback.hpp"
#include "Utils.Async.hpp"
#include "ModInstaller.Native.h"
#include <codecvt>
#include <mutex>
#include <condition_variable>

using namespace Napi;
using namespace Utils;
using namespace Utils::Async;
using namespace Utils::Callback;
using namespace ModInstaller::Native;

namespace Bindings::ModInstaller
{
    class ModInstaller : public Napi::ObjectWrap<ModInstaller>
    {
    public:
        Napi::ThreadSafeFunction TSFN;
        Napi::ThreadSafeFunction TSFNPluginsGetAll;
        Napi::ThreadSafeFunction TSFNContextGetAppVersion;
        Napi::ThreadSafeFunction TSFNContextGetCurrentGameVersion;
        Napi::ThreadSafeFunction TSFNContextGetExtenderVersion;
        Napi::ThreadSafeFunction TSFNUIStartDialog;
        Napi::ThreadSafeFunction TSFNUIEndDialog;
        Napi::ThreadSafeFunction TSFNUIUpdateState;
        Napi::ThreadSafeFunction TSFNReadFileContent;
        Napi::ThreadSafeFunction TSFNReadDirectoryFileList;
        Napi::ThreadSafeFunction TSFNReadDirectoryList;

        static void TSFNFunction(const Napi::CallbackInfo &info)
        {
            LoggerScope logger(__FUNCTION__);
            const auto length = info.Length();
            logger.Log("Length: " + std::to_string(length));
            if (length == 2)
            {
                const auto promise = info[0].As<Napi::Promise>();
                const auto onResolve = info[1].As<Napi::Function>();
                const auto then = promise.Get("then").As<Napi::Function>();
                then.Call(promise, {onResolve});
            }
            else if (length == 3)
            {
                const auto promise = info[0].As<Napi::Promise>();
                const auto onResolve = info[1].As<Napi::Function>();
                const auto onReject = info[2].As<Napi::Function>();
                const auto then = promise.Get("then").As<Napi::Function>();
                then.Call(promise, {onResolve, onReject});
            }
        }

        FunctionReference FPluginsGetAll;
        FunctionReference FContextGetAppVersion;
        FunctionReference FContextGetCurrentGameVersion;
        FunctionReference FContextGetExtenderVersion;
        FunctionReference FUIStartDialog;
        FunctionReference FUIEndDialog;
        FunctionReference FUIUpdateState;
        FunctionReference FReadFileContent;
        FunctionReference FReadDirectoryFileList;
        FunctionReference FReadDirectoryList;

        static Object Init(const Napi::Env env, const Object exports);

        ModInstaller(const CallbackInfo &info);
        ~ModInstaller();

        Napi::Value TestSupported(const CallbackInfo &info);
        Napi::Value Install(const CallbackInfo &info);

    private:
        void *_pInstance;
    };

    // Initialize native add-on
    Napi::Object Init(const Napi::Env env, const Napi::Object exports)
    {
        return ModInstaller::Init(env, exports);
    }

    Object ModInstaller::Init(const Napi::Env env, Object exports)
    {
        // This method is used to hook the accessor and method callbacks
        const auto func = DefineClass(env, "ModInstaller",
                                      {
                                          InstanceMethod<&ModInstaller::TestSupported>("testSupported", static_cast<napi_property_attributes>(napi_writable | napi_configurable)),
                                          InstanceMethod<&ModInstaller::Install>("install", static_cast<napi_property_attributes>(napi_writable | napi_configurable)),
                                      });

        auto *const constructor = new FunctionReference();

        // Create a persistent reference to the class constructor. This will allow
        // a function called on a class prototype and a function
        // called on instance of a class to be distinguished from each other.
        *constructor = Persistent(func);
        exports.Set("ModInstaller", func);

        // Store the constructor as the add-on instance data. This will allow this
        // add-on to support multiple instances of itself running on multiple worker
        // threads, as well as multiple instances of itself running in different
        // contexts on the same thread.
        //
        // By default, the value set on the environment here will be destroyed when
        // the add-on is unloaded using the `delete` operator, but it is also
        // possible to supply a custom deleter.
        const_cast<Napi::Env&>(env).SetInstanceData<FunctionReference>(constructor);

        return exports;
    }

    static return_value_void *pluginsGetAll(param_ptr *p_owner,
                                            param_bool active_only,
                                            param_ptr *p_callback_handler,
                                            void (*p_callback)(param_ptr *, return_value_json *)) noexcept
    {
        using ContextType = AsyncCallbackContext<decltype(p_callback)>;
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName, active_only);
        try
        {
            auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

            auto *context = new ContextType{p_callback_handler, p_callback};
            const auto callback = [functionName, active_only](Napi::Env env, Napi::Function jsCallback, ContextType *data)
            {
                LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                try
                {
                    const auto activeOnly = Boolean::New(env, active_only != 0);
                    const auto promise = jsCallback({activeOnly}).As<Promise>();

                    const auto onResolve = [functionName, data, &callbackLogger](const CallbackInfo &info)
                    {
                        LoggerScope resolveLogger(NAMEOFWITHCALLBACK(functionName, onResolve));
                        try
                        {
                            const auto result = ConvertToJsonResult(resolveLogger, info[0]);
                            data->callback(data->callback_handler, result);
                            delete data;
                        }
                        catch (const Napi::Error &e)
                        {
                            resolveLogger.Log("Error: " + std::string(e.Message()));
                            auto errorResult = Create(return_value_json{Copy(GetErrorMessage(e)), nullptr});
                            data->callback(data->callback_handler, errorResult);
                            delete data;
                        }
                    };
                    const auto onReject = [functionName, data, &callbackLogger](const CallbackInfo &info)
                    {
                        LoggerScope rejectLogger(NAMEOFWITHCALLBACK(functionName, onReject));
                        const auto env = info.Env();
                        const auto error = info.Length() > 0 ? info[0].As<Error>() : Error::New(env, "Unknown error");
                        rejectLogger.Log("Rejected with error: " + std::string(error.Message()));
                        auto errorResult = Create(return_value_json{Copy(GetErrorMessage(error)), nullptr});
                        data->callback(data->callback_handler, errorResult);
                        delete data;
                    };

                    // Attach handlers to promise
                    const auto then = promise.Get("then").As<Function>();
                    const auto thenResult = then.Call(promise, {Function::New(env, onResolve), Function::New(env, onReject)});
                }
                catch (const Napi::Error &e)
                {
                    callbackLogger.Log("Error: " + std::string(e.Message()));
                    auto errorResult = Create(return_value_json{Copy(GetErrorMessage(e)), nullptr});
                    data->callback(data->callback_handler, errorResult);
                    delete data;
                }
            };

            manager->TSFNPluginsGetAll.NonBlockingCall(context, callback);

            logger.Log("Async call queued");
            return Create(return_value_void{nullptr});
        }
        catch (const Napi::Error &e)
        {
            logger.Log("Error: " + std::string(e.Message()));
            return Create(return_value_void{Copy(GetErrorMessage(e))});
        }
        catch (const std::exception &e)
        {
            logger.Log("Exception: " + std::string(e.what()));
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_void{Copy(conv.from_bytes(e.what()))});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_void{Copy(u"Unknown exception")});
        }
    }
    static return_value_void *contextGetAppVersion(param_ptr *p_owner,
                                                   param_ptr *p_callback_handler,
                                                   void (*p_callback)(param_ptr *, return_value_string *)) noexcept
    {
        using ContextType = AsyncCallbackContext<decltype(p_callback)>;
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        try
        {
            auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

            auto *context = new ContextType{p_callback_handler, p_callback};
            const auto callback = [functionName](Napi::Env env, Napi::Function jsCallback, ContextType *data)
            {
                LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                try
                {
                    const auto promise = jsCallback({}).As<Promise>();

                    // Create then/catch handlers for the promise
                    const auto onResolve = [functionName, data, &callbackLogger](const CallbackInfo &info)
                    {
                        LoggerScope resolveLogger(NAMEOFWITHCALLBACK(functionName, onResolve));
                        try
                        {
                            const auto result = ConvertToStringResult(resolveLogger, info[0]);
                            data->callback(data->callback_handler, result);
                            delete data;
                        }
                        catch (const Napi::Error &e)
                        {
                            resolveLogger.Log("Error: " + std::string(e.Message()));
                            auto errorResult = Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
                            data->callback(data->callback_handler, errorResult);
                            delete data;
                        }
                    };
                    const auto onReject = [functionName, data, &callbackLogger](const CallbackInfo &info)
                    {
                        LoggerScope rejectLogger(NAMEOFWITHCALLBACK(functionName, onReject));
                        const auto env = info.Env();
                        const auto error = info.Length() > 0 ? info[0].As<Error>() : Error::New(env, "Unknown error");
                        rejectLogger.Log("Rejected with error: " + std::string(error.Message()));
                        auto errorResult = Create(return_value_string{Copy(GetErrorMessage(error)), nullptr});
                        data->callback(data->callback_handler, errorResult);
                        delete data;
                    };

                    // Attach handlers to promise
                    const auto then = promise.Get("then").As<Function>();
                    const auto thenResult = then.Call(promise, {Function::New(env, onResolve), Function::New(env, onReject)});
                }
                catch (const Napi::Error &e)
                {
                    callbackLogger.Log("Error: " + std::string(e.Message()));
                    auto errorResult = Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
                    data->callback(data->callback_handler, errorResult);
                    delete data;
                }
            };

            manager->TSFNContextGetAppVersion.NonBlockingCall(context, callback);

            logger.Log("Async call queued");
            return Create(return_value_void{nullptr});
        }
        catch (const Napi::Error &e)
        {
            logger.Log("Error: " + std::string(e.Message()));
            return Create(return_value_void{Copy(GetErrorMessage(e))});
        }
        catch (const std::exception &e)
        {
            logger.Log("Exception: " + std::string(e.what()));
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_void{Copy(conv.from_bytes(e.what()))});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_void{Copy(u"Unknown exception")});
        }
    }
    static return_value_void *contextGetCurrentGameVersion(param_ptr *p_owner,
                                                           param_ptr *p_callback_handler,
                                                           void (*p_callback)(param_ptr *, return_value_string *)) noexcept
    {
        using ContextType = AsyncCallbackContext<decltype(p_callback)>;
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        try
        {
            auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

            auto *context = new ContextType{p_callback_handler, p_callback};
            const auto callback = [functionName](Napi::Env env, Napi::Function jsCallback, ContextType *data)
            {
                LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                try
                {
                    const auto promise = jsCallback({}).As<Promise>();

                    const auto onResolve = [functionName, data, &callbackLogger](const CallbackInfo &info)
                    {
                        LoggerScope resolveLogger(NAMEOFWITHCALLBACK(functionName, onResolve));
                        try
                        {
                            const auto result = ConvertToStringResult(resolveLogger, info[0]);
                            data->callback(data->callback_handler, result);
                            delete data;
                        }
                        catch (const Napi::Error &e)
                        {
                            resolveLogger.Log("Error: " + std::string(e.Message()));
                            auto errorResult = Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
                            data->callback(data->callback_handler, errorResult);
                            delete data;
                        }
                    };

                    const auto onReject = [functionName, data, &callbackLogger](const CallbackInfo &info)
                    {
                        LoggerScope rejectLogger(NAMEOFWITHCALLBACK(functionName, onReject));
                        const auto env = info.Env();
                        const auto error = info.Length() > 0 ? info[0].As<Error>() : Error::New(env, "Unknown error");
                        rejectLogger.Log("Rejected with error: " + std::string(error.Message()));
                        auto errorResult = Create(return_value_string{Copy(GetErrorMessage(error)), nullptr});
                        data->callback(data->callback_handler, errorResult);
                        delete data;
                    };

                    // Attach handlers to promise
                    const auto then = promise.Get("then").As<Function>();
                    const auto thenResult = then.Call(promise, {Function::New(env, onResolve), Function::New(env, onReject)});
                }
                catch (const Napi::Error &e)
                {
                    callbackLogger.Log("Error: " + std::string(e.Message()));
                    auto errorResult = Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
                    data->callback(data->callback_handler, errorResult);
                    delete data;
                }
            };

            manager->TSFNContextGetCurrentGameVersion.NonBlockingCall(context, callback);

            logger.Log("Async call queued");
            return Create(return_value_void{nullptr});
        }
        catch (const Napi::Error &e)
        {
            logger.Log("Error: " + std::string(e.Message()));
            return Create(return_value_void{Copy(GetErrorMessage(e))});
        }
        catch (const std::exception &e)
        {
            logger.Log("Exception: " + std::string(e.what()));
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_void{Copy(conv.from_bytes(e.what()))});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_void{Copy(u"Unknown exception")});
        }
    }
    static return_value_void *contextGetExtenderVersion(param_ptr *p_owner,
                                                        param_string *p_extender,
                                                        param_ptr *p_callback_handler,
                                                        void (*p_callback)(param_ptr *, return_value_string *)) noexcept
    {
        using ContextType = AsyncCallbackContext<decltype(p_callback)>;
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        try
        {
            auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

            auto *context = new ContextType{p_callback_handler, p_callback};
            const auto callback = [functionName, p_extender](Napi::Env env, Napi::Function jsCallback, ContextType *data)
            {
                LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                try
                {
                    const auto extender = p_extender == nullptr ? env.Null() : String::New(env, p_extender);
                    const auto promise = jsCallback({extender}).As<Promise>();

                    const auto onResolve = [functionName, data, &callbackLogger](const CallbackInfo &info)
                    {
                        LoggerScope resolveLogger(NAMEOFWITHCALLBACK(functionName, onResolve));
                        try
                        {
                            const auto result = ConvertToStringResult(resolveLogger, info[0]);
                            data->callback(data->callback_handler, result);
                            delete data;
                        }
                        catch (const Napi::Error &e)
                        {
                            resolveLogger.Log("Error: " + std::string(e.Message()));
                            auto errorResult = Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
                            data->callback(data->callback_handler, errorResult);
                            delete data;
                        }
                    };
                    const auto onReject = [functionName, data, &callbackLogger](const CallbackInfo &info)
                    {
                        LoggerScope rejectLogger(NAMEOFWITHCALLBACK(functionName, onReject));
                        const auto env = info.Env();
                        const auto error = info.Length() > 0 ? info[0].As<Error>() : Error::New(env, "Unknown error");
                        rejectLogger.Log("Rejected with error: " + std::string(error.Message()));
                        auto errorResult = Create(return_value_string{Copy(GetErrorMessage(error)), nullptr});
                        data->callback(data->callback_handler, errorResult);
                        delete data;
                    };

                    // Attach handlers to promise
                    const auto then = promise.Get("then").As<Function>();
                    const auto thenResult = then.Call(promise, {Function::New(env, onResolve), Function::New(env, onReject)});
                }
                catch (const Napi::Error &e)
                {
                    callbackLogger.Log("Error: " + std::string(e.Message()));
                    auto errorResult = Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
                    data->callback(data->callback_handler, errorResult);
                    delete data;
                }
            };

            manager->TSFNContextGetExtenderVersion.NonBlockingCall(context, callback);

            logger.Log("Async call queued");
            return Create(return_value_void{nullptr});
        }
        catch (const Napi::Error &e)
        {
            logger.Log("Error: " + std::string(e.Message()));
            return Create(return_value_void{Copy(GetErrorMessage(e))});
        }
        catch (const std::exception &e)
        {
            logger.Log("Exception: " + std::string(e.what()));
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_void{Copy(conv.from_bytes(e.what()))});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_void{Copy(u"Unknown exception")});
        }
    }
    static return_value_data *readFileContent(param_ptr *p_owner,
                                              param_string *p_file_path,
                                              param_int v_offset,
                                              param_int v_length) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

        const auto callback = [functionName, p_file_path, v_offset, v_length](Napi::Env env, Napi::Function jsCallback, CallbackData<return_value_data> *data)
        {
            LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
            const auto filePath = p_file_path == nullptr ? String::New(env, "") : String::New(env, p_file_path);
            const auto offset = Number::New(env, v_offset);
            const auto length = Number::New(env, v_length);
            HandleCallbackDataResultWithSignal(callbackLogger, env, jsCallback({filePath, offset, length}), data);
        };

        return ExecuteBlockingCallbackWithSignal(logger, manager->TSFNReadFileContent, callback, CreateDataError);
    }
    static return_value_json *readDirectoryFileList(param_ptr *p_owner,
                                                    param_string *p_directory_path,
                                                    param_string *p_pattern,
                                                    param_int search_type) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName, p_directory_path, p_pattern, search_type);
        auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

        const auto callback = [functionName, p_directory_path, p_pattern, search_type](Napi::Env env, Napi::Function jsCallback, CallbackData<return_value_json> *data)
        {
            LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
            const auto directoryPath = p_directory_path == nullptr ? String::New(env, "") : String::New(env, p_directory_path);
            const auto pattern = p_pattern == nullptr ? String::New(env, "") : String::New(env, p_pattern);
            const auto searchType = Number::New(env, search_type);
            HandleCallbackJsonResultWithSignal(callbackLogger, env, jsCallback({directoryPath, pattern, searchType}), data);
        };

        return ExecuteBlockingCallbackWithSignal(logger, manager->TSFNReadDirectoryFileList, callback, CreateJsonError);
    }
    static return_value_json *readDirectoryList(param_ptr *p_owner,
                                                param_string *p_directory_path) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName, p_directory_path);
        auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

        const auto callback = [functionName, p_directory_path](Napi::Env env, Napi::Function jsCallback, CallbackData<return_value_json> *data)
        {
            LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
            const auto directoryPath = p_directory_path == nullptr ? String::New(env, "") : String::New(env, p_directory_path);
            HandleCallbackJsonResultWithSignal(callbackLogger, env, jsCallback({directoryPath}), data);
        };

        return ExecuteBlockingCallbackWithSignal(logger, manager->TSFNReadDirectoryList, callback, CreateJsonError);
    }
    static return_value_void *uiStartDialog(param_ptr *p_owner,
                                            param_string *p_module_name,
                                            param_json *p_image,
                                            param_ptr *p_callback_handler,
                                            void (*p_select_callback)(param_ptr *, param_int, param_int, param_json *, return_value_void *),
                                            void (*p_const_callback)(param_ptr *, param_bool, param_int, return_value_void *),
                                            void (*p_cancel_callback)(param_ptr *, return_value_void *)) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        try
        {
            auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

            const auto callback = [functionName, p_module_name, p_image, p_callback_handler, p_select_callback, p_const_callback, p_cancel_callback](Napi::Env env, Napi::Function jsCallback)
            {
                LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                try
                {
                    const auto moduleName = p_module_name == nullptr ? env.Null() : String::New(env, p_module_name);
                    const auto image = p_image == nullptr ? env.Null() : JSONParse(Napi::String::New(env, p_image));

                    const auto selectCallback = [functionName, p_callback_handler, p_select_callback](const CallbackInfo &info)
                    {
                        LoggerScope selectLogger(NAMEOFWITHCALLBACK(functionName, selectCallback));
                        try
                        {
                            const auto groupId = info[0].As<Number>().Int32Value();
                            const auto optionId = info[1].As<Number>().Int32Value();
                            const auto selectedIds = JSONStringify(info[2].As<Object>());
                            const auto selectedIdsCopy = CopyWithFree(selectedIds.Utf16Value());

                            auto result = Create(return_value_void{nullptr});
                            p_select_callback(p_callback_handler, groupId, optionId, selectedIdsCopy.get(), result);

                            if (result->error != nullptr)
                            {
                                const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
                                common_dealloc(reinterpret_cast<param_ptr *>(result));
                                throw Error::New(info.Env(), String::New(info.Env(), error.get()));
                            }
                            common_dealloc(reinterpret_cast<param_ptr *>(result));
                        }
                        catch (const Napi::Error &e)
                        {
                            selectLogger.Log("Error: " + std::string(e.Message()));
                            throw;
                        }
                    };
                    const auto constCallback = [functionName, p_callback_handler, p_const_callback](const CallbackInfo &info)
                    {
                        LoggerScope contLogger(NAMEOFWITHCALLBACK(functionName, constCallback));
                        try
                        {
                            const auto goForward = info[0].As<Boolean>().Value() ? (uint8_t)1 : (uint8_t)0;
                            const auto stepId = info[1].As<Number>().Int32Value();

                            auto result = Create(return_value_void{nullptr});
                            p_const_callback(p_callback_handler, goForward, stepId, result);

                            if (result->error != nullptr)
                            {
                                const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
                                common_dealloc(reinterpret_cast<param_ptr *>(result));
                                throw Error::New(info.Env(), String::New(info.Env(), error.get()));
                            }
                            common_dealloc(reinterpret_cast<param_ptr *>(result));
                        }
                        catch (const Napi::Error &e)
                        {
                            contLogger.Log("Error: " + std::string(e.Message()));
                            throw;
                        }
                    };
                    const auto cancelCallback = [functionName, p_callback_handler, p_cancel_callback](const CallbackInfo &info)
                    {
                        LoggerScope cancelLogger(NAMEOFWITHCALLBACK(functionName, cancelCallback));
                        try
                        {
                            auto result = Create(return_value_void{nullptr});
                            p_cancel_callback(p_callback_handler, result);

                            if (result->error != nullptr)
                            {
                                const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
                                common_dealloc(reinterpret_cast<param_ptr *>(result));
                                throw Error::New(info.Env(), String::New(info.Env(), error.get()));
                            }
                            common_dealloc(reinterpret_cast<param_ptr *>(result));
                        }
                        catch (const Napi::Error &e)
                        {
                            cancelLogger.Log("Error: " + std::string(e.Message()));
                            throw;
                        }
                    };

                    const auto result = jsCallback({moduleName, image, Function::New(env, selectCallback, NAMEOF(selectCallback)), Function::New(env, constCallback, NAMEOF(constCallback)), Function::New(env, cancelCallback, NAMEOF(cancelCallback))});

                    // If the result is a promise, attach a rejection handler to log errors
                    if (result.IsPromise())
                    {
                        const auto promise = result.As<Promise>();
                        const auto onReject = [functionName](const CallbackInfo &info)
                        {
                            LoggerScope rejectLogger(NAMEOFWITHCALLBACK(functionName, onReject));
                            const auto env = info.Env();
                            const auto error = info.Length() > 0 ? info[0].As<Error>() : Error::New(env, "Unknown error");
                            rejectLogger.Log("Rejected with error: " + std::string(error.Message()));
                        };

                        const auto catchFn = promise.Get("catch").As<Function>();
                        catchFn.Call(promise, {Function::New(env, onReject)});
                    }
                }
                catch (const Napi::Error &e)
                {
                    callbackLogger.Log("Error: " + std::string(e.Message()));
                }
            };

            manager->TSFNUIStartDialog.NonBlockingCall(callback);

            logger.Log("Async call queued");
            return Create(return_value_void{nullptr});
        }
        catch (const Napi::Error &e)
        {
            logger.Log("Error: " + std::string(e.Message()));
            return Create(return_value_void{Copy(GetErrorMessage(e))});
        }
        catch (const std::exception &e)
        {
            logger.Log("Exception: " + std::string(e.what()));
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_void{Copy(conv.from_bytes(e.what()))});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_void{Copy(u"Unknown exception")});
        }
    }
    static return_value_void *uiEndDialog(param_ptr *p_owner) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        try
        {
            auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

            const auto callback = [functionName](Napi::Env env, Napi::Function jsCallback)
            {
                LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                try
                {
                    const auto result = jsCallback({});

                    // If the result is a promise, attach a rejection handler to log errors
                    if (result.IsPromise())
                    {
                        const auto promise = result.As<Promise>();
                        const auto onReject = [functionName](const CallbackInfo &info)
                        {
                            LoggerScope rejectLogger(NAMEOFWITHCALLBACK(functionName, onReject));
                            const auto env = info.Env();
                            const auto error = info.Length() > 0 ? info[0].As<Error>() : Error::New(env, "Unknown error");
                            rejectLogger.Log("Rejected with error: " + std::string(error.Message()));
                        };

                        const auto catchFn = promise.Get("catch").As<Function>();
                        catchFn.Call(promise, {Function::New(env, onReject)});
                    }
                }
                catch (const Napi::Error &e)
                {
                    callbackLogger.Log("Error: " + std::string(e.Message()));
                }
            };

            manager->TSFNUIEndDialog.NonBlockingCall(callback);

            logger.Log("Async call queued");
            return Create(return_value_void{nullptr});
        }
        catch (const Napi::Error &e)
        {
            logger.Log("Error: " + std::string(e.Message()));
            return Create(return_value_void{Copy(GetErrorMessage(e))});
        }
        catch (const std::exception &e)
        {
            logger.Log("Exception: " + std::string(e.what()));
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_void{Copy(conv.from_bytes(e.what()))});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_void{Copy(u"Unknown exception")});
        }
    }
    static return_value_void *uiUpdateState(param_ptr *p_owner,
                                            param_json *p_install_steps,
                                            param_int current_step) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        try
        {
            auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

            const auto callback = [functionName, p_install_steps, current_step](Napi::Env env, Napi::Function jsCallback)
            {
                LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                try
                {
                    const auto installSteps = p_install_steps == nullptr ? env.Null() : JSONParse(Napi::String::New(env, p_install_steps));
                    const auto stepNumber = Number::New(env, current_step);
                    const auto result = jsCallback({installSteps, stepNumber});

                    // If the result is a promise, attach a rejection handler to log errors
                    if (result.IsPromise())
                    {
                        const auto promise = result.As<Promise>();
                        const auto onReject = [functionName](const CallbackInfo &info)
                        {
                            LoggerScope rejectLogger(NAMEOFWITHCALLBACK(functionName, onReject));
                            const auto env = info.Env();
                            const auto error = info.Length() > 0 ? info[0].As<Error>() : Error::New(env, "Unknown error");
                            rejectLogger.Log("Rejected with error: " + std::string(error.Message()));
                        };

                        const auto catchFn = promise.Get("catch").As<Function>();
                        catchFn.Call(promise, {Function::New(env, onReject)});
                    }
                }
                catch (const Napi::Error &e)
                {
                    callbackLogger.Log("Error: " + std::string(e.Message()));
                }
            };

            manager->TSFNUIUpdateState.NonBlockingCall(callback);

            logger.Log("Async call queued");
            return Create(return_value_void{nullptr});
        }
        catch (const Napi::Error &e)
        {
            logger.Log("Error: " + std::string(e.Message()));
            return Create(return_value_void{Copy(GetErrorMessage(e))});
        }
        catch (const std::exception &e)
        {
            logger.Log("Exception: " + std::string(e.what()));
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_void{Copy(conv.from_bytes(e.what()))});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_void{Copy(u"Unknown exception")});
        }
    }

    ModInstaller::ModInstaller(const CallbackInfo &info) : ObjectWrap<ModInstaller>(info)
    {
        LoggerScope logger(__FUNCTION__);

        const auto env = Env();
        this->TSFN = Napi::ThreadSafeFunction::New(env, Napi::Function::New(env, TSFNFunction), "TSFN", 0, 1);
        this->FPluginsGetAll = Persistent(info[0].As<Function>());
        this->FContextGetAppVersion = Persistent(info[1].As<Function>());
        this->FContextGetCurrentGameVersion = Persistent(info[2].As<Function>());
        this->FContextGetExtenderVersion = Persistent(info[3].As<Function>());
        this->FUIStartDialog = Persistent(info[4].As<Function>());
        this->FUIEndDialog = Persistent(info[5].As<Function>());
        this->FUIUpdateState = Persistent(info[6].As<Function>());
        // this->FReadFileContent = Persistent(info[7].As<Function>());
        // this->FReadDirectoryFileList = Persistent(info[8].As<Function>());
        // this->FReadDirectoryList = Persistent(info[9].As<Function>());

        // Initialize thread-safe function wrappers for synchronous callbacks
        this->TSFNPluginsGetAll = Napi::ThreadSafeFunction::New(env, this->FPluginsGetAll.Value(), "PluginsGetAll", 0, 1);
        this->TSFNContextGetAppVersion = Napi::ThreadSafeFunction::New(env, this->FContextGetAppVersion.Value(), "ContextGetAppVersion", 0, 1);
        this->TSFNContextGetCurrentGameVersion = Napi::ThreadSafeFunction::New(env, this->FContextGetCurrentGameVersion.Value(), "ContextGetCurrentGameVersion", 0, 1);
        this->TSFNContextGetExtenderVersion = Napi::ThreadSafeFunction::New(env, this->FContextGetExtenderVersion.Value(), "ContextGetExtenderVersion", 0, 1);
        this->TSFNUIStartDialog = Napi::ThreadSafeFunction::New(env, this->FUIStartDialog.Value(), "UIStartDialog", 0, 1);
        this->TSFNUIEndDialog = Napi::ThreadSafeFunction::New(env, this->FUIEndDialog.Value(), "UIEndDialog", 0, 1);
        this->TSFNUIUpdateState = Napi::ThreadSafeFunction::New(env, this->FUIUpdateState.Value(), "UIUpdateState", 0, 1);
        // this->TSFNReadFileContent = Napi::ThreadSafeFunction::New(env, this->FReadFileContent.Value(), "ReadFileContent", 0, 1);
        // this->TSFNReadDirectoryFileList = Napi::ThreadSafeFunction::New(env, this->FReadDirectoryFileList.Value(), "ReadDirectoryFileList", 0, 1);
        // this->TSFNReadDirectoryList = Napi::ThreadSafeFunction::New(env, this->FReadDirectoryList.Value(), "ReadDirectoryList", 0, 1);

        const auto result = create_handler(this,
                                           pluginsGetAll,
                                           contextGetAppVersion,
                                           contextGetCurrentGameVersion,
                                           contextGetExtenderVersion,
                                           uiStartDialog,
                                           uiEndDialog,
                                           uiUpdateState);
        this->_pInstance = ThrowOrReturnPtr(env, result);
    }

    ModInstaller::~ModInstaller()
    {
        LoggerScope logger(__FUNCTION__);

        // Release thread-safe functions
        this->TSFN.Release();
        this->TSFNPluginsGetAll.Release();
        this->TSFNContextGetAppVersion.Release();
        this->TSFNContextGetCurrentGameVersion.Release();
        this->TSFNContextGetExtenderVersion.Release();
        this->TSFNUIStartDialog.Release();
        this->TSFNUIEndDialog.Release();
        this->TSFNUIUpdateState.Release();
        this->TSFNReadFileContent.Release();
        this->TSFNReadDirectoryFileList.Release();
        this->TSFNReadDirectoryList.Release();

        // Release function references
        this->FPluginsGetAll.Unref();
        this->FContextGetAppVersion.Unref();
        this->FContextGetCurrentGameVersion.Unref();
        this->FContextGetExtenderVersion.Unref();
        this->FUIStartDialog.Unref();
        this->FUIEndDialog.Unref();
        this->FUIUpdateState.Unref();
        this->FReadFileContent.Unref();
        this->FReadDirectoryList.Unref();
        this->FReadDirectoryFileList.Unref();
        dispose_handler(this->_pInstance);
    }

    Value ModInstaller::TestSupported(const CallbackInfo &info)
    {
        LoggerScope logger(__FUNCTION__);

        const auto env = info.Env();
        const auto modArchiveFileList = JSONStringify(info[0].As<Object>());
        const auto allowedTypes = JSONStringify(info[1].As<Object>());

        const auto modArchiveFileListCopy = CopyWithFree(modArchiveFileList.Utf16Value());
        const auto allowedTypesCopy = CopyWithFree(allowedTypes.Utf16Value());

        const auto result = test_supported(modArchiveFileListCopy.get(), allowedTypesCopy.get());
        return ThrowOrReturnJson(env, result);
    }

    Value ModInstaller::Install(const CallbackInfo &info)
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);

        const auto env = info.Env();
        const auto files = JSONStringify(info[0].As<Object>());
        const auto stopPatterns = JSONStringify(info[1].As<Object>());
        const auto pluginPath = info[2].As<String>();
        const auto scriptPath = info[3].As<String>();
        const auto presetRaw = info[4].As<Object>();
        const auto validate = info[5].As<Boolean>();

        const auto filesCopy = CopyWithFree(files.Utf16Value());
        const auto stopPatternsCopy = CopyWithFree(stopPatterns.Utf16Value());
        const auto pluginPathCopy = CopyWithFree(pluginPath.Utf16Value());
        const auto scriptPathCopy = CopyWithFree(scriptPath.Utf16Value());
        const auto presetCopy = presetRaw.IsUndefined() || presetRaw.IsNull() ? NullStringCopy() : CopyWithFree(JSONStringify(presetRaw));
        const auto validateCopy = validate.Value() ? (uint8_t)1 : (uint8_t)0;

        auto cbData = CreateResultCallbackData(env, NAMEOFWITHCALLBACK(functionName, HandleJsonResultCallback));
        const auto deferred = cbData->deferred;
        const auto tsfn = cbData->tsfn;

        const auto result = install(
            this->_pInstance,
            filesCopy.get(),
            stopPatternsCopy.get(),
            pluginPathCopy.get(),
            scriptPathCopy.get(),
            presetCopy.get(),
            validateCopy,
            cbData,
            HandleJsonResultCallback);
        return ReturnAndHandleReject(env, result, deferred, tsfn);
    }
}
#endif
