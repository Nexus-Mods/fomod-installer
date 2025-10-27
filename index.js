/**
 * FOMOD Installer Meta Package
 *
 * This package provides access to both:
 * - Native bindings (fomod-installer-native)
 * - IPC-based installer (fomod-installer-ipc)
 */

// Try to load the native bindings package
let native;
try {
  native = require('fomod-installer-native');
} catch (err) {
  console.warn('Native bindings not available:', err.message);
  native = null;
}

// Try to load the IPC package
let ipc;
try {
  ipc = require('fomod-installer-ipc');
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
  ...(ipc || {}),
};

// Also allow destructured imports
module.exports.default = module.exports;
