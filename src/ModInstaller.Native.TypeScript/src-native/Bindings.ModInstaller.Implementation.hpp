#ifndef VE_MODINSTALLER_IMPL_GUARD_HPP_
#define VE_MODINSTALLER_IMPL_GUARD_HPP_

#include "utils.hpp"
#include "Utils.Callback.hpp"
#include "Utils.Async.hpp"
#include "ModInstaller.Native.h"
#include "Bindings.ModInstaller.hpp"
#include "Bindings.ModInstaller.Callbacks.hpp"
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
    Object ModInstaller::Init(const Napi::Env env, Object exports)
    {
        // This method is used to hook the accessor and method callbacks
        const auto func = DefineClass(env, "ModInstaller",
                                      {
                                          InstanceMethod<&ModInstaller::Install>("install", static_cast<napi_property_attributes>(napi_writable | napi_configurable)),
                                          StaticMethod<&ModInstaller::TestSupported>("testSupported", static_cast<napi_property_attributes>(napi_writable | napi_configurable)),
                                      });

        auto *const constructor = new FunctionReference();

        // Create a persistent reference to the class constructor. This will allow
        // a function called on a class prototype and a function
        // called on instance of a class to be distinguished from each other.
        *constructor = Persistent(func);
        exports.Set("ModInstaller", func);

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

    ModInstaller::ModInstaller(const CallbackInfo &info) : ObjectWrap<ModInstaller>(info)
    {
        LoggerScope logger(__FUNCTION__);

        const auto env = Env();
        this->TSFN = Napi::ThreadSafeFunction::New(env, Napi::Function::New(env, TSFNFunction), "TSFN", 0, 1);
        this->FPluginsGetAll = Persistent(info[0].As<Function>());
        this->FContextGetAppVersion = Persistent(info[1].As<Function>());
        this->FContextGetCurrentGameVersion = Persistent(info[2].As<Function>());
        this->FContextGetExtenderVersion = Persistent(info[3].As<Function>());
        this->FUIStartDialog = Persistent(info[4].As<Function>());
        this->FUIEndDialog = Persistent(info[5].As<Function>());
        this->FUIUpdateState = Persistent(info[6].As<Function>());

        // Initialize thread-safe function wrappers for synchronous callbacks
        this->TSFNPluginsGetAll = Napi::ThreadSafeFunction::New(env, this->FPluginsGetAll.Value(), "PluginsGetAll", 0, 1);
        this->TSFNContextGetAppVersion = Napi::ThreadSafeFunction::New(env, this->FContextGetAppVersion.Value(), "ContextGetAppVersion", 0, 1);
        this->TSFNContextGetCurrentGameVersion = Napi::ThreadSafeFunction::New(env, this->FContextGetCurrentGameVersion.Value(), "ContextGetCurrentGameVersion", 0, 1);
        this->TSFNContextGetExtenderVersion = Napi::ThreadSafeFunction::New(env, this->FContextGetExtenderVersion.Value(), "ContextGetExtenderVersion", 0, 1);
        this->TSFNUIStartDialog = Napi::ThreadSafeFunction::New(env, this->FUIStartDialog.Value(), "UIStartDialog", 0, 1);
        this->TSFNUIEndDialog = Napi::ThreadSafeFunction::New(env, this->FUIEndDialog.Value(), "UIEndDialog", 0, 1);
        this->TSFNUIUpdateState = Napi::ThreadSafeFunction::New(env, this->FUIUpdateState.Value(), "UIUpdateState", 0, 1);

        const auto result = create_handler(this,
                                           pluginsGetAll,
                                           contextGetAppVersion,
                                           contextGetCurrentGameVersion,
                                           contextGetExtenderVersion,
                                           uiStartDialog,
                                           uiEndDialog,
                                           uiUpdateState);
        this->_pInstance = ThrowOrReturnPtr(env, result);

        this->MainThreadId = std::this_thread::get_id();
    }

    ModInstaller::~ModInstaller()
    {
        LoggerScope logger(__FUNCTION__);

        // Release thread-safe functions
        this->TSFN.Release();
        this->TSFNPluginsGetAll.Release();
        this->TSFNContextGetAppVersion.Release();
        this->TSFNContextGetCurrentGameVersion.Release();
        this->TSFNContextGetExtenderVersion.Release();
        this->TSFNUIStartDialog.Release();
        this->TSFNUIEndDialog.Release();
        this->TSFNUIUpdateState.Release();

        // Release function references
        this->FPluginsGetAll.Unref();
        this->FContextGetAppVersion.Unref();
        this->FContextGetCurrentGameVersion.Unref();
        this->FContextGetExtenderVersion.Unref();
        this->FUIStartDialog.Unref();
        this->FUIEndDialog.Unref();
        this->FUIUpdateState.Unref();
        dispose_handler(this->_pInstance);
    }

    Value ModInstaller::Install(const CallbackInfo &info)
    {
        const auto functionName = __FUNCTION__;
        LoggerScope logger(functionName);

        const auto env = info.Env();
        const auto files = JSONStringify(info[0].As<Object>());
        const auto stopPatterns = JSONStringify(info[1].As<Object>());
        const auto pluginPathRaw = info[2];
        const auto scriptPath = info[3].As<String>();
        const auto presetRaw = info[4];
        const auto validate = info[5].As<Boolean>();

        const auto filesCopy = CopyWithFree(files.Utf16Value());
        const auto stopPatternsCopy = CopyWithFree(stopPatterns.Utf16Value());
        const auto pluginPathCopy = pluginPathRaw.IsNull() ? NullStringCopy() : CopyWithFree(pluginPathRaw.As<String>().Utf16Value());
        const auto scriptPathCopy = CopyWithFree(scriptPath.Utf16Value());
        const auto presetCopy = presetRaw.IsUndefined() || presetRaw.IsNull() ? NullStringCopy() : CopyWithFree(JSONStringify(presetRaw.As<Object>()));
        const auto validateCopy = validate.Value() ? (uint8_t)1 : (uint8_t)0;

        auto cbData = CreateResultCallbackData(env, NAMEOFWITHCALLBACK(functionName, HandleJsonResultCallback));
        const auto deferred = cbData->deferred;
        const auto tsfn = cbData->tsfn;

        const auto result = install(
            this->_pInstance,
            filesCopy.get(),
            stopPatternsCopy.get(),
            pluginPathCopy.get(),
            scriptPathCopy.get(),
            presetCopy.get(),
            validateCopy,
            cbData,
            HandleJsonResultCallback);
        return ReturnAndHandleReject(env, result, deferred, tsfn);
    }

    Value ModInstaller::TestSupported(const CallbackInfo &info)
    {
        LoggerScope logger(__FUNCTION__);

        const auto env = info.Env();
        const auto modArchiveFileList = JSONStringify(info[0].As<Object>());
        const auto allowedTypes = JSONStringify(info[1].As<Object>());

        const auto modArchiveFileListCopy = CopyWithFree(modArchiveFileList.Utf16Value());
        const auto allowedTypesCopy = CopyWithFree(allowedTypes.Utf16Value());

        const auto result = test_supported(modArchiveFileListCopy.get(), allowedTypesCopy.get());
        return ThrowOrReturnJson(env, result);
    }

    // Initialize native add-on
    Napi::Object Init(const Napi::Env env, const Napi::Object exports)
    {
        ModInstaller::Init(env, exports);

        return exports;
    }
}
#endif
