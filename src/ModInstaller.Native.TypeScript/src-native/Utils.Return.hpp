#ifndef VE_LIB_UTILS_RETURN_GUARD_HPP_
#define VE_LIB_UTILS_RETURN_GUARD_HPP_

#include <napi.h>
#include "ModInstaller.Native.h"
#include "Logger.hpp"
#include "Utils.Generic.hpp"
#include "Utils.Converters.hpp"

using namespace Napi;
using namespace ModInstaller::Native;

namespace Utils
{
    Napi::Value ReturnAndHandleReject(const Env env, return_value_async *const result, const Napi::Promise::Deferred deferred, Napi::ThreadSafeFunction tsfn)
    {
        const del_async del{result};

        if (result == nullptr)
        {
            Logger::Log(__FUNCTION__, "Null result");
            NAPI_THROW(Error::New(env, String::New(env, "Return value was null!")));
        }

        if (result->error != nullptr)
        {
            const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
            deferred.Reject(Error::New(env, String::New(env, error.get())).Value());
            tsfn.Release();
        }

        return deferred.Promise();
    }

    void ThrowOrReturn(const Env env, return_value_void *const val)
    {
        const del_void del{val};

        if (val == nullptr)
        {
            Logger::Log(__FUNCTION__, "Null result");
            NAPI_THROW(Error::New(env, String::New(env, "Return value was null!")));
        }

        if (val->error == nullptr)
        {
            return;
        }

        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(val->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }

    Value ThrowOrReturnString(const Env env, return_value_string *const result)
    {
        const del_string del{result};

        if (result == nullptr)
        {
            Logger::Log(__FUNCTION__, "Null result");
            NAPI_THROW(Error::New(env, String::New(env, "Return value was null!")));
        }

        if (result->error == nullptr)
        {
            if (result->value == nullptr)
            {
                Logger::Log(__FUNCTION__, "Null error and value");
                NAPI_THROW(Error::New(env, String::New(env, "Return value was null!")));
            }

            const auto value = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->value);
            return String::New(env, result->value);
        }

        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }

    Value ThrowOrReturnJson(const Env env, return_value_json *const result)
    {
        const del_json del{result};

        if (result == nullptr)
        {
            Logger::Log(__FUNCTION__, "Null result");
            NAPI_THROW(Error::New(env, String::New(env, "Return value was null!")));
        }

        if (result->error == nullptr)
        {
            if (result->value == nullptr)
            {
                Logger::Log(__FUNCTION__, "Null error and value");
                NAPI_THROW(Error::New(env, String::New(env, "Return value was null!")));
            }

            const auto value = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->value);
            return JSONParse(String::New(env, result->value));
        }

        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }

    Value ThrowOrReturnBoolean(const Env env, return_value_bool *const result)
    {
        const del_bool del{result};

        if (result == nullptr)
        {
            Logger::Log(__FUNCTION__, "Null result");
            NAPI_THROW(Error::New(env, String::New(env, "Return value was null!")));
        }

        if (result->error == nullptr)
        {
            return Boolean::New(env, result->value);
        }

        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }

    Value ThrowOrReturnInt32(const Env env, return_value_int32 *const result)
    {
        const del_int32 del{result};

        if (result == nullptr)
        {
            Logger::Log(__FUNCTION__, "Null result");
            NAPI_THROW(Error::New(env, String::New(env, "Return value was null!")));
        }

        if (result->error == nullptr)
        {
            return Number::New(env, result->value);
        }

        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }

    Value ThrowOrReturnUInt32(const Env env, return_value_uint32 *const result)
    {
        const del_uint32 del{result};

        if (result == nullptr)
        {
            Logger::Log(__FUNCTION__, "Null result");
            NAPI_THROW(Error::New(env, String::New(env, "Return value was null!")));
        }

        if (result->error == nullptr)
        {
            return Number::New(env, result->value);
        }

        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }

    void *ThrowOrReturnPtr(const Env env, return_value_ptr *const result)
    {
        const del_ptr del{result};

        if (result == nullptr)
        {
            Logger::Log(__FUNCTION__, "Null result");
            NAPI_THROW(Error::New(env, String::New(env, "Return value was null!")));
        }

        if (result->error == nullptr)
        {
            return result->value;
        }
        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }
}
#endif