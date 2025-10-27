/**
 * FOMOD Installer Meta Package Type Definitions
 *
 * This package provides access to both:
 * - Native bindings (fomod-installer-native)
 * - IPC-based installer (fomod-installer-ipc)
 */

// Import types from dependencies
import * as NativeModule from 'fomod-installer-native';
import * as IpcModule from 'fomod-installer-ipc';

// Re-export as namespaces
export { NativeModule as Native };
export * from 'fomod-installer-ipc';

// Default export structure
declare const fomodInstaller: {
  /**
   * Native N-API bindings for FOMOD installation
   */
  native: typeof NativeModule | null;

  /**
   * IPC-based FOMOD installer
   */
  ipc: typeof IpcModule | null;
} & typeof IpcModule;

export default fomodInstaller;
