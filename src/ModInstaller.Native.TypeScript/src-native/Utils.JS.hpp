#ifndef VE_LIB_UTILS_JS_GUARD_HPP_
#define VE_LIB_UTILS_JS_GUARD_HPP_

#include <napi.h>
#include "Logger.hpp"

using namespace Napi;
using namespace ModInstaller::Native;

namespace Utils
{
    void ConsoleLog(const String message)
    {
        const auto env = message.Env();
        const auto consoleObject = env.Global().Get("console").As<Object>();
        const auto log = consoleObject.Get("log").As<Function>();
        // log.Call(consoleObject, {message});
    }

    String JSONStringify(const Object object)
    {
        const auto env = object.Env();

        if (object.IsUndefined() || object.IsNull())
        {
            Logger::Log(__FUNCTION__, "Null or undefined object");
            return env.Null().As<String>();
        }
        const auto jsonObject = env.Global().Get("JSON").As<Object>();
        const auto stringify = jsonObject.Get("stringify").As<Function>();
        return stringify.Call(jsonObject, {object}).As<String>();
    }

    Object JSONParse(const String json)
    {
        const auto env = json.Env();
        const auto jsonObject = env.Global().Get("JSON").As<Object>();
        const auto parse = jsonObject.Get("parse").As<Function>();
        return parse.Call(jsonObject, {json}).As<Object>();
    }
}
#endif