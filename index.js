/**
 * FOMOD Installer Meta Package
 *
 * This package provides access to both:
 * - Native bindings (fomod-installer-native)
 * - IPC-based installer (fomod-installer)
 */

const path = require('path');

// Export the native bindings package
const nativePath = path.join(__dirname, 'src', 'ModInstaller.Native.TypeScript');
let native;
try {
  native = require(nativePath);
} catch (err) {
  console.warn('Native bindings not available:', err.message);
  native = null;
}

// Export the IPC package
const ipcPath = path.join(__dirname, 'src', 'ModInstaller.IPC.TypeScript');
let ipc;
try {
  ipc = require(ipcPath);
} catch (err) {
  console.warn('IPC package not available:', err.message);
  ipc = null;
}

// Export both packages
module.exports = {
  // Native bindings (N-API)
  native: native,

  // IPC-based installer
  ipc: ipc,

  // For backward compatibility, default export is IPC
  ...ipc,
};

// Also allow destructured imports
module.exports.default = module.exports;
