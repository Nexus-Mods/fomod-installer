#ifndef VE_LIB_UTILS_GUARD_HPP_
#define VE_LIB_UTILS_GUARD_HPP_

#include <napi.h>
#include <cstdint>
#include "ModInstaller.Native.h"
#include <codecvt>
#include <locale>
#include "logger.hpp"

using namespace Napi;
using namespace ModInstaller::Native;

namespace Utils
{
    void ConsoleLog(const String message)
    {
        const auto env = message.Env();
        const auto consoleObject = env.Global().Get("console").As<Object>();
        const auto log = consoleObject.Get("log").As<Function>();
        // log.Call(consoleObject, {message});
    }
    String JSONStringify(const Object object)
    {
        const auto env = object.Env();

        if (object.IsUndefined() || object.IsNull())
        {
            return env.Null().As<String>();
        }
        const auto jsonObject = env.Global().Get("JSON").As<Object>();
        const auto stringify = jsonObject.Get("stringify").As<Function>();
        return stringify.Call(jsonObject, {object}).As<String>();
    }
    Object JSONParse(const String json)
    {
        const auto env = json.Env();
        const auto jsonObject = env.Global().Get("JSON").As<Object>();
        const auto parse = jsonObject.Get("parse").As<Function>();
        return parse.Call(jsonObject, {json}).As<Object>();
    }

