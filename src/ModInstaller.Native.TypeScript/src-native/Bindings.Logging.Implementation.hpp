#ifndef VE_LOGGING_IMPL_GUARD_HPP_
#define VE_LOGGING_IMPL_GUARD_HPP_

#include "utils.hpp"
#include "Utils.Callback.hpp"
#include "Utils.Async.hpp"
#include "ModInstaller.Native.h"
#include "Bindings.Logging.hpp"
#include "Bindings.Logging.Callbacks.hpp"
#include <codecvt>
#include <mutex>
#include <condition_variable>

using namespace Napi;
using namespace Utils;
using namespace Utils::Async;
using namespace Utils::Callback;
using namespace ModInstaller::Native;

namespace Bindings::Logging
{
    Object Logger::Init(const Napi::Env env, Object exports)
    {
        // This method is used to hook the accessor and method callbacks
        const auto func = DefineClass(env, "Logger",
                                      {
                                          InstanceMethod<&Logger::SetCallbacks>("setCallbacks", static_cast<napi_property_attributes>(napi_writable | napi_configurable)),
                                          StaticMethod<&Logger::SetDefaultCallbacks>("setDefaultCallbacks", static_cast<napi_property_attributes>(napi_writable | napi_configurable)),
                                      });

        auto *const constructor = new FunctionReference();

        // Create a persistent reference to the class constructor. This will allow
        // a function called on a class prototype and a function
        // called on instance of a class to be distinguished from each other.
        *constructor = Persistent(func);
        exports.Set("Logger", func);

        // Store the constructor as the add-on instance data. This will allow this
        // add-on to support multiple instances of itself running on multiple worker
        // threads, as well as multiple instances of itself running in different
        // contexts on the same thread.
        //
        // By default, the value set on the environment here will be destroyed when
        // the add-on is unloaded using the `delete` operator, but it is also
        // possible to supply a custom deleter.
        const_cast<Napi::Env &>(env).SetInstanceData<FunctionReference>(constructor);

        return exports;
    }

    Logger::Logger(const CallbackInfo &info) : ObjectWrap<Logger>(info)
    {
        const auto env = Env();
        this->FLog = Persistent(info[0].As<Function>());

        // Initialize thread-safe function wrappers for synchronous callbacks
        this->TSFNLog = Napi::ThreadSafeFunction::New(env, this->FLog.Value(), "Log", 0, 1);

        this->MainThreadId = std::this_thread::get_id();
    }

    Logger::~Logger()
    {
        // Release thread-safe functions
        this->TSFNLog.Release();

        // Release function references
        this->FLog.Unref();
    }

    void Logger::SetDefaultCallbacks(const CallbackInfo &info)
    {
        const auto env = info.Env();

        const auto result = set_default_logging_callbacks();
        return ThrowOrReturn(env, result);
    }

    void Logger::SetCallbacks(const CallbackInfo &info)
    {
        const auto env = info.Env();

        const auto result = set_logging_callbacks(this,
                                                  log);
        return ThrowOrReturn(env, result);
    }

    void DisposeDefaultLogger(const CallbackInfo &info)
    {
        const auto env = info.Env();

        const auto result = dispose_default_logger();
        return ThrowOrReturn(env, result);
    }

    // Initialize native add-on
    Napi::Object Init(const Napi::Env env, const Napi::Object exports)
    {
        Logger::Init(env, exports);

        return exports;
    }
}
#endif
