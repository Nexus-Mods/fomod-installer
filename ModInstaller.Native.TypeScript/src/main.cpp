#define NODE_API_NO_EXTERNAL_BUFFERS_ALLOWED // Thanks Electron

#include "Bindings.Common.hpp"
#include "Bindings.ModInstaller.hpp"
#include <napi.h>

using namespace Napi;

Object InitAll(const Env env, const Object exports)
{
  Bindings::Common::Init(env, exports);
  Bindings::ModInstaller::Init(env, exports);
  return exports;
}

NODE_API_MODULE(NODE_GYP_MODULE_NAME, InitAll)
