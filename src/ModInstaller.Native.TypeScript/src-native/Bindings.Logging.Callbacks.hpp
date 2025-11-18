#ifndef VE_LOGGING_CB_GUARD_HPP_
#define VE_LOGGING_CB_GUARD_HPP_

#include "utils.hpp"
#include "Utils.Callback.hpp"
#include "Utils.Async.hpp"
#include "ModInstaller.Native.h"
#include "Bindings.Logging.hpp"
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
    static int32_t log(param_ptr *p_owner,
                       param_int level,
                       param_string *message) noexcept
    {
        try
        {
            auto manager = const_cast<Bindings::Logging::Logger *>(static_cast<const Bindings::Logging::Logger *>(p_owner));

            if (std::this_thread::get_id() == manager->MainThreadId)
            {
                const auto env = manager->FLog.Env();
                const auto levelValue = Number::New(env, level);
                const auto messageValue = String::New(env, message);
                manager->FLog({levelValue, messageValue});
                return 0;
            }
            else
            {
                // The C# async function called from a non-main JS thread
                // So we need to use the ThreadSafeFunction to marshal the call to the main JS thread
                // and wait for the result synchronously

                std::mutex mtx;
                std::condition_variable cv;
                bool completed = false;
                int32_t result = 0;

                const auto callback = [manager, level, message, &result, &mtx, &cv, &completed](Napi::Env env, Napi::Function jsCallback)
                {
                    try
                    {
                        const auto levelValue = Napi::Number::New(env, level);
                        const auto messageValue = Napi::String::New(env, message);
                        jsCallback({levelValue, messageValue});

                        std::lock_guard<std::mutex> lock(mtx);
                        result = 0;
                        completed = true;
                        cv.notify_one();
                    }
                    catch (const Napi::Error &e)
                    {
                        std::cerr << "Error in log callback: " << e.what() << std::endl;

                        std::lock_guard<std::mutex> lock(mtx);
                        result = -1;
                        completed = true;
                        cv.notify_one();
                    }
                };

                const auto status = manager->TSFNLog.BlockingCall(callback);
                if (status != napi_ok)
                {
                    std::cerr << "Error calling ThreadSafeFunction for log callback" << std::endl;
                    return -2;
                }

                std::unique_lock<std::mutex> lock(mtx);
                cv.wait(lock, [&completed]
                        { return completed; });

                return result;
            }
        }
        catch (const Napi::Error &e)
        {
            std::cerr << "Error in log callback: " << e.what() << std::endl;
            return -3;
        }
        catch (const std::exception &e)
        {
            std::cerr << "Error in log callback: " << e.what() << std::endl;
            return -4;
        }
        catch (...)
        {
            std::cerr << "Unknown error in log callback" << std::endl;
            return -5;
        }
    }
}
#endif
