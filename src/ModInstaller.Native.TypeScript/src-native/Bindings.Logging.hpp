#ifndef VE_LOGGING_GUARD_HPP_
#define VE_LOGGING_GUARD_HPP_

#include <napi.h>
#include "ModInstaller.Native.h"

using namespace Napi;
using namespace ModInstaller::Native;

namespace Bindings::Logging
{
    class Logger : public Napi::ObjectWrap<Logger>
    {
    public:
        Napi::ThreadSafeFunction TSFNLog;

        FunctionReference FLog;

        std::thread::id MainThreadId;

        static Object Init(const Napi::Env env, const Object exports);

        Logger(const CallbackInfo &info);
        ~Logger();

        void SetCallbacks(const CallbackInfo &info);
        void DisposeDefaultLogger(const CallbackInfo &info);
        static void SetDefaultCallbacks(const CallbackInfo &info);
    };
}
#endif
