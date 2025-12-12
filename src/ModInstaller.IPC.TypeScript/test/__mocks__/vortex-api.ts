// Mock for vortex-api that doesn't require Electron

export function log(level: string, message: string, metadata?: any): void {
  if (process.env.DEBUG_IPC) {
    console.log(`[${level}] ${message}`, metadata ? JSON.stringify(metadata) : '');
  }
}

export default {
  log
};
