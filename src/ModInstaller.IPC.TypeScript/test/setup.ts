// Setup file for AVA tests
// This must be run before the tests to mock modules that require Electron

import * as Module from 'module';

// Save original require
const originalRequire = (Module as any).prototype.require;

// Mock vortex-api module
const vortexApiMock = {
  log: (level: string, message: string, metadata?: any): void => {
    if (process.env.DEBUG_IPC) {
      console.log(`[${level}] ${message}`, metadata ? JSON.stringify(metadata) : '');
    }
  }
};

// Override require to intercept vortex-api
(Module as any).prototype.require = function(id: string) {
  if (id === 'vortex-api') {
    return vortexApiMock;
  }
  return originalRequire.apply(this, arguments);
};
