#ifndef VE_LIB_UTILS_RETURN_GUARD_HPP_
#define VE_LIB_UTILS_RETURN_GUARD_HPP_

#include <napi.h>
#include "Logger.hpp"
#include "Utils.Generic.hpp"
#include "Utils.Converters.hpp"

using namespace Napi;

namespace Utils
{
    // Type traits for return value handling
    template <typename T>
    struct ReturnValueTraits;

    template <>
    struct ReturnValueTraits<return_value_void>
    {
        using deleter_type = del_void;
        static constexpr bool has_value = false;
        static constexpr bool value_can_be_null = false;
    };

    template <>
    struct ReturnValueTraits<return_value_string>
    {
        using deleter_type = del_string;
        static constexpr bool has_value = true;
        static constexpr bool value_can_be_null = true;
        static Value convert(const Env env, return_value_string *result)
        {
            const auto value = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->value);
            return String::New(env, value.get());
        }
    };

    template <>
    struct ReturnValueTraits<return_value_json>
    {
        using deleter_type = del_json;
        static constexpr bool has_value = true;
        static constexpr bool value_can_be_null = true;
        static Value convert(const Env env, return_value_json *result)
        {
            const auto value = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->value);
            return JSONParse(String::New(env, value.get()));
        }
    };

    template <>
    struct ReturnValueTraits<return_value_bool>
    {
        using deleter_type = del_bool;
        static constexpr bool has_value = true;
        static constexpr bool value_can_be_null = false;
        static Value convert(const Env env, return_value_bool *result)
        {
            return Boolean::New(env, result->value);
        }
    };

    template <>
    struct ReturnValueTraits<return_value_int32>
    {
        using deleter_type = del_int32;
        static constexpr bool has_value = true;
        static constexpr bool value_can_be_null = false;
        static Value convert(const Env env, return_value_int32 *result)
        {
            return Number::New(env, result->value);
        }
    };

    template <>
    struct ReturnValueTraits<return_value_uint32>
    {
        using deleter_type = del_uint32;
        static constexpr bool has_value = true;
        static constexpr bool value_can_be_null = false;
        static Value convert(const Env env, return_value_uint32 *result)
        {
            return Number::New(env, result->value);
        }
    };

    template <>
    struct ReturnValueTraits<return_value_ptr>
    {
        using deleter_type = del_ptr;
        static constexpr bool has_value = true;
        static constexpr bool value_can_be_null = false;
        static void *convert(const Env, return_value_ptr *result)
        {
            return result->value;
        }
    };

    // Unified template for ThrowOrReturn with value types
    template <typename T>
    inline decltype(auto) ThrowOrReturnValue(const Env env, T *const result)
    {
        using Traits = ReturnValueTraits<T>;
        const typename Traits::deleter_type del{result};

        if (result == nullptr)
        {
            Logger::Log(__FUNCTION__, "Null result");
            NAPI_THROW(Error::New(env, String::New(env, "Return value was null!")));
        }

        if (result->error == nullptr)
        {
            if constexpr (Traits::value_can_be_null)
            {
                if (result->value == nullptr)
                {
                    Logger::Log(__FUNCTION__, "Null error and value");
                    NAPI_THROW(Error::New(env, String::New(env, "Return value was null!")));
                }
            }
            return Traits::convert(env, result);
        }

        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }

    // Async result handler
    inline Napi::Value ReturnAndHandleReject(const Env env, return_value_async *const result, const Napi::Promise::Deferred deferred, Napi::ThreadSafeFunction tsfn)
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

    // Void return handler (special case - no return value)
    inline void ThrowOrReturn(const Env env, return_value_void *const val)
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

    // Convenience aliases that use the unified template
    inline Value ThrowOrReturnString(const Env env, return_value_string *const result)
    {
        return ThrowOrReturnValue(env, result);
    }

    inline Value ThrowOrReturnJson(const Env env, return_value_json *const result)
    {
        return ThrowOrReturnValue(env, result);
    }

    inline Value ThrowOrReturnBoolean(const Env env, return_value_bool *const result)
    {
        return ThrowOrReturnValue(env, result);
    }

    inline Value ThrowOrReturnInt32(const Env env, return_value_int32 *const result)
    {
        return ThrowOrReturnValue(env, result);
    }

    inline Value ThrowOrReturnUInt32(const Env env, return_value_uint32 *const result)
    {
        return ThrowOrReturnValue(env, result);
    }

    inline void *ThrowOrReturnPtr(const Env env, return_value_ptr *const result)
    {
        return ThrowOrReturnValue(env, result);
    }
}
#endif