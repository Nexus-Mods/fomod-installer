#ifndef VE_COMMON_GUARD_HPP_
#define VE_COMMON_GUARD_HPP_

#include <napi.h>
#include "ModInstaller.Native.h"

using namespace Napi;
using namespace ModInstaller::Native;

namespace Bindings::Common
{
    Value AllocWithOwnership(const CallbackInfo &info);

    Value AllocWithoutOwnership(const CallbackInfo &info);

    Value AllocAliveCount(const CallbackInfo &info);

    Object Init(const Env env, Object exports);
}
#endif
