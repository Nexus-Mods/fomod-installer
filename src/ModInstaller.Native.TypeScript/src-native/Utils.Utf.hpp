#ifndef LIB_UTILS_UTF_GUARD_HPP_
#define LIB_UTILS_UTF_GUARD_HPP_

#include <string>
#include <codecvt>
#include <locale>

// Suppress deprecation warnings for std::wstring_convert
#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable : 4996)
#endif

namespace Utils
{
    // Convert UTF-8 string to UTF-16 string
    inline std::u16string Utf8ToUtf16(const std::string &utf8)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
        return conv.from_bytes(utf8);
    }

    // Convert UTF-16 string to UTF-8 string
    inline std::string Utf16ToUtf8(const std::u16string &utf16)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
        return conv.to_bytes(utf16);
    }

    // Overload for char16_t* (null-terminated)
    inline std::string Utf16ToUtf8(const char16_t *utf16)
    {
        if (utf16 == nullptr)
            return "";
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> conv;
        return conv.to_bytes(utf16);
    }
}

#ifdef _MSC_VER
#pragma warning(pop)
#endif

#endif
