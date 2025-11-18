#ifndef VE_FILESYSTEM_IMPL_GUARD_HPP_
#define VE_FILESYSTEM_IMPL_GUARD_HPP_

#include "utils.hpp"
#include "Utils.Callback.hpp"
#include "Utils.Async.hpp"
#include "ModInstaller.Native.h"
#include "Bindings.FileSystem.hpp"
#include "Bindings.FileSystem.Callbacks.hpp"
#include <codecvt>
#include <mutex>
#include <condition_variable>

using namespace Napi;
using namespace Utils;
using namespace Utils::Async;
using namespace Utils::Callback;
using namespace ModInstaller::Native;

namespace Bindings::FileSystem
{
    Object FileSystem::Init(const Napi::Env env, Object exports)
    {
        // This method is used to hook the accessor and method callbacks
        const auto func = DefineClass(env, "FileSystem",
                                      {
                                          InstanceMethod<&FileSystem::SetCallbacks>("setCallbacks", static_cast<napi_property_attributes>(napi_writable | napi_configurable)),
                                          StaticMethod<&FileSystem::SetDefaultCallbacks>("setDefaultCallbacks", static_cast<napi_property_attributes>(napi_writable | napi_configurable)),
                                      });

        auto *const constructor = new FunctionReference();

        // Create a persistent reference to the class constructor. This will allow
        // a function called on a class prototype and a function
        // called on instance of a class to be distinguished from each other.
        *constructor = Persistent(func);
        exports.Set("FileSystem", func);

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

    FileSystem::FileSystem(const CallbackInfo &info) : ObjectWrap<FileSystem>(info)
    {
        LoggerScope logger(__FUNCTION__);

        const auto env = Env();
        this->FReadFileContent = Persistent(info[0].As<Function>());
        this->FReadDirectoryFileList = Persistent(info[1].As<Function>());
        this->FReadDirectoryList = Persistent(info[2].As<Function>());

        // Initialize thread-safe function wrappers for synchronous callbacks
        this->TSFNReadFileContent = Napi::ThreadSafeFunction::New(env, this->FReadFileContent.Value(), "ReadFileContent", 0, 1);
        this->TSFNReadDirectoryFileList = Napi::ThreadSafeFunction::New(env, this->FReadDirectoryFileList.Value(), "ReadDirectoryFileList", 0, 1);
        this->TSFNReadDirectoryList = Napi::ThreadSafeFunction::New(env, this->FReadDirectoryList.Value(), "ReadDirectoryList", 0, 1);

        this->MainThreadId = std::this_thread::get_id();
    }

    FileSystem::~FileSystem()
    {
        LoggerScope logger(__FUNCTION__);

        // Release thread-safe functions
        this->TSFNReadFileContent.Release();
        this->TSFNReadDirectoryFileList.Release();
        this->TSFNReadDirectoryList.Release();

        // Release function references
        this->FReadFileContent.Unref();
        this->FReadDirectoryList.Unref();
        this->FReadDirectoryFileList.Unref();
    }

    void FileSystem::SetDefaultCallbacks(const CallbackInfo &info)
    {
        LoggerScope logger(__FUNCTION__);

        const auto env = info.Env();

        const auto result = set_default_file_system_callbacks();

        if (result != 0)
        {
            logger.Log("Error setting default file system callbacks");
            NAPI_THROW(Error::New(env, "Failed to set default file system callbacks"));
        }
    }

    void FileSystem::SetCallbacks(const CallbackInfo &info)
    {
        LoggerScope logger(__FUNCTION__);

        const auto env = info.Env();

        const auto result = set_file_system_callbacks(this,
                                                      readFileContent,
                                                      readDirectoryFileList,
                                                      readDirectoryList);

        if (result != 0)
        {
            logger.Log("Error setting file system callbacks");
            NAPI_THROW(Error::New(env, "Failed to set file system callbacks"));
        }
    }

    // Initialize native add-on
    Napi::Object Init(const Napi::Env env, const Napi::Object exports)
    {
        FileSystem::Init(env, exports);

        return exports;
    }
}
#endif
