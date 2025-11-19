#ifndef VE_MODINSTALLER_CB_GUARD_HPP_
#define VE_MODINSTALLER_CB_GUARD_HPP_

#include <mutex>
#include <condition_variable>
#include <thread>
#include "ModInstaller.Native.h"
#include "Logger.hpp"
#include "Utils.Callbacks.hpp"
#include "Utils.Converters.hpp"
#include "Bindings.ModInstaller.hpp"

using namespace Napi;
using namespace Utils;
using namespace ModInstaller::Native;

namespace Bindings::ModInstaller
{
    static return_value_json *pluginsGetAll(param_ptr *p_owner,
                                            param_bool active_only) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName, active_only);
        try
        {
            auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

            if (std::this_thread::get_id() == manager->MainThreadId)
            {
                const auto env = manager->FPluginsGetAll.Env();

                const auto activeOnly = Boolean::New(env, active_only != 0);
                const auto jsResult = manager->FPluginsGetAll({activeOnly});

                return ConvertToJsonResult(jsResult);
            }
            else
            {
                // The C# async function called from a non-main JS thread
                // So we need to use the ThreadSafeFunction to marshal the call to the main JS thread
                // and wait for the result synchronously

                std::mutex mtx;
                std::condition_variable cv;
                bool completed = false;
                return_value_json *result = nullptr;

                const auto callback = [functionName, manager, active_only, &result, &mtx, &cv, &completed](Napi::Env env, Napi::Function jsCallback)
                {
                    LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                    try
                    {
                        const auto activeOnly = Boolean::New(env, active_only != 0);

                        const auto jsResult = jsCallback({activeOnly});

                        std::lock_guard<std::mutex> lock(mtx);
                        result = ConvertToJsonResult(jsResult);
                        completed = true;
                        cv.notify_one();
                    }
                    catch (const Napi::Error &e)
                    {
                        callbackLogger.LogError(e);
                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_json{Copy(GetErrorMessage(e)), nullptr});
                        completed = true;
                        cv.notify_one();
                    }
                };

                const auto status = manager->TSFNPluginsGetAll.BlockingCall(callback);
                if (status != napi_ok)
                {
                    logger.Log("BlockingCall failed with status: " + std::to_string(status));
                    return Create(return_value_json{Copy(u"Failed to queue async call"), nullptr});
                }

                std::unique_lock<std::mutex> lock(mtx);
                cv.wait(lock, [&completed]
                        { return completed; });

                logger.Log("Blocking call completed");
                return result;
            }
        }
        catch (const Napi::Error &e)
        {
            logger.LogError(e);
            return Create(return_value_json{Copy(GetErrorMessage(e)), nullptr});
        }
        catch (const std::exception &e)
        {
            logger.LogException(e);
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_json{Copy(conv.from_bytes(e.what())), nullptr});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_json{Copy(u"Unknown exception"), nullptr});
        }
    }

    static return_value_string *contextGetAppVersion(param_ptr *p_owner) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        try
        {
            auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

            if (std::this_thread::get_id() == manager->MainThreadId)
            {
                const auto env = manager->FContextGetAppVersion.Env();
                const auto jsResult = manager->FContextGetAppVersion({});
                return ConvertToStringResult(jsResult);
            }
            else
            {
                // The C# async function called from a non-main JS thread
                // So we need to use the ThreadSafeFunction to marshal the call to the main JS thread
                // and wait for the result synchronously

                std::mutex mtx;
                std::condition_variable cv;
                bool completed = false;
                return_value_string *result = nullptr;

                const auto callback = [functionName, manager, &result, &mtx, &cv, &completed](Napi::Env env, Napi::Function jsCallback)
                {
                    LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                    try
                    {
                        const auto jsResult = jsCallback({});

                        std::lock_guard<std::mutex> lock(mtx);
                        result = ConvertToStringResult(jsResult);
                        completed = true;
                        cv.notify_one();
                    }
                    catch (const Napi::Error &e)
                    {
                        callbackLogger.LogError(e);
                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
                        completed = true;
                        cv.notify_one();
                    }
                };

                const auto status = manager->TSFNContextGetAppVersion.BlockingCall(callback);
                if (status != napi_ok)
                {
                    logger.Log("BlockingCall failed with status: " + std::to_string(status));
                    return Create(return_value_string{Copy(u"Failed to queue async call"), nullptr});
                }

                std::unique_lock<std::mutex> lock(mtx);
                cv.wait(lock, [&completed]
                        { return completed; });

                logger.Log("Blocking call completed");
                return result;
            }
        }
        catch (const Napi::Error &e)
        {
            logger.LogError(e);
            return Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
        }
        catch (const std::exception &e)
        {
            logger.LogException(e);
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_string{Copy(conv.from_bytes(e.what())), nullptr});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_string{Copy(u"Unknown exception"), nullptr});
        }
    }

    static return_value_string *contextGetCurrentGameVersion(param_ptr *p_owner) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        try
        {
            auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

            if (std::this_thread::get_id() == manager->MainThreadId)
            {
                const auto env = manager->FContextGetCurrentGameVersion.Env();
                const auto jsResult = manager->FContextGetCurrentGameVersion({});
                return ConvertToStringResult(jsResult);
            }
            else
            {
                // The C# async function called from a non-main JS thread
                // So we need to use the ThreadSafeFunction to marshal the call to the main JS thread
                // and wait for the result synchronously

                std::mutex mtx;
                std::condition_variable cv;
                bool completed = false;
                return_value_string *result = nullptr;

                const auto callback = [functionName, manager, &result, &mtx, &cv, &completed](Napi::Env env, Napi::Function jsCallback)
                {
                    LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                    try
                    {
                        const auto jsResult = jsCallback({});

                        std::lock_guard<std::mutex> lock(mtx);
                        result = ConvertToStringResult(jsResult);
                        completed = true;
                        cv.notify_one();
                    }
                    catch (const Napi::Error &e)
                    {
                        callbackLogger.LogError(e);
                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
                        completed = true;
                        cv.notify_one();
                    }
                };

                const auto status = manager->TSFNContextGetCurrentGameVersion.BlockingCall(callback);
                if (status != napi_ok)
                {
                    logger.Log("BlockingCall failed with status: " + std::to_string(status));
                    return Create(return_value_string{Copy(u"Failed to queue async call"), nullptr});
                }

                std::unique_lock<std::mutex> lock(mtx);
                cv.wait(lock, [&completed]
                        { return completed; });

                logger.Log("Blocking call completed");
                return result;
            }
        }
        catch (const Napi::Error &e)
        {
            logger.LogError(e);
            return Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
        }
        catch (const std::exception &e)
        {
            logger.LogException(e);
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_string{Copy(conv.from_bytes(e.what())), nullptr});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_string{Copy(u"Unknown exception"), nullptr});
        }
    }

    static return_value_string *contextGetExtenderVersion(param_ptr *p_owner,
                                                          param_string *p_extender) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        try
        {
            auto manager = const_cast<Bindings::ModInstaller::ModInstaller *>(static_cast<const Bindings::ModInstaller::ModInstaller *>(p_owner));

            if (std::this_thread::get_id() == manager->MainThreadId)
            {
                const auto env = manager->FContextGetExtenderVersion.Env();
                const auto extender = p_extender == nullptr ? env.Null() : String::New(env, p_extender);
                const auto jsResult = manager->FContextGetExtenderVersion({extender});
                return ConvertToStringResult(jsResult);
            }
            else
            {
                // The C# async function called from a non-main JS thread
                // So we need to use the ThreadSafeFunction to marshal the call to the main JS thread
                // and wait for the result synchronously

                std::mutex mtx;
                std::condition_variable cv;
                bool completed = false;
                return_value_string *result = nullptr;

                const auto callback = [functionName, manager, p_extender, &result, &mtx, &cv, &completed](Napi::Env env, Napi::Function jsCallback)
                {
                    LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                    try
                    {
                        const auto extender = p_extender == nullptr ? env.Null() : String::New(env, p_extender);
                        const auto jsResult = jsCallback({extender});

                        std::lock_guard<std::mutex> lock(mtx);
                        result = ConvertToStringResult(jsResult);
                        completed = true;
                        cv.notify_one();
                    }
                    catch (const Napi::Error &e)
                    {
                        callbackLogger.LogError(e);
                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
                        completed = true;
                        cv.notify_one();
                    }
                };

                const auto status = manager->TSFNContextGetExtenderVersion.BlockingCall(callback);
                if (status != napi_ok)
                {
                    logger.Log("BlockingCall failed with status: " + std::to_string(status));
                    return Create(return_value_string{Copy(u"Failed to queue async call"), nullptr});
                }

                std::unique_lock<std::mutex> lock(mtx);
                cv.wait(lock, [&completed]
                        { return completed; });

                logger.Log("Blocking call completed");
                return result;
            }
        }
        catch (const Napi::Error &e)
        {
            logger.LogError(e);
            return Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
        }
        catch (const std::exception &e)
        {
            logger.LogException(e);
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_string{Copy(conv.from_bytes(e.what())), nullptr});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_string{Copy(u"Unknown exception"), nullptr});
        }
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

            // Define callback lambdas that will be used by the JavaScript functions
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
                }
                catch (const std::exception &e)
                {
                    selectLogger.LogException(e);
                    std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
                    auto errorResult = Create(return_value_void{Copy(conv.from_bytes(e.what()))});
                    p_select_callback(p_callback_handler, 0, 0, nullptr, errorResult);
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
                }
                catch (const std::exception &e)
                {
                    contLogger.LogException(e);
                    std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
                    auto errorResult = Create(return_value_void{Copy(conv.from_bytes(e.what()))});
                    p_const_callback(p_callback_handler, 0, 0, errorResult);
                }
            };
            const auto cancelCallback = [functionName, p_callback_handler, p_cancel_callback](const CallbackInfo &info)
            {
                LoggerScope cancelLogger(NAMEOFWITHCALLBACK(functionName, cancelCallback));
                try
                {
                    auto result = Create(return_value_void{nullptr});
                    p_cancel_callback(p_callback_handler, result);
                }
                catch (const std::exception &e)
                {
                    cancelLogger.LogException(e);
                    std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
                    auto errorResult = Create(return_value_void{Copy(conv.from_bytes(e.what()))});
                    p_cancel_callback(p_callback_handler, errorResult);
                }
            };

            if (std::this_thread::get_id() == manager->MainThreadId)
            {
                const auto env = manager->FUIStartDialog.Env();
                const auto moduleName = p_module_name == nullptr ? env.Null() : String::New(env, p_module_name);
                const auto image = p_image == nullptr ? env.Null() : JSONParse(Napi::String::New(env, p_image));
                const auto selectFunction = Function::New(env, selectCallback, NAMEOF(selectCallback));
                const auto constFunction = Function::New(env, constCallback, NAMEOF(constCallback));
                const auto cancelFunction = Function::New(env, cancelCallback, NAMEOF(cancelCallback));

                manager->FUIStartDialog({moduleName, image, selectFunction, constFunction, cancelFunction});
                return Create(return_value_void{nullptr});
            }
            else
            {
                // The C# async function called from a non-main JS thread
                // So we need to use the ThreadSafeFunction to marshal the call to the main JS thread
                // and wait for the result synchronously

                std::mutex mtx;
                std::condition_variable cv;
                bool completed = false;
                return_value_void *result = nullptr;

                const auto callback = [functionName, manager, p_module_name, p_image, selectCallback, constCallback, cancelCallback, &result, &mtx, &cv, &completed](Napi::Env env, Napi::Function jsCallback)
                {
                    LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                    try
                    {
                        const auto moduleName = p_module_name == nullptr ? env.Null() : String::New(env, p_module_name);
                        const auto image = p_image == nullptr ? env.Null() : JSONParse(Napi::String::New(env, p_image));
                        const auto selectFunction = Function::New(env, selectCallback, NAMEOF(selectCallback));
                        const auto constFunction = Function::New(env, constCallback, NAMEOF(constCallback));
                        const auto cancelFunction = Function::New(env, cancelCallback, NAMEOF(cancelCallback));

                        jsCallback({moduleName, image, selectFunction, constFunction, cancelFunction});

                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_void{nullptr});
                        completed = true;
                        cv.notify_one();
                    }
                    catch (const Napi::Error &e)
                    {
                        callbackLogger.LogError(e);
                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_void{Copy(GetErrorMessage(e))});
                        completed = true;
                        cv.notify_one();
                    }
                };

                const auto status = manager->TSFNUIStartDialog.BlockingCall(callback);
                if (status != napi_ok)
                {
                    logger.Log("BlockingCall failed with status: " + std::to_string(status));
                    return Create(return_value_void{Copy(u"Failed to queue async call")});
                }

                std::unique_lock<std::mutex> lock(mtx);
                cv.wait(lock, [&completed]
                        { return completed; });

                logger.Log("Blocking call completed");
                return result;
            }
        }
        catch (const Napi::Error &e)
        {
            logger.LogError(e);
            return Create(return_value_void{Copy(GetErrorMessage(e))});
        }
        catch (const std::exception &e)
        {
            logger.LogException(e);
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

            if (std::this_thread::get_id() == manager->MainThreadId)
            {
                const auto env = manager->FUIEndDialog.Env();
                manager->FUIEndDialog({});
                return Create(return_value_void{nullptr});
            }
            else
            {
                // The C# async function called from a non-main JS thread
                // So we need to use the ThreadSafeFunction to marshal the call to the main JS thread
                // and wait for the result synchronously

                std::mutex mtx;
                std::condition_variable cv;
                bool completed = false;
                return_value_void *result = nullptr;

                const auto callback = [functionName, manager, &result, &mtx, &cv, &completed](Napi::Env env, Napi::Function jsCallback)
                {
                    LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                    try
                    {
                        jsCallback({});

                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_void{nullptr});
                        completed = true;
                        cv.notify_one();
                    }
                    catch (const Napi::Error &e)
                    {
                        callbackLogger.LogError(e);
                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_void{Copy(GetErrorMessage(e))});
                        completed = true;
                        cv.notify_one();
                    }
                };

                const auto status = manager->TSFNUIEndDialog.BlockingCall(callback);
                if (status != napi_ok)
                {
                    logger.Log("BlockingCall failed with status: " + std::to_string(status));
                    return Create(return_value_void{Copy(u"Failed to queue async call")});
                }

                std::unique_lock<std::mutex> lock(mtx);
                cv.wait(lock, [&completed]
                        { return completed; });

                logger.Log("Blocking call completed");
                return result;
            }
        }
        catch (const Napi::Error &e)
        {
            logger.LogError(e);
            return Create(return_value_void{Copy(GetErrorMessage(e))});
        }
        catch (const std::exception &e)
        {
            logger.LogException(e);
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

            if (std::this_thread::get_id() == manager->MainThreadId)
            {
                const auto env = manager->FUIUpdateState.Env();
                const auto installSteps = p_install_steps == nullptr ? env.Null() : JSONParse(Napi::String::New(env, p_install_steps));
                const auto stepNumber = Number::New(env, current_step);
                manager->FUIUpdateState({installSteps, stepNumber});
                return Create(return_value_void{nullptr});
            }
            else
            {
                // The C# async function called from a non-main JS thread
                // So we need to use the ThreadSafeFunction to marshal the call to the main JS thread
                // and wait for the result synchronously

                std::mutex mtx;
                std::condition_variable cv;
                bool completed = false;
                return_value_void *result = nullptr;

                const auto callback = [functionName, manager, p_install_steps, current_step, &result, &mtx, &cv, &completed](Napi::Env env, Napi::Function jsCallback)
                {
                    LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                    try
                    {
                        const auto installSteps = p_install_steps == nullptr ? env.Null() : JSONParse(Napi::String::New(env, p_install_steps));
                        const auto stepNumber = Number::New(env, current_step);

                        jsCallback({installSteps, stepNumber});

                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_void{nullptr});
                        completed = true;
                        cv.notify_one();
                    }
                    catch (const Napi::Error &e)
                    {
                        callbackLogger.LogError(e);
                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_void{Copy(GetErrorMessage(e))});
                        completed = true;
                        cv.notify_one();
                    }
                };

                const auto status = manager->TSFNUIUpdateState.BlockingCall(callback);
                if (status != napi_ok)
                {
                    logger.Log("BlockingCall failed with status: " + std::to_string(status));
                    return Create(return_value_void{Copy(u"Failed to queue async call")});
                }

                std::unique_lock<std::mutex> lock(mtx);
                cv.wait(lock, [&completed]
                        { return completed; });

                logger.Log("Blocking call completed");
                return result;
            }
        }
        catch (const Napi::Error &e)
        {
            logger.LogError(e);
            return Create(return_value_void{Copy(GetErrorMessage(e))});
        }
        catch (const std::exception &e)
        {
            logger.LogException(e);
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_void{Copy(conv.from_bytes(e.what()))});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_void{Copy(u"Unknown exception")});
        }
    }
}
#endif
