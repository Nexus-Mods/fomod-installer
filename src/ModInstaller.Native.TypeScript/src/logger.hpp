#ifndef VE_LIB_LOGGER_GUARD_HPP_
#define VE_LIB_LOGGER_GUARD_HPP_

#define LOGGING_

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
#include <cstdint>
#include <codecvt>
#include <locale>

#include <iostream>
#include <fstream>
#include <string>
#include <chrono>

// Define this before <windows.h> to prevent winsock.h conflicts
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>
#include <ctime>
#include <iomanip>
#include <sstream>

using namespace Napi;

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
#ifdef LOGGING
        HANDLE mutex = OpenMutexW(SYNCHRONIZE, FALSE, _mutexName.c_str());
        if (!mutex)
            mutex = CreateMutexW(NULL, FALSE, _mutexName.c_str());

        if (mutex)
        {
            while (true)
            {
                DWORD waitResult = WaitForSingleObject(mutex, 100); // 100 ms timeout
                if (waitResult == WAIT_OBJECT_0)
                {
                    try
                    {
                        std::ofstream fs(_logFilePath, std::ios::app);
                        if (fs.is_open())
                        {
                            auto now = std::chrono::system_clock::now();
                            auto in_time_t = std::chrono::system_clock::to_time_t(now);

                            std::tm buf;
                            gmtime_s(&buf, &in_time_t);

                            char timeStr[100];
                            strftime(timeStr, sizeof(timeStr), "%Y-%m-%d %H:%M:%S", &buf);

                            auto milliseconds = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()).count() % 1000;

                            fs << "[C++][" << timeStr << "." << std::setfill('0') << std::setw(3) << milliseconds << "] " << message << std::endl;
                        }
                    }
                    catch (const std::exception &)
                    {
                        // Ignore exceptions and retry
                    }
                    ReleaseMutex(mutex);
                    break;
                }
            }
            CloseHandle(mutex);
        }
#endif
    }

    static void Log(const std::string &caller, const std::string &message)
    {
        // Log(ExtractFunctionName(caller) + " - " + message);
        Log(caller + " - " + message);
    }

    static void LogInput(const std::string &caller)
    {
        Log(caller, "Starting");
    }

    static void LogOutput(const std::string &caller)
    {
        Log(caller, "Finished");
    }

    static void LogInput(const std::string &caller, char16_t *val)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        Log(caller, "Starting: " + convert.to_bytes(val));
    }
    static void LogInput(const std::string &caller, uint8_t val)
    {
        Log(caller, "Starting: " + val ? "true" : "false");
    }
    static void LogInput(const std::string &caller, int32_t val)
    {
        Log(caller, "Starting: " + std::to_string(val));
    }
    static void LogInput(const std::string &caller, param_uint val)
    {
        Log(caller, "Starting: " + std::to_string(val));
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
    template <typename T, typename... Args>
    LoggerScope(const std::string &caller, const T &first, const Args &...args) : caller_(caller)
    {
        Logger::LogInput(caller_, first);
        if constexpr (sizeof...(args) > 0)
        {
            Logger::LogInput(caller_, args...);
        }
    }

    LoggerScope(const std::string &caller) : caller_(caller)
    {
        Logger::LogInput(caller_);
    }

    void Log(const std::string &message)
    {
        Logger::Log(caller_, message);
    }

    ~LoggerScope()
    {
        Logger::LogOutput(caller_);
    }
};

const std::string Logger::_logFilePath = "D:\\Git\\FOMOD.ModInstaller.log";
const std::wstring Logger::_mutexName = L"Global\\FOMODLoggerMutex";

#endif
