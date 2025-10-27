#ifndef LIB_UTILS_CALLBACK_GUARD_HPP_
#define LIB_UTILS_CALLBACK_GUARD_HPP_

// Define this before any Windows headers to prevent winsock.h conflicts
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <napi.h>
#include <uv.h>
#include "logger.hpp"
#include "utils.hpp"

using namespace Napi;
using namespace Utils;

namespace Utils::Callback
{
    template <typename T>
    struct CallbackData
    {
        T *result;
        std::atomic<bool> completed{false};
    };

    // Helper to signal callback completion
    template <typename T>
    inline void SignalCompletion(CallbackData<T> *data)
    {
        data->completed.store(true, std::memory_order_release);
    }

    // Helper to wait for callback completion while processing events
    template <typename T>
    inline void WaitForCompletion(CallbackData<T> &data)
    {
        // Get the libuv event loop
        uv_loop_t* loop = uv_default_loop();

        // Process events while waiting for callback to complete
        while (!data.completed.load(std::memory_order_acquire))
        {
            // Process pending events without blocking
            // This keeps the UI responsive!
            uv_run(loop, UV_RUN_NOWAIT);

            // Small yield to prevent busy-waiting
            std::this_thread::sleep_for(std::chrono::milliseconds(1));
        }
    }

    inline return_value_string *CreateStringError(const std::u16string &msg)
    {
        return Create(return_value_string{Copy(msg), nullptr});
    }

    inline return_value_json *CreateJsonError(const std::u16string &msg)
    {
        return Create(return_value_json{Copy(msg), nullptr});
    }

    inline return_value_data *CreateDataError(const std::u16string &msg)
    {
        return Create(return_value_data{Copy(msg), nullptr, 0});
    }

    template <typename TReturn, typename TCallback>
    inline TReturn *ExecuteBlockingCallbackWithSignal(
        LoggerScope &logger,
        const Napi::ThreadSafeFunction &tsfn,
        const TCallback callback,
        TReturn *(*createErrorResult)(const std::u16string &))
    {
        try
        {
            logger.Log("Starting callback");
            CallbackData<TReturn> data;

            // Queue the callback on the Node.js thread
            tsfn.BlockingCall(&data, callback);

            logger.Log("Waiting for completion");

            // CHANGED: Process events instead of blocking with mutex
            WaitForCompletion(data);

            logger.Log("Completed");
            return data.result;
        }
        catch (const std::exception &e)
        {
            logger.Log("Exception: " + std::string(e.what()));
            return createErrorResult(std::u16string(u"C++ exception"));
        }
    }

    // Common callback handler for string results
    template <typename TLogger>
    inline void HandleCallbackStringResultWithSignal(TLogger &logger, Napi::Env env, const Napi::Value &result, CallbackData<return_value_string> *data)
    {
        try
        {
            if (result.IsNull())
            {
                logger.Log("Value: NULL");
                data->result = Create(return_value_string{nullptr, nullptr});
            }
            else
            {
                const auto resultStr = result.As<Napi::String>();
                logger.Log("Value: " + resultStr.Utf8Value());
                data->result = Create(return_value_string{nullptr, Copy(resultStr.Utf16Value())});
            }
        }
        catch (const Napi::Error &e)
        {
            logger.Log("Error: " + std::string(e.Message()));
            data->result = Create(return_value_string{Copy(GetErrorMessage(e)), nullptr});
        }
        SignalCompletion(data);
    }

    template <typename TLogger>
    inline void HandleCallbackJsonResultWithSignal(TLogger &logger, Napi::Env env, const Napi::Value &result, CallbackData<return_value_json> *data)
    {
        try
        {
            if (result.IsNull())
            {
                logger.Log("Value: NULL");
                data->result = Create(return_value_json{nullptr, nullptr});
            }
            else
            {
                const auto resultObj = result.As<Object>();
                logger.Log("Value: " + JSONStringify(resultObj).Utf8Value());
                data->result = Create(return_value_json{nullptr, Copy(JSONStringify(resultObj).Utf16Value())});
            }
        }
        catch (const Napi::Error &e)
        {
            logger.Log("Error: " + std::string(e.Message()));
            data->result = Create(return_value_json{Copy(GetErrorMessage(e)), nullptr});
        }
        SignalCompletion(data);
    }

    template <typename TLogger>
    inline void HandleCallbackDataResultWithSignal(TLogger &logger, Napi::Env env, const Napi::Value &result, CallbackData<return_value_data> *data)
    {
        try
        {
            logger.Log("Processing data callback");
            if (result.IsNull())
            {
                logger.Log("Value: NULL");
                data->result = Create(return_value_data{nullptr, nullptr, 0});
            }
            else if (!result.IsBuffer())
            {
                logger.Log("Value: Not a Buffer<uint8_t>");
                std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
                data->result = Create(return_value_data{Copy(conv.from_bytes("Not a Buffer<uint8_t>")), nullptr, 0});
            }
            else
            {
                logger.Log("Processing buffer");
                auto buffer = result.As<Buffer<uint8_t>>();
                data->result = Create(return_value_data{nullptr, Copy(buffer.Data(), buffer.ByteLength()), static_cast<int>(buffer.ByteLength())});
            }
        }
        catch (const Napi::Error &e)
        {
            logger.Log("Error: " + std::string(e.Message()));
            data->result = Create(return_value_data{Copy(GetErrorMessage(e)), nullptr, 0});
        }
        SignalCompletion(data);
    }
}

#endif
