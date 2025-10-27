#ifndef LIB_UTILS_ASYNC_GUARD_HPP_
#define LIB_UTILS_ASYNC_GUARD_HPP_

#include <napi.h>
#include "logger.hpp"
#include "utils.hpp"

using namespace Napi;
using namespace Utils;

namespace Utils::Async
{
    // Generic callback context for async operations
    template <typename TCallback>
    struct AsyncCallbackContext
    {
        param_ptr *callback_handler;
        TCallback callback;
    };

    // Helper to convert Napi::Value to return_value_string
    inline return_value_string *ConvertToStringResult(LoggerScope &logger, const Napi::Value &result)
    {
        if (result.IsNull())
        {
            logger.Log("Value: NULL");
            return Create(return_value_string{nullptr, nullptr});
        }

        const auto resultStr = result.As<Napi::String>();
        logger.Log("Value: " + resultStr.Utf8Value());
        return Create(return_value_string{nullptr, Copy(resultStr.Utf16Value())});
    }

    // Helper to convert Napi::Value to return_value_json
    inline return_value_json *ConvertToJsonResult(LoggerScope &logger, const Napi::Value &result)
    {
        if (result.IsNull())
        {
            logger.Log("Value: NULL");
            return Create(return_value_json{nullptr, nullptr});
        }

        const auto resultObj = result.As<Object>();
        logger.Log("Value: " + JSONStringify(resultObj).Utf8Value());
        return Create(return_value_json{nullptr, Copy(JSONStringify(resultObj).Utf16Value())});
    }

    // Helper to convert Napi::Value to return_value_data
    inline return_value_data *ConvertToDataResult(LoggerScope &logger, const Napi::Value &result)
    {
        if (result.IsNull())
        {
            logger.Log("Value: NULL");
            return Create(return_value_data{nullptr, nullptr, 0});
        }

        if (!result.IsBuffer())
        {
            logger.Log("Value: Not a Buffer<uint8_t>");
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
            return Create(return_value_data{Copy(conv.from_bytes("Not a Buffer<uint8_t>")), nullptr, 0});
        }

        auto buffer = result.As<Buffer<uint8_t>>();
        logger.Log("Buffer size: " + std::to_string(buffer.ByteLength()));
        return Create(return_value_data{nullptr, Copy(buffer.Data(), buffer.ByteLength()),
                                        static_cast<int>(buffer.ByteLength())});
    }

    // Helper to convert Napi::Value to return_value_void
    inline return_value_void *ConvertToVoidResult(LoggerScope &logger)
    {
        return Create(return_value_void{nullptr});
    }
}

#endif
