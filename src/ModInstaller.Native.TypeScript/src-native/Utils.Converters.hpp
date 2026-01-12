#ifndef LIB_UTILS_CONVERTERS_GUARD_HPP_
#define LIB_UTILS_CONVERTERS_GUARD_HPP_

#include <napi.h>
#include "Logger.hpp"
#include "Utils.Generic.hpp"
#include "Utils.JS.hpp"
#include "Utils.Utf.hpp"

using namespace Napi;

namespace Utils
{
    inline return_value_string *ConvertToStringResult(const Napi::Value &result)
    {
        if (result.IsNull())
        {
            Logger::Log(__FUNCTION__, "Value: NULL");
            return Create(return_value_string{nullptr, nullptr});
        }

        const auto resultStr = result.As<Napi::String>();
#if DEBUG
        Logger::Log(__FUNCTION__, "Value: " + resultStr.Utf8Value());
#endif
        return Create(return_value_string{nullptr, Copy(resultStr.Utf16Value())});
    }

    inline return_value_json *ConvertToJsonResult(const Napi::Value &result)
    {
        if (result.IsNull())
        {
            Logger::Log(__FUNCTION__, "Value: NULL");
            return Create(return_value_json{nullptr, nullptr});
        }

        const auto resultObj = result.As<Object>();
#if DEBUG
        Logger::Log(__FUNCTION__, "Value: " + JSONStringify(resultObj).Utf8Value());
#endif
        return Create(return_value_json{nullptr, Copy(JSONStringify(resultObj).Utf16Value())});
    }

    inline return_value_data *ConvertToDataResult(const Napi::Value &result)
    {
        if (result.IsNull())
        {
            Logger::Log(__FUNCTION__, "Value: NULL");
            return Create(return_value_data{nullptr, nullptr, 0});
        }

        if (!result.IsBuffer())
        {
            Logger::Log(__FUNCTION__, "Value: Not a Buffer<uint8_t>");
            return Create(return_value_data{Copy(Utf8ToUtf16("Not a Buffer<uint8_t>")), nullptr, 0});
        }

        auto buffer = result.As<Buffer<uint8_t>>();
        Logger::Log(__FUNCTION__, "Buffer size: " + std::to_string(buffer.ByteLength()));
        return Create(return_value_data{nullptr, Copy(buffer.Data(), buffer.ByteLength()), static_cast<int>(buffer.ByteLength())});
    }

    inline return_value_void *ConvertToVoidResult(LoggerScope &logger)
    {
        return Create(return_value_void{nullptr});
    }
}
#endif
