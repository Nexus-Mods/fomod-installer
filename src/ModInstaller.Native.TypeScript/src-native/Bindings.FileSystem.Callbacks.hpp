#ifndef VE_FILESYSTEM_CB_GUARD_HPP_
#define VE_FILESYSTEM_CB_GUARD_HPP_

#include "utils.hpp"
#include "Utils.Callback.hpp"
#include "Utils.Async.hpp"
#include "ModInstaller.Native.h"
#include "Bindings.FileSystem.hpp"
#include <codecvt>
#include <mutex>
#include <condition_variable>

using namespace Napi;
using namespace Utils;
using namespace Utils::Async;
using namespace Utils::Callback;
using namespace ModInstaller::Native;

namespace Bindings::FileSystem
{
    static return_value_data *readFileContent(param_ptr *p_owner,
                                              param_string *p_file_path,
                                              param_int v_offset,
                                              param_int v_length) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        try
        {
            auto manager = const_cast<Bindings::FileSystem::FileSystem *>(static_cast<const Bindings::FileSystem::FileSystem *>(p_owner));

            if (std::this_thread::get_id() == manager->MainThreadId)
            {
                const auto env = manager->FReadFileContent.Env();
                const auto filePath = String::New(env, p_file_path);
                const auto offset = Number::New(env, v_offset);
                const auto length = Number::New(env, v_length);
                const auto jsResult = manager->FReadFileContent({filePath, offset, length});
                return ConvertToDataResult(logger, jsResult);
            }
            else
            {
                // The C# async function called from a non-main JS thread
                // So we need to use the ThreadSafeFunction to marshal the call to the main JS thread
                // and wait for the result synchronously

                std::mutex mtx;
                std::condition_variable cv;
                bool completed = false;
                return_value_data *result = nullptr;

                const auto callback = [functionName, manager, p_file_path, v_offset, v_length, &result, &mtx, &cv, &completed](Napi::Env env, Napi::Function jsCallback)
                {
                    LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                    try
                    {
                        const auto filePath = String::New(env, p_file_path);
                        const auto offset = Number::New(env, v_offset);
                        const auto length = Number::New(env, v_length);
                        const auto jsResult = jsCallback({filePath, offset, length});

                        std::lock_guard<std::mutex> lock(mtx);
                        result = ConvertToDataResult(callbackLogger, jsResult);
                        completed = true;
                        cv.notify_one();
                    }
                    catch (const Napi::Error &e)
                    {
                        callbackLogger.Log("Error: " + std::string(e.Message()));

                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_data{Copy(GetErrorMessage(e)), nullptr, 0});
                        completed = true;
                        cv.notify_one();
                    }
                };

                const auto status = manager->TSFNReadFileContent.BlockingCall(callback);
                if (status != napi_ok)
                {
                    logger.Log("BlockingCall failed with status: " + std::to_string(status));
                    return Create(return_value_data{Copy(u"Failed to queue async call"), nullptr, 0});
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
            logger.Log("Error: " + std::string(e.Message()));
            return Create(return_value_data{Copy(GetErrorMessage(e)), nullptr, 0});
        }
        catch (const std::exception &e)
        {
            logger.Log("Exception: " + std::string(e.what()));
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_data{Copy(conv.from_bytes(e.what())), nullptr, 0});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_data{Copy(u"Unknown exception"), nullptr, 0});
        }
    }
    static return_value_json *readDirectoryFileList(param_ptr *p_owner,
                                                    param_string *p_directory_path,
                                                    param_string *p_pattern,
                                                    param_int search_type) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        try
        {
            auto manager = const_cast<Bindings::FileSystem::FileSystem *>(static_cast<const Bindings::FileSystem::FileSystem *>(p_owner));

            if (std::this_thread::get_id() == manager->MainThreadId)
            {
                const auto env = manager->FReadDirectoryFileList.Env();
                const auto directoryPath = String::New(env, p_directory_path);
                const auto pattern = p_pattern == nullptr ? env.Null() : String::New(env, p_pattern);
                const auto searchType = Number::New(env, search_type);
                const auto jsResult = manager->FReadDirectoryFileList({directoryPath, pattern, searchType});
                return ConvertToJsonResult(logger, jsResult);
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

                const auto callback = [functionName, manager, p_directory_path, p_pattern, search_type, &result, &mtx, &cv, &completed](Napi::Env env, Napi::Function jsCallback)
                {
                    LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                    try
                    {
                        const auto directoryPath = String::New(env, p_directory_path);
                        const auto pattern = p_pattern == nullptr ? env.Null() : String::New(env, p_pattern);
                        const auto searchType = Number::New(env, search_type);
                        const auto jsResult = jsCallback({directoryPath, pattern, searchType});

                        std::lock_guard<std::mutex> lock(mtx);
                        result = ConvertToJsonResult(callbackLogger, jsResult);
                        completed = true;
                        cv.notify_one();
                    }
                    catch (const Napi::Error &e)
                    {
                        callbackLogger.Log("Error: " + std::string(e.Message()));

                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_json{Copy(GetErrorMessage(e)), nullptr});
                        completed = true;
                        cv.notify_one();
                    }
                };

                const auto status = manager->TSFNReadDirectoryFileList.BlockingCall(callback);
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
            logger.Log("Error: " + std::string(e.Message()));
            return Create(return_value_json{Copy(GetErrorMessage(e)), nullptr});
        }
        catch (const std::exception &e)
        {
            logger.Log("Exception: " + std::string(e.what()));
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_json{Copy(conv.from_bytes(e.what())), nullptr});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_json{Copy(u"Unknown exception"), nullptr});
        }
    }
    static return_value_json *readDirectoryList(param_ptr *p_owner,
                                                param_string *p_directory_path) noexcept
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        try
        {
            auto manager = const_cast<Bindings::FileSystem::FileSystem *>(static_cast<const Bindings::FileSystem::FileSystem *>(p_owner));

            if (std::this_thread::get_id() == manager->MainThreadId)
            {
                const auto env = manager->FReadDirectoryList.Env();
                const auto directoryPath = String::New(env, p_directory_path);
                const auto jsResult = manager->FReadDirectoryList({directoryPath});
                return ConvertToJsonResult(logger, jsResult);
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

                const auto callback = [functionName, manager, p_directory_path, &result, &mtx, &cv, &completed](Napi::Env env, Napi::Function jsCallback)
                {
                    LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
                    try
                    {
                        const auto directoryPath = p_directory_path == nullptr ? env.Null() : String::New(env, p_directory_path);
                        const auto jsResult = jsCallback({directoryPath});

                        std::lock_guard<std::mutex> lock(mtx);
                        result = ConvertToJsonResult(callbackLogger, jsResult);
                        completed = true;
                        cv.notify_one();
                    }
                    catch (const Napi::Error &e)
                    {
                        callbackLogger.Log("Error: " + std::string(e.Message()));

                        std::lock_guard<std::mutex> lock(mtx);
                        result = Create(return_value_json{Copy(GetErrorMessage(e)), nullptr});
                        completed = true;
                        cv.notify_one();
                    }
                };

                const auto status = manager->TSFNReadDirectoryList.BlockingCall(callback);
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
            logger.Log("Error: " + std::string(e.Message()));
            return Create(return_value_json{Copy(GetErrorMessage(e)), nullptr});
        }
        catch (const std::exception &e)
        {
            logger.Log("Exception: " + std::string(e.what()));
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_json{Copy(conv.from_bytes(e.what())), nullptr});
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            return Create(return_value_json{Copy(u"Unknown exception"), nullptr});
        }
    }
}
#endif
