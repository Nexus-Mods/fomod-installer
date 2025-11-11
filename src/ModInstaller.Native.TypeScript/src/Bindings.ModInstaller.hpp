#ifndef VE_MODINSTALLER_GUARD_HPP_
#define VE_MODINSTALLER_GUARD_HPP_

#include "utils.hpp"
#include "Utils.Callback.hpp"
#include "Utils.Async.hpp"
#include "ModInstaller.Native.h"
#include <codecvt>
#include <mutex>
#include <condition_variable>
#include <thread>

using namespace Napi;
using namespace Utils;
using namespace Utils::Async;
using namespace Utils::Callback;
using namespace ModInstaller::Native;

namespace Bindings::ModInstaller
{
    class ModInstaller : public Napi::ObjectWrap<ModInstaller>
    {
    public:
        Napi::ThreadSafeFunction TSFN;
        Napi::ThreadSafeFunction TSFNPluginsGetAll;
        Napi::ThreadSafeFunction TSFNContextGetAppVersion;
        Napi::ThreadSafeFunction TSFNContextGetCurrentGameVersion;
        Napi::ThreadSafeFunction TSFNContextGetExtenderVersion;
        Napi::ThreadSafeFunction TSFNUIStartDialog;
        Napi::ThreadSafeFunction TSFNUIEndDialog;
        Napi::ThreadSafeFunction TSFNUIUpdateState;
        static void TSFNFunction(const Napi::CallbackInfo &info)
        {
            LoggerScope logger(__FUNCTION__);
            const auto length = info.Length();
            logger.Log("Length: " + std::to_string(length));

            if (length == 0)
            {
                logger.Log("No arguments provided");
                return;
            }

            const auto env = info.Env();
            const auto firstArg = info[0];

            // Check if first argument is a Promise
            // Using IsPromise() if available, otherwise duck typing with 'then' check
            bool isPromise = false;
            if (firstArg.IsObject())
            {
                const auto obj = firstArg.As<Napi::Object>();
                isPromise = obj.Has("then") && obj.Get("then").IsFunction();
            }

            if (length == 3 && isPromise)
            {
                // Promise case: jsCallback.Call({promise, onResolveCallback, onRejectCallback})
                // info[0] = promise
                // info[1] = onResolve callback
                // info[2] = onReject callback
                const auto promise = firstArg.As<Napi::Object>();
                const auto onResolve = info[1].As<Napi::Function>();
                const auto onReject = info[2].As<Napi::Function>();
                const auto then = promise.Get("then").As<Napi::Function>();
                then.Call(promise, {onResolve, onReject});
                logger.Log("Attached resolve and reject handlers to promise");
            }
            else if (length == 2 && isPromise)
            {
                // Promise case with only resolve handler
                // info[0] = promise
                // info[1] = onResolve callback
                const auto promise = firstArg.As<Napi::Object>();
                const auto onResolve = info[1].As<Napi::Function>();
                const auto then = promise.Get("then").As<Napi::Function>();
                then.Call(promise, {onResolve});
                logger.Log("Attached resolve handler to promise");
            }
            else if (length == 1 && !isPromise)
            {
                // Synchronous case: jsCallback.Call({result})
                // info[0] = synchronous result value
                // No handlers needed - value is already resolved
                logger.Log("Synchronous result provided (length=1, no handlers)");
                // Nothing to do - the synchronous result was already provided
            }
            else if (length == 1 && isPromise)
            {
                // Promise without handlers - let it execute but results ignored
                logger.Log("Promise provided without handlers (results will be ignored)");
            }
            else
            {
                // Unexpected calling pattern
                logger.Log("Unexpected TSFNFunction call pattern: length=" + std::to_string(length) +
                           ", isPromise=" + (isPromise ? "true" : "false"));
            }
        }

        FunctionReference FPluginsGetAll;
        FunctionReference FContextGetAppVersion;
        FunctionReference FContextGetCurrentGameVersion;
        FunctionReference FContextGetExtenderVersion;
        FunctionReference FUIStartDialog;
        FunctionReference FUIEndDialog;
        FunctionReference FUIUpdateState;

        std::thread::id MainThreadId;

        static Object Init(const Napi::Env env, const Object exports);

        ModInstaller(const CallbackInfo &info);
        ~ModInstaller();

        Napi::Value Install(const CallbackInfo &info);
        static Napi::Value TestSupported(const CallbackInfo &info);

    private:
        void *_pInstance;
    };
}
#endif
