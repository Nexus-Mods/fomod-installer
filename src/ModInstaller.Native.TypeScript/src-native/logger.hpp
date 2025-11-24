#ifndef VE_LIB_LOGGER_GUARD_HPP_
#define VE_LIB_LOGGER_GUARD_HPP_

#define NAMEOF(x) #x
#define NAMEOFWITHCALLBACK(x, y) (std::string(x) + "_" + #y)

// Don't even care anymore

#define EXTRACT_FUNCTION_NAME(function) \
    []() { \
                std::string fullName = function; \
                size_t lastColon = fullName.rfind("::"); \
                size_t secondLastColon = fullName.rfind("::", lastColon - 1); \
                size_t thirdLastColon = fullName.rfind("::", secondLastColon - 1); \
                return fullName.substr(thirdLastColon + 2, secondLastColon - thirdLastColon - 2); }()

#define EXTRACT_LAMBDA_FUNCTION_NAME(function) \
    []() { \
        std::string fullName = function; \
        size_t lastColon = fullName.rfind("::"); \
        size_t secondLastColon = fullName.rfind("::", lastColon - 1); \
        size_t thirdLastColon = fullName.rfind("::", secondLastColon - 1); \
        size_t fourthLastColon = fullName.rfind("::", thirdLastColon - 1); \
        return fullName.substr(0, fourthLastColon); }()

/*
#define LOG(message) Logger::Log(std::string(__FUNCTION__) + std::string(" - ") + std::string(message))
#define LOGINPUT() Logger::LogInput(__FUNCTION__)
#define LOGOUTPUT() Logger::LogOutput(__FUNCTION__)

#define LOGLAMBDA(caller, message) Logger::Log(std::string(EXTRACT_LAMBDA_FUNCTION_NAME(__FUNCTION__)) + std::string("::") + std::string(caller) + std::string(" - ") + std::string(message))
#define LOGINPUTLAMBDA(caller) Logger::LogInput(std::string(EXTRACT_LAMBDA_FUNCTION_NAME(__FUNCTION__)) + std::string("::") + std::string(caller))
#define LOGOUTPUTLAMBDA(caller) Logger::LogOutput(std::string(EXTRACT_LAMBDA_FUNCTION_NAME(__FUNCTION__)) + std::string("::") + std::string(caller))
*/

#define LOG(message)                                                    \
    do                                                                  \
    {                                                                   \
        std::ostringstream oss;                                         \
        oss << EXTRACT_FUNCTION_NAME(__FUNCTION__) << " - " << message; \
        Logger::Log(oss.str());                                         \
    } while (0)

#define LOGINPUT()                                  \
    do                                              \
    {                                               \
        std::ostringstream oss;                     \
        oss << EXTRACT_FUNCTION_NAME(__FUNCTION__); \
        Logger::LogInput(oss.str());                \
    } while (0)

#define LOGOUTPUT()                                 \
    do                                              \
    {                                               \
        std::ostringstream oss;                     \
        oss << EXTRACT_FUNCTION_NAME(__FUNCTION__); \
        Logger::LogOutput(oss.str());               \
    } while (0)

#define LOGLAMBDA(caller, message)                                                              \
    do                                                                                          \
    {                                                                                           \
        std::ostringstream oss;                                                                 \
        oss << EXTRACT_LAMBDA_FUNCTION_NAME(__FUNCTION__) << "_" << caller << " - " << message; \
        Logger::Log(oss.str());                                                                 \
    } while (0)

#define LOGINPUTLAMBDA(caller)                                              \
    do                                                                      \
    {                                                                       \
        std::ostringstream oss;                                             \
        oss << EXTRACT_LAMBDA_FUNCTION_NAME(__FUNCTION__) << "_" << caller; \
        Logger::LogInput(oss.str());                                        \
    } while (0)

#define LOGOUTPUTLAMBDA(caller)                                             \
    do                                                                      \
    {                                                                       \
        std::ostringstream oss;                                             \
        oss << EXTRACT_LAMBDA_FUNCTION_NAME(__FUNCTION__) << "_" << caller; \
        Logger::LogOutput(oss.str());                                       \
    } while (0)

#include <napi.h>
#include <codecvt>
#include <locale>
#include <string>
#include <sstream>
#include "ModInstaller.Native.h"

using namespace Napi;
using namespace ModInstaller::Native;

class Logger
{
private:
    static const std::string _logFilePath;
    static const std::wstring _mutexName;

    static std::string ExtractFunctionName(std::string function)
    {
        size_t lastColon = function.rfind("::");
        if (lastColon == std::string::npos)
            return function;

        size_t secondLastColon = function.rfind("::", lastColon - 1);
        if (secondLastColon == std::string::npos)
            return function.substr(0, lastColon);

        size_t thirdLastColon = function.rfind("::", secondLastColon - 1);
        if (thirdLastColon == std::string::npos)
            return function.substr(secondLastColon + 2, lastColon - secondLastColon - 2);

        return function.substr(thirdLastColon + 2, secondLastColon - thirdLastColon - 2);
    };

public:
    static void Log(const std::string &message)
    {
        // Convert UTF-8 string to UTF-16 for the log function
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        std::u16string utf16_message = convert.from_bytes(message);
        ModInstaller::Native::log_message(2, const_cast<char16_t *>(utf16_message.c_str()));
    }

