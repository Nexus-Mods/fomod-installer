/**
 * FOMOD Installer IPC Module
 * Provides inter-process communication functionality for FOMOD installers
 * with support for multiple transport mechanisms and security levels
 */

// Core IPC connection
export {
  BaseIPCConnection,
  ConnectionStrategy,
  TimeoutOptions
} from './BaseIPCConnection';

// Transport mechanisms
export {
  ITransport,
  TransportType,
  TransportError,
  TCPTransport,
  NamedPipeTransport
} from './transport';

// Process launchers with security levels
export {
  IProcessLauncher,
  ProcessLaunchOptions,
  ChildProcessCompatible,
  RegularProcessLauncher,
  SandboxProcessLauncher,
  SandboxLauncherConfig,
  SecurityLevel
} from './launchers';

// Logging configuration
export {
  LogLevel,
  LogFunction,
  setLogger,
  resetLogger,
} from './util/log';
