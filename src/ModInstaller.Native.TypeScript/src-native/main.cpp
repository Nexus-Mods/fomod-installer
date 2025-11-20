#define NODE_API_NO_EXTERNAL_BUFFERS_ALLOWED // Thanks Electron

#include <napi.h>
#include "Bindings.Common.hpp"
#include "Bindings.Logging.Implementation.hpp"
#include "Bindings.ModInstaller.Implementation.hpp"
#include "Bindings.FileSystem.Implementation.hpp"

using namespace Napi;

Object InitAll(const Env env, const Object exports)
{
  Bindings::Common::Init(env, exports);
  Bindings::Logging::Init(env, exports);
  Bindings::ModInstaller::Init(env, exports);
  Bindings::FileSystem::Init(env, exports);
  return exports;
}

NODE_API_MODULE(NODE_GYP_MODULE_NAME, InitAll)
