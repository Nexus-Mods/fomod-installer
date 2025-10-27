/**
 * FOMOD Installer Meta Package Type Definitions
 *
 * This package provides access to both:
 * - Native bindings (fomod-installer-native)
 * - IPC-based installer (fomod-installer)
 */

// Re-export types from native package
export * as Native from './src/ModInstaller.Native.TypeScript/dist/main/lib/index';

// Re-export types from IPC package
export * from './src/ModInstaller.IPC.TypeScript/index';

// Default export structure
declare const fomodInstaller: {
  /**
   * Native N-API bindings for FOMOD installation
   */
  native: typeof import('./src/ModInstaller.Native.TypeScript/dist/main/lib/index') | null;

  /**
   * IPC-based FOMOD installer
   */
  ipc: typeof import('./src/ModInstaller.IPC.TypeScript/index') | null;
} & typeof import('./src/ModInstaller.IPC.TypeScript/index');

export default fomodInstaller;