    struct ResultCallbackData
    {
        Napi::Promise::Deferred deferred;
        Napi::ThreadSafeFunction tsfn;
    };
    ResultCallbackData *CreateResultCallbackData(const Napi::Env env, const std::string callbackName)
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);

        const auto deferred = Napi::Promise::Deferred::New(env);
        const auto callback = [functionName, deferred](const Napi::CallbackInfo &info)
        {
            LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));
            const auto env = info.Env();

            const auto length = info.Length();

            if (length == 0)
            {
                deferred.Resolve(env.Undefined());
            }
            else if (length == 2)
            {
                const auto isErr = info[0].As<Napi::Boolean>();
                const auto obj = info[1];
                if (isErr.Value())
                {
                    callbackLogger.Log("Rejecting");
                    deferred.Reject(obj);
                }
                else
                {
                    callbackLogger.Log("Resolving");
                    deferred.Resolve(obj);
                }
            }
            else
            {
                deferred.Reject(Napi::Error::New(env, "Too many arguments").Value());
            }
        };
        const auto tsfn = Napi::ThreadSafeFunction::New(env, Napi::Function::New(env, callback), callbackName, 0, 1);
        return new ResultCallbackData{deferred, tsfn};
    }

    template <typename T>
    struct common_deallocor
    {
        void operator()(T *const ptr) const
        {
            if (ptr != nullptr)
            {
                common_dealloc(static_cast<void *const>(ptr));
            }
        }
    };

    template <typename T>
    struct deleter
    {
        void operator()(T *const ptr) const { delete ptr; }
    };

    using del_rcbd = std::unique_ptr<ResultCallbackData, deleter<ResultCallbackData>>;
    using del_void = std::unique_ptr<return_value_void, common_deallocor<return_value_void>>;
    using del_string = std::unique_ptr<return_value_string, common_deallocor<return_value_string>>;
    using del_json = std::unique_ptr<return_value_json, common_deallocor<return_value_json>>;
    using del_bool = std::unique_ptr<return_value_bool, common_deallocor<return_value_bool>>;
    using del_int32 = std::unique_ptr<return_value_int32, common_deallocor<return_value_int32>>;
    using del_uint32 = std::unique_ptr<return_value_uint32, common_deallocor<return_value_uint32>>;
    using del_ptr = std::unique_ptr<return_value_ptr, common_deallocor<return_value_ptr>>;
    using del_async = std::unique_ptr<return_value_async, common_deallocor<return_value_async>>;

    uint8_t *const Copy(const uint8_t *src, const size_t length)
    {
        auto dst = static_cast<uint8_t *const>(common_alloc(length));
        if (dst == nullptr)
        {
            throw std::bad_alloc();
        }
        std::memmove(dst, src, length);
        return dst;
    }

    char16_t *const Copy(const std::u16string str)
    {
        const auto src = str.c_str();
        const auto srcChar16Length = str.length();
        const auto srcByteLength = srcChar16Length * sizeof(char16_t);
        const auto size = srcByteLength + sizeof(char16_t);

        auto dst = static_cast<char16_t *const>(common_alloc(size));
        if (dst == nullptr)
        {
            throw std::bad_alloc();
        }
        std::memmove(dst, src, srcByteLength);
        dst[srcChar16Length] = '\0';
        return dst;
    }

    std::unique_ptr<uint8_t[], common_deallocor<uint8_t>> CopyWithFree(const uint8_t *const data, size_t length)
    {
        return std::unique_ptr<uint8_t[], common_deallocor<uint8_t>>(Copy(data, length));
    }

    std::unique_ptr<char16_t[], common_deallocor<char16_t>> CopyWithFree(const std::u16string str)
    {
        return std::unique_ptr<char16_t[], common_deallocor<char16_t>>(Copy(str));
    }

    std::unique_ptr<char16_t[], common_deallocor<char16_t>> NullStringCopy()
    {
        return std::unique_ptr<char16_t[], common_deallocor<char16_t>>(nullptr);
    }

    const char16_t *const NoCopy(const std::u16string str) noexcept
    {
        return str.c_str();
    }

    template <typename T>
    T *const Create(const T val)
    {
        const auto size = sizeof(T);
        auto dst = static_cast<T *const>(common_alloc(size));
        if (dst == nullptr)
        {
            throw std::bad_alloc();
        }
        std::memcpy(dst, &val, sizeof(T));
        return dst;
    }

    std::u16string GetErrorMessage(const Napi::Error e)
    {
        const auto errorValue = e.Value();
        if (errorValue.Has("stack"))
        {
            const auto stack = e.Value().Get("stack").As<String>();
            return stack.Utf16Value();
        }

        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
        return conv.from_bytes(e.Message());
    }

    Napi::Value ReturnAndHandleReject(const Env env, return_value_async *const result, const Napi::Promise::Deferred &deferred, Napi::ThreadSafeFunction tsfn)
    {
        LoggerScope logger(__FUNCTION__);
        const del_async del{result};

        if (result->error != nullptr)
        {
            // del_rcbd delCb{cbData};
            const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
            deferred.Reject(Error::New(env, String::New(env, error.get())).Value());
            tsfn.Release();
        }

        return deferred.Promise();
    }

    void ThrowOrReturn(const Env env, return_value_void *const val)
    {
        LoggerScope logger(__FUNCTION__);
        const del_void del{val};

        if (val->error == nullptr)
        {
            return;
        }

        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(val->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }
    Value ThrowOrReturnString(const Env env, return_value_string *const result)
    {
        LoggerScope logger(__FUNCTION__);
        const del_string del{result};

        if (result->error == nullptr)
        {
            if (result->value == nullptr)
            {
                logger.Log("Null error and value");
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
        LoggerScope logger(__FUNCTION__);
        const del_json del{result};

        if (result->error == nullptr)
        {
            if (result->value == nullptr)
            {
                logger.Log("Null error and value");
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
        LoggerScope logger(__FUNCTION__);
        const del_bool del{result};

        if (result->error == nullptr)
        {
            return Boolean::New(env, result->value);
        }

        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }
    Value ThrowOrReturnInt32(const Env env, return_value_int32 *const result)
    {
        LoggerScope logger(__FUNCTION__);
        const del_int32 del{result};

        if (result->error == nullptr)
        {
            return Number::New(env, result->value);
        }

        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }
    Value ThrowOrReturnUInt32(const Env env, return_value_uint32 *const result)
    {
        LoggerScope logger(__FUNCTION__);
        const del_uint32 del{result};

        if (result->error == nullptr)
        {
            return Number::New(env, result->value);
        }

        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }
    void *ThrowOrReturnPtr(const Env env, return_value_ptr *const result)
    {
        LoggerScope logger(__FUNCTION__);
        const del_ptr del{result};

        if (result->error == nullptr)
        {
            return result->value;
        }
        const auto error = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(result->error);
        NAPI_THROW(Error::New(env, String::New(env, error.get())));
    }

    void HandleVoidResultCallback(param_ptr *handler, return_value_void *returnData)
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);

        auto *cbData = static_cast<ResultCallbackData *>(handler);
        del_rcbd delCb{cbData};

        auto tsfn = cbData->tsfn;

        const auto lambda = [functionName](Napi::Env env, Napi::Function jsCallback, return_value_void *returnData)
        {
            LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));

            del_void del{returnData};

            if (returnData->error != nullptr)
            {
                callbackLogger.Log("Error");
                const auto isError = Napi::Boolean::New(env, true);
                const auto errorStr = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(returnData->error);
                const auto error = Napi::Error::New(env, String::New(env, errorStr.get())).Value();
                jsCallback.Call({isError, error});
            }
            else
            {
                callbackLogger.Log("Resolving");
                jsCallback.Call({});
            }
        };

        tsfn.BlockingCall(returnData, lambda);
        tsfn.Release();
    }
    void HandleJsonResultCallback(param_ptr *handler, return_value_json *returnData)
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);

        auto *cbData = static_cast<ResultCallbackData *>(handler);
        del_rcbd delCb{cbData};
        auto tsfn = cbData->tsfn;

        const auto lambda = [functionName](Napi::Env env, Napi::Function jsCallback, return_value_json *returnData)
        {
            LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));

            del_json del{returnData};

            if (returnData->error != nullptr)
            {
                callbackLogger.Log("Error");
                const auto isError = Napi::Boolean::New(env, true);
                const auto errorStr = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(returnData->error);
                const auto error = Napi::Error::New(env, String::New(env, errorStr.get())).Value();
                jsCallback.Call({isError, error});
            }
            else
            {
                callbackLogger.Log("Resolving");
                const auto isError = Napi::Boolean::New(env, false);
                if (returnData->value == nullptr)
                {
                    const auto result = env.Null();
                    jsCallback.Call({isError, result});
                }
                else
                {
                    callbackLogger.Log("Resolving");
                    const auto resultStr = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(returnData->value);
                    const auto result = JSONParse(Napi::String::New(env, resultStr.get()));
                    jsCallback.Call({isError, result});
                }
            }
        };
        tsfn.BlockingCall(returnData, lambda);
        tsfn.Release();
    }
    void HandleStringResultCallback(param_ptr *handler, return_value_string *returnData)
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);

        auto *cbData = static_cast<ResultCallbackData *>(handler);
        del_rcbd delCb{cbData};

        const auto deferred = cbData->deferred;
        auto tsfn = cbData->tsfn;

        const auto lambda = [functionName](Napi::Env env, Napi::Function jsCallback, return_value_string *returnData)
        {
            LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));

            del_string del{returnData};

            if (returnData->error != nullptr)
            {
                callbackLogger.Log("Resolving");
                const auto isError = Napi::Boolean::New(env, true);
                const auto errorStr = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(returnData->error);
                const auto error = Napi::Error::New(env, String::New(env, errorStr.get())).Value();
                jsCallback.Call({isError, error});
            }
            else
            {
                callbackLogger.Log("Resolving");
                const auto isError = Napi::Boolean::New(env, false);
                if (returnData->value == nullptr)
                {
                    const auto result = env.Null();
                    jsCallback.Call({isError, result});
                }
                else
                {
                    const auto resultStr = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(returnData->value);
                    const auto result = Napi::String::New(env, resultStr.get());
                    jsCallback.Call({isError, result});
                }
            }
        };
        tsfn.BlockingCall(returnData, lambda);
        tsfn.Release();
    }
    void HandleBooleanResultCallback(param_ptr *handler, return_value_bool *returnData)
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);
        auto *cbData = static_cast<ResultCallbackData *>(handler);
        del_rcbd del{cbData};
        auto tsfn = cbData->tsfn;

        const auto lambda = [functionName](Napi::Env env, Napi::Function jsCallback, return_value_bool *returnData)
        {
            LoggerScope callbackLogger(NAMEOFWITHCALLBACK(functionName, callback));

            del_bool del{returnData};

            if (returnData->error != nullptr)
            {
                callbackLogger.Log("Resolving");
                const auto isError = Napi::Boolean::New(env, true);
                const auto errorStr = std::unique_ptr<char16_t[], common_deallocor<char16_t>>(returnData->error);
                const auto error = Napi::Error::New(env, String::New(env, errorStr.get())).Value();
                jsCallback.Call({isError, error});
            }
            else
            {
                callbackLogger.Log("Resolving");
                const auto isError = Napi::Boolean::New(env, false);
                const auto result = Boolean::New(env, returnData->value == 1);
                jsCallback.Call({isError, result});
            }
        };
        tsfn.BlockingCall(returnData, lambda);
        tsfn.Release();
    }
}

#endif