    static void Log(const std::string &caller, const std::string &message)
    {
        // Log(ExtractFunctionName(caller) + " - " + message);
        Log(caller + " - " + message);
    }

    static void LogStarted(const std::string &caller)
    {
        Log(caller, "Started");
    }

    static void LogFinished(const std::string &caller)
    {
        Log(caller, "Finished");
    }

    static void LogInput(const std::string &caller, char16_t *val)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        Log(caller, "Parameter: " + convert.to_bytes(val));
    }
    static void LogInput(const std::string &caller, uint8_t val)
    {
        Log(caller, std::string("Parameter: ") + (val ? "true" : "false"));
    }
    static void LogInput(const std::string &caller, int32_t val)
    {
        Log(caller, std::string("Parameter: ") + std::to_string(val));
    }
    static void LogInput(const std::string &caller, param_uint val)
    {
        Log(caller, "Parameter: " + std::to_string(val));
    }

    template <typename T, typename... Args>
    static void LogInput(const std::string &caller, const T &first, const Args &...args)
    {
        LogInput(caller, first);
        LogInput(caller, args...);
    }

    static void LogInput(const std::string &caller, return_value_void *returnData)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        Log(caller, "Starting: " + (returnData->error == nullptr ? "" : std::string(convert.to_bytes(returnData->error))));
    }

    static void LogInput(const std::string &caller, return_value_string *returnData)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        Log(caller, "Starting: " + std::string(convert.to_bytes(returnData->error == nullptr ? returnData->value : returnData->error)));
    }

    static void LogInput(const std::string &caller, return_value_json *returnData)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        Log(caller, "Starting: " + std::string(convert.to_bytes(returnData->error == nullptr ? returnData->value : returnData->error)));
    }

    static void LogInput(const std::string &caller, return_value_data *returnData)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        Log(caller, "Starting: " + (returnData->error == nullptr ? ("(" + to_hex(returnData->value) + ", " + std::to_string(returnData->length) + ")") : std::string(convert.to_bytes(returnData->error))));
    }

    static void LogInput(const std::string &caller, return_value_bool *returnData)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        Log(caller, "Starting: " + (returnData->error == nullptr ? returnData->value ? "true" : "false" : std::string(convert.to_bytes(returnData->error))));
    }

    static void LogInput(const std::string &caller, return_value_int32 *returnData)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        Log(caller, "Starting: " + (returnData->error == nullptr ? std::to_string(returnData->value) : std::string(convert.to_bytes(returnData->error))));
    }

    static void LogInput(const std::string &caller, return_value_uint32 *returnData)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        Log(caller, "Starting: " + (returnData->error == nullptr ? std::to_string(returnData->value) : std::string(convert.to_bytes(returnData->error))));
    }

    static void LogInput(const std::string &caller, return_value_ptr *returnData)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        Log(caller, "Starting: " + (returnData->error == nullptr ? to_hex(returnData->value) : std::string(convert.to_bytes(returnData->error))));
    }

    static void LogInput(const std::string &caller, return_value_async *returnData)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        Log(caller, "Starting: " + (returnData->error == nullptr ? "" : std::string(convert.to_bytes(returnData->error))));
    }

    static std::string to_hex(void *ptr)
    {
        std::string result;
        std::stringstream ss;
        ss << "0x" << std::hex << reinterpret_cast<size_t>(ptr);
        ss >> result;
        std::transform(result.begin(), result.end(), result.begin(), ::toupper);
        return result;
    }
};

class LoggerScope
{
    const std::string caller_;

public:
    template <typename... Args>
    LoggerScope(const std::string &caller, const Args &...args) : caller_(caller)
    {
        Logger::LogStarted(caller_);

#if DEBUG
        if constexpr (sizeof...(args) > 0)
        {
            Logger::LogInput(caller_, args...);
        }
#endif
    }

    LoggerScope(const std::string &caller) : caller_(caller)
    {
        Logger::LogStarted(caller_);
    }

    void LogError(const Napi::Error &e)
    {
        Logger::Log(caller_, "Error: " + std::string(e.Message()));
    }

    void LogException(const std::exception &e)
    {
        Logger::Log(caller_, "Exception: " + std::string(e.what()));
    }

    void Log(const std::string &message)
    {
        Logger::Log(caller_, message);
    }

    void LogResult(const std::string &message)
    {
        Logger::Log(caller_, message);
    }

    ~LoggerScope()
    {
        Logger::LogFinished(caller_);
    }
};

#endif // VE_LIB_LOGGER_GUARD_HPP_