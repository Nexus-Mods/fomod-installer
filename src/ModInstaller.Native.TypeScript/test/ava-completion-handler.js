/**
 * AVA completion handler to force process exit.
 *
 * This prevents segmentation faults on Linux caused by Node.js trying to unload
 * the Native AOT library during process shutdown. .NET Native AOT libraries
 * do not support proper unloading (dlclose), which can cause segfaults.
 *
 * References:
 * - https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/libraries
 * - https://github.com/avajs/ava/blob/main/docs/08-common-pitfalls.md
 */

// On Linux, add an exit handler that forces process exit after a short delay
// This prevents Node.js from attempting to cleanly unload the Native AOT library
if (process.platform === 'linux') {
  process.on('beforeExit', () => {
    // Force immediate exit to prevent Native AOT unload segfault
    process.exit(0);
  });
}
