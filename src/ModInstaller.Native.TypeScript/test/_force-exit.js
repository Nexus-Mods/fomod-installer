/**
 * Force process exit after tests complete to avoid hanging on Native AOT cleanup.
 *
 * .NET Native AOT libraries do not support proper unloading, which can cause:
 * - Segmentation faults on Linux (exit code 139)
 * - Process hanging on Windows (AVA "Failed to exit")
 *
 * This file forces the process to exit after tests complete, bypassing the
 * problematic cleanup phase.
 *
 * References:
 * - https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/libraries
 */

process.on('beforeExit', () => {
  // Force immediate exit to prevent Native AOT unload issues
  process.exit(0);
});
