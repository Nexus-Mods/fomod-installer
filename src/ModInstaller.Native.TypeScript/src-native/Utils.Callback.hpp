#ifndef LIB_UTILS_CALLBACK_GUARD_HPP_
#define LIB_UTILS_CALLBACK_GUARD_HPP_

// Define this before any Windows headers to prevent winsock.h conflicts
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <napi.h>
#include <uv.h>
#include "logger.hpp"
#include "utils.hpp"

using namespace Napi;
using namespace Utils;

namespace Utils::Callback
{
    inline return_value_string *CreateStringError(const std::u16string &msg)
    {
        return Create(return_value_string{Copy(msg), nullptr});
    }

    inline return_value_json *CreateJsonError(const std::u16string &msg)
    {
        return Create(return_value_json{Copy(msg), nullptr});
    }

    inline return_value_data *CreateDataError(const std::u16string &msg)
    {
        return Create(return_value_data{Copy(msg), nullptr, 0});
    }
}

#endif
