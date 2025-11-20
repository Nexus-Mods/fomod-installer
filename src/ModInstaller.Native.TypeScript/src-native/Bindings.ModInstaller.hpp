#ifndef VE_MODINSTALLER_GUARD_HPP_
#define VE_MODINSTALLER_GUARD_HPP_

#include <napi.h>
#include "ModInstaller.Native.h"
#include "Logger.hpp"

using namespace Napi;
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
