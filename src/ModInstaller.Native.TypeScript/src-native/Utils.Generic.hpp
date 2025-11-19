#ifndef VE_LIB_UTILS_GUARD_HPP_
#define VE_LIB_UTILS_GUARD_HPP_

#include <napi.h>
#include <codecvt>
#include "ModInstaller.Native.h"
#include "Logger.hpp"

using namespace Napi;
using namespace ModInstaller::Native;

namespace Utils
{
    template <typename T>
    struct common_deallocor
    {
        void operator()(T *const ptr) const
        {
            if (ptr != nullptr)
            {
                common_dealloc(static_cast<void *const>(ptr));
            }
        }
    };

    template <typename T>
    struct deleter
    {
        void operator()(T *const ptr) const { delete ptr; }
    };

    using del_void = std::unique_ptr<return_value_void, common_deallocor<return_value_void>>;
    using del_string = std::unique_ptr<return_value_string, common_deallocor<return_value_string>>;
    using del_json = std::unique_ptr<return_value_json, common_deallocor<return_value_json>>;
    using del_bool = std::unique_ptr<return_value_bool, common_deallocor<return_value_bool>>;
    using del_int32 = std::unique_ptr<return_value_int32, common_deallocor<return_value_int32>>;
    using del_uint32 = std::unique_ptr<return_value_uint32, common_deallocor<return_value_uint32>>;
    using del_ptr = std::unique_ptr<return_value_ptr, common_deallocor<return_value_ptr>>;
    using del_async = std::unique_ptr<return_value_async, common_deallocor<return_value_async>>;

    uint8_t *const Copy(const uint8_t *src, const size_t length)
    {
        auto dst = static_cast<uint8_t *const>(common_alloc(length));
        if (dst == nullptr)
        {
            Logger::Log(__FUNCTION__, "Failed to allocate memory");
            throw std::bad_alloc();
        }
        std::memmove(dst, src, length);
        return dst;
    }

    char16_t *const Copy(const std::u16string str)
    {
        const auto src = str.c_str();
        const auto srcChar16Length = str.length();
        const auto srcByteLength = srcChar16Length * sizeof(char16_t);
        const auto size = srcByteLength + sizeof(char16_t);

        auto dst = static_cast<char16_t *const>(common_alloc(size));
        if (dst == nullptr)
        {
            Logger::Log(__FUNCTION__, "Failed to allocate memory");
            throw std::bad_alloc();
        }
        std::memmove(dst, src, srcByteLength);
        dst[srcChar16Length] = '\0';
        return dst;
    }

    std::unique_ptr<uint8_t[], common_deallocor<uint8_t>> CopyWithFree(const uint8_t *const data, size_t length)
    {
        return std::unique_ptr<uint8_t[], common_deallocor<uint8_t>>(Copy(data, length));
    }

    std::unique_ptr<char16_t[], common_deallocor<char16_t>> CopyWithFree(const std::u16string str)
    {
        return std::unique_ptr<char16_t[], common_deallocor<char16_t>>(Copy(str));
    }

    std::unique_ptr<char16_t[], common_deallocor<char16_t>> NullStringCopy()
    {
        return std::unique_ptr<char16_t[], common_deallocor<char16_t>>(nullptr);
    }

    const char16_t *const NoCopy(const std::u16string str) noexcept
    {
        return str.c_str();
    }

    template <typename T>
    T *const Create(const T val)
    {
        const auto size = sizeof(T);
        auto dst = static_cast<T *const>(common_alloc(size));
        if (dst == nullptr)
        {
            Logger::Log(__FUNCTION__, "Failed to allocate memory");
            throw std::bad_alloc();
        }
        std::memcpy(dst, &val, sizeof(T));
        return dst;
    }
}

#endif
