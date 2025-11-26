#ifndef PLATFORM_HPP
#define PLATFORM_HPP

// Platform-specific calling convention
// On Linux/Mac, __cdecl is not supported but we can't just make it empty
// because the generated header uses it in function pointer syntax like:
//   void (__cdecl *)(params)
// If __cdecl becomes empty, this becomes void (*)(params) which is fine,
// but if it's void (__cdecl )(params) it becomes void ()(params) which is invalid.
// Solution: On non-Windows, define __cdecl as __attribute__((cdecl)) which GCC ignores
#ifdef _WIN32
    // Windows already has __cdecl defined by the compiler
#else
    // Linux/Mac: define __cdecl as something GCC will accept and ignore
    #ifndef __cdecl
        #define __cdecl __attribute__((__cdecl__))
    #endif
#endif

#endif // PLATFORM_HPP
