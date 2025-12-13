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

if (process.platform === 'linux') {
  const ava = require('ava');

  ava.registerCompletionHandler(() => {
    // Force immediate exit on Linux to avoid Native AOT unload segfault
    process.exit(0);
  });
}
