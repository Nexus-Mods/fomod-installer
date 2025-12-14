#ifndef VE_COMMON_IMPL_GUARD_HPP_
#define VE_COMMON_IMPL_GUARD_HPP_

#include <napi.h>
#include "ModInstaller.Native.h"
#include "Logger.hpp"
#include "Utils.Return.hpp"
#include "Bindings.Common.hpp"

using namespace Napi;
using namespace Utils;
using namespace ModInstaller::Native;

namespace Bindings::Common
{
    Value AllocWithOwnership(const CallbackInfo &info)
    {
        LoggerScope logger(__FUNCTION__);
        const auto env = info.Env();

        return WithExceptionHandlingReturningNull(logger, env, [&]()
                                                  {
            const auto length = info[0].As<Number>();

#ifndef NODE_API_NO_EXTERNAL_BUFFERS_ALLOWED
            const auto result = common_alloc(length.Int32Value());
            const auto buffer = Buffer<uint8_t>::New(env, reinterpret_cast<uint8_t *>(result), length.Int32Value(), [](Env, void *data)
                                                     { common_dealloc(data); });
            return buffer;
#else
            return env.Null();
#endif
        });
    }

    Value AllocWithoutOwnership(const CallbackInfo &info)
    {
        LoggerScope logger(__FUNCTION__);
        const auto env = info.Env();

        return WithExceptionHandlingReturningNull(logger, env, [&]()
                                                  {
            const auto length = info[0].As<Number>();

#ifndef NODE_API_NO_EXTERNAL_BUFFERS_ALLOWED
            const auto result = common_alloc(length.Int32Value());
            const auto buffer = Buffer<uint8_t>::New(env, reinterpret_cast<uint8_t *>(result), length.Int32Value());
            buffer.Set("FOMODSkipCopy", Boolean::New(env, true));
            return buffer;
#else
            return env.Null();
#endif
        });
    }

    Value AllocAliveCount(const CallbackInfo &info)
    {
        LoggerScope logger(__FUNCTION__);
        const auto env = info.Env();

        return WithExceptionHandlingReturningNull(logger, env, [&]()
                                                  {
            const auto result = common_alloc_alive_count();
            return Number::New(env, result);
        });
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
