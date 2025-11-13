/**
 * Configurable logger for IPC module
 * Allows injection of custom log function (e.g., Vortex's log system)
 */

export type LogLevel = 'debug' | 'info' | 'warn' | 'error';

/**
 * Log function signature
 */
export type LogFunction = (level: LogLevel, message: string, metadata?: any) => void;

/**
 * Default console-based logger (fallback)
 */
const defaultLogger: LogFunction = (level: LogLevel, message: string, metadata?: any): void => {
  const timestamp = new Date().toISOString();
  const prefix = `[${timestamp}] [${level.toUpperCase()}]`;

  if (metadata !== undefined) {
    console.log(`${prefix} ${message}`, metadata);
  } else {
    console.log(`${prefix} ${message}`);
  }
};

/**
 * Current logger instance (can be overridden by setLogger)
 */
let currentLogger: LogFunction = defaultLogger;

/**
 * Set a custom log function
 * Use this to inject Vortex's log system or any other logging implementation
 *
 * @example
 * // In Vortex extension:
 * import { setLogger } from 'fomod-installer-ipc';
 * import { log as vortexLog } from '../../../util/log';
 *
 * setLogger(vortexLog);
 */
export function setLogger(logFn: LogFunction): void {
  currentLogger = logFn;
}

/**
 * Reset to default console logger
 */
export function resetLogger(): void {
  currentLogger = defaultLogger;
}

/**
 * Log a message with a given level
 * @param level - Log level
 * @param message - Message to log
 * @param metadata - Optional metadata to include
 */
export function log(level: LogLevel, message: string, metadata?: any): void {
  currentLogger(level, message, metadata);
}
