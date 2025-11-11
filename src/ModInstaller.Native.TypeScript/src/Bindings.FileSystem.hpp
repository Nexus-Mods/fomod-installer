#ifndef VE_FILESYSTEM_GUARD_HPP_
#define VE_FILESYSTEM_GUARD_HPP_

#include "utils.hpp"
#include "Utils.Callback.hpp"
#include "Utils.Async.hpp"
#include "ModInstaller.Native.h"
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
    class FileSystem : public Napi::ObjectWrap<FileSystem>
    {
    public:
        Napi::ThreadSafeFunction TSFNReadFileContent;
        Napi::ThreadSafeFunction TSFNReadDirectoryFileList;
        Napi::ThreadSafeFunction TSFNReadDirectoryList;

        FunctionReference FReadFileContent;
        FunctionReference FReadDirectoryFileList;
        FunctionReference FReadDirectoryList;

        std::thread::id MainThreadId;

        static Object Init(const Napi::Env env, const Object exports);

        FileSystem(const CallbackInfo &info);
        ~FileSystem();

        void SetCallbacks(const CallbackInfo &info);
        static void SetDefaultCallbacks(const CallbackInfo &info);
    };
}
#endif
