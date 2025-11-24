#ifndef VE_COMMON_GUARD_HPP_
#define VE_COMMON_GUARD_HPP_

#include "Platform.hpp"
#include <napi.h>
#include <codecvt>
#include <locale>
#include "ModInstaller.Native.h"
#include "Logger.hpp"

using namespace Napi;
using namespace ModInstaller::Native;

namespace Bindings::Common
{
    Value AllocWithOwnership(const CallbackInfo &info)
    {
        LoggerScope logger(__FUNCTION__);

        try
        {
            const auto env = info.Env();
            const auto length = info[0].As<Number>();

#ifndef NODE_API_NO_EXTERNAL_BUFFERS_ALLOWED
            const auto result = common_alloc(length.Int32Value());
            const auto buffer = Buffer<uint8_t>::New(env, reinterpret_cast<uint8_t *>(result), length.Int32Value(), [](Env, void *data)
                                                     { common_dealloc(data); });
            return buffer;
#else
            return env.Null();
#endif
        }
        catch (const Napi::Error &e)
        {
            logger.LogError(e);
            const auto env = info.Env();
            return env.Null();
        }
        catch (const std::exception &e)
        {
            logger.LogException(e);
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;

            const auto env = info.Env();
            return env.Null();
        }
        catch (...)
        {
            logger.Log("Unknown exception");
            const auto env = info.Env();
            return env.Null();
        }
    }

    Value AllocWithoutOwnership(const CallbackInfo &info)
    {
        LoggerScope logger(__FUNCTION__);

        try
        {
            const auto env = info.Env();
            const auto length = info[0].As<Number>();

#ifndef NODE_API_NO_EXTERNAL_BUFFERS_ALLOWED
            const auto result = common_alloc(length.Int32Value());
            const auto buffer = Buffer<uint8_t>::New(env, reinterpret_cast<uint8_t *>(result), length.Int32Value());
            buffer.Set("FOMODSkipCopy", Boolean::New(env, true));
            return buffer;
#else
            return env.Null();
#endif
        }
        catch (const Napi::Error &e)
        {
            logger.LogError(e);
            const auto env = info.Env();
            return env.Null();
        }
        catch (const std::exception &e)
        {
            logger.LogException(e);
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;

            const auto env = info.Env();
            return env.Null();
        }
        catch (...)
        {
            logger.Log("Unknown exception");

            const auto env = info.Env();
            return env.Null();
        }
    }

    Value AllocAliveCount(const CallbackInfo &info)
    {
        LoggerScope logger(__FUNCTION__);

        try
        {
            const auto env = info.Env();

            const auto result = common_alloc_alive_count();
            return Number::New(env, result);
        }
        catch (const Napi::Error &e)
        {
            logger.LogError(e);
            const auto env = info.Env();
            return env.Null();
        }
        catch (const std::exception &e)
        {
            logger.LogException(e);
            std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;

            const auto env = info.Env();
            return env.Null();
        }
        catch (...)
        {
            logger.Log("Unknown exception");

            const auto env = info.Env();
            return env.Null();
        }
    }

    Object Init(const Env env, Object exports)
    {
        exports.Set("allocWithOwnership", Function::New(env, AllocWithOwnership));
        exports.Set("allocWithoutOwnership", Function::New(env, AllocWithoutOwnership));
        exports.Set("allocAliveCount", Function::New(env, AllocAliveCount));

        return exports;
    }
}
#endif
