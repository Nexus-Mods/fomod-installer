import * as path from 'path';
import * as os from 'os';
import { ChildProcess } from 'child_process';
import { log } from 'vortex-api';
import { SecurityLevel, IProcessLauncher, ProcessLaunchOptions, ChildProcessCompatible } from './launchers';
import { ITransport } from './transport';

/**
 * Message structure for IPC communication
 */
interface IPCMessage {
  id: string;
  payload?: {
    command: string;
    [key: string]: any;
  };
  callback?: {
    id: string;
    type: string;
  };
  data?: any;
  error?: {
    message: string;
    stack?: string;
    name?: string;
  };
}

/**
 * Callback function signature for delegate methods
 */
type DelegateCallback = (...args: any) => any | Promise<any>;

/**
 * Awaiting promise tracking structure
 */
interface AwaitingPromise {
  resolve: (data: any) => void;
  reject: (error: Error) => void;
  timeout?: NodeJS.Timeout;
  command?: string;
  dialogId?: string; // ID of timeout dialog if currently showing
}

/**
 * Configuration for a transport-launcher pair
 */
export interface ConnectionStrategy {
  transport: ITransport;
  launcher: IProcessLauncher;
}

/**
 * Options for timeout handling
 */
export interface TimeoutOptions {
  /**
   * Show a dialog to the user when timeout occurs
   * If true, onTimeoutDialog will be called
   */
  showDialog: boolean;

  /**
   * Callback to show timeout dialog to user
   * Should return true if user wants to continue, false to cancel
   */
  onTimeoutDialog?: (messageId: string, command?: string) => Promise<boolean>;

  /**
   * Callback to dismiss a currently showing timeout dialog
   */
  onDismissDialog?: (dialogId: string) => void;
}

/**
 * Generic IPC connection base class without framework-specific dependencies
 * Handles communication via pluggable transport (Named Pipe or TCP socket)
 *
 * This class can be extended to add framework-specific features like:
 * - Custom logging implementations
 * - Framework-specific file system access
 * - Framework-specific dialog systems
 * - Framework-specific path resolution
 */
export abstract class BaseIPCConnection {
  private process: ChildProcess | ChildProcessCompatible | null = null;
  private transport: ITransport | null = null;
  private launcher: IProcessLauncher | null = null;
  private strategies: ConnectionStrategy[];
  private currentStrategyIndex: number = -1;
  private pendingReplies = new Map<string, AwaitingPromise>();
  private callbacks = new Map<string, DelegateCallback>();
  private disposed = false;
  private connectionTimeout: number;
  private disconnected = false;
  private timeoutOptions: TimeoutOptions;

  /**
   * Create a new IPC connection with fallback strategies
   *
   * The connection will try each transport-launcher pair in order until one succeeds.
   * This allows automatic fallback from sandbox → regular, or named pipe → TCP.
   *
   * @param strategies Array of transport-launcher pairs to try (in order)
   * @param connectionTimeout Timeout in milliseconds (default: 10000)
   * @param timeoutOptions Options for handling timeouts
   *
   * @example
   * // Try sandbox with named pipe, fallback to regular with named pipe, then TCP
   * const connection = new BaseIPCConnection([
   *   { transport: namedPipeTransport1, launcher: sandboxLauncher },
   *   { transport: namedPipeTransport2, launcher: regularLauncher },
   *   { transport: tcpTransport, launcher: regularLauncher }
   * ], 10000, { showDialog: false });
   */
  public constructor(
    strategies: ConnectionStrategy | ConnectionStrategy[],
    connectionTimeout: number = 10000,
    timeoutOptions?: TimeoutOptions
  ) {
    // Normalize to array
    this.strategies = Array.isArray(strategies) ? strategies : [strategies];

    if (this.strategies.length === 0) {
      throw new Error('At least one connection strategy must be provided');
    }

    this.connectionTimeout = connectionTimeout;
    this.timeoutOptions = timeoutOptions || { showDialog: false };
  }

  /**
   * Check if a file exists at the given path
   */
  protected abstract fileExists(filePath: string): Promise<boolean>;

  /**
   * Get possible paths where the executable might be located
   *
   * This method should return an array of possible paths where the executable might be found.
   * The paths should be in order of preference (most likely first).
   *
   * Default implementation looks for the executable in:
   * 1. The dist folder relative to this module (distributed executable)
   * 2. Common development build locations
   *
   * Override this method to provide custom search paths.
   *
   * @param exeName Name of the executable file (e.g., 'ModInstallerIPC.exe')
   * @returns Array of absolute paths to check
   */
  protected getExecutablePaths(exeName: string): string[] {
    const paths: string[] = [];

    const distPath = path.join(__dirname, '..', '..', exeName);
    paths.push(distPath);

    return paths;
  }

  /**
   * Initialize the IPC connection by starting the process and establishing connection
   * Tries each transport-launcher strategy in order until one succeeds
   */
  public async initialize(): Promise<void> {
    const errors: Array<{ strategyIndex: number; transport: string; launcher: string; error: Error }> = [];

    for (let i = 0; i < this.strategies.length; i++) {
      const strategy = this.strategies[i];
      this.transport = strategy.transport;
      this.launcher = strategy.launcher;

      log('info', `Attempting connection strategy ${i + 1}/${this.strategies.length}`, {
        transportType: this.transport.type,
        securityLevel: this.launcher.getSecurityLevel()
      });

      try {
        await this.tryInitialize();

        // Success! Mark this strategy as active
        this.currentStrategyIndex = i;

        log('info', `Connection strategy ${i + 1} succeeded`, {
          transportType: this.transport.type,
          securityLevel: this.launcher.getSecurityLevel()
        });

        return;
      } catch (err: any) {

        log('warn', `Connection strategy ${i + 1} failed`, {
          transportType: this.transport.type,
          securityLevel: this.launcher.getSecurityLevel(),
          error: err.message
        });

        errors.push({
          strategyIndex: i,
          transport: this.transport.type,
          launcher: this.launcher.getSecurityLevel(),
          error: err
        });

        // Cleanup failed strategy before trying next
        await this.cleanupFailedStrategy();

        // Reset state for next attempt
        this.transport = null;
        this.launcher = null;
        this.process = null;
      }
    }

    // All strategies failed
    const errorDetails = errors.map(e =>
      `Strategy ${e.strategyIndex + 1} (${e.transport}/${e.launcher}): ${e.error.message}`
    ).join('; ');

    throw new Error(`All ${this.strategies.length} connection strategies failed: ${errorDetails}`);
  }

  /**
   * Attempt to initialize with the current transport and launcher
   * This is the original initialize() logic extracted into a separate method
   */
  private async tryInitialize(): Promise<void> {
    try {
      if (!this.transport || !this.launcher) {
        throw new Error('Transport or launcher not set for current strategy');
      }
      // Initialize transport and get connection identifier
      const connectionId = await this.transport.initialize();

      log('debug', 'Transport initialized', {
        type: this.transport.type,
        connectionId
      });

      // Find executable
      const exePath = await this.findExecutable();
      const cwd = path.dirname(exePath);

      // Get process arguments from transport
      const processArgs = this.transport.getProcessArgs(connectionId);

      log('debug', 'Starting process', {
        exePath,
        cwd,
        args: processArgs,
        transportType: this.transport.type
      });

      // Prepare spawn options
      const launchOptions: ProcessLaunchOptions = {
        detached: true,
        stdio: ['ignore', 'pipe', 'pipe'],
        windowsHide: false,
        cwd,
      };

      // IMPORTANT: Some transports need to create servers before launching the process
      // Launcher-specific configuration (e.g., App Container ACLs) happens automatically
      // via the launcher's constructor wrapping createServers()
      if (this.transport.createServers) {
        try {
          log('debug', 'Creating transport servers before launching process');
          await this.transport.createServers();
          log('debug', 'Transport servers created');
        } catch (err: any) {
          throw new Error(`Failed to create transport servers: ${err.message}`);
        }
      }

      // Now launch the process (any transport-specific configuration already applied)
      log('info', 'Launching process', {
        securityLevel: this.launcher.getSecurityLevel(),
        exePath,
        cwd
      });

      // Launch the process using the launcher
      this.process = await this.launcher.launch(exePath, processArgs, launchOptions);

      // Handle process output
      this.process.stdout?.on('data', (data) => {
        const text = data.toString();
        log('debug', '[Process stdout]', { data: text });
      });

      this.process.stderr?.on('data', (data) => {
        const text = data.toString();
        // Extract error information but don't kill the process
        if (text.includes('Exception') || text.includes('Error:') || text.includes('Failed to')) {
          log('error', '[Process stderr]', { data: text });
        } else {
          log('warn', '[Process stderr]', { data: text });
        }
      });

      this.process.on('exit', (code) => {
        log('info', 'Process exited', { code });
        this.handleProcessExit(code);
      });

      this.process.on('error', (err) => {
        log('error', 'Process error', { error: err.message });
        this.disconnected = true;

        // Reject all pending replies on process error
        const processError = new Error(`Process error: ${err.message}`);
        processError.name = 'ProcessError';

        for (const pending of this.pendingReplies.values()) {
          if (pending.timeout) {
            clearTimeout(pending.timeout);
          }
          pending.reject(processError);
        }
        this.pendingReplies.clear();
      });

      // Wait for C# process to connect to the pipe servers
      // For named pipes, the servers are already listening, we just need to wait for client connection
      // For TCP, waitForConnection creates the server and waits
      log('debug', 'Waiting for transport connection', {
        transportType: this.transport.type
      });

      try {
        // Don't use timeout dialog for connection - just fail and try next strategy
        await this.transport.waitForConnection(this.connectionTimeout);
        log('debug', 'Transport connection established');
      } catch (err: any) {
        throw new Error(`Failed to establish connection: ${err.message}. The process may have failed to start or connect.`);
      }

      // Read the "connected" handshake message (if implemented by C# side)
      // Note: This is optional - if C# doesn't send handshake, this will timeout
      // but we can still proceed with the connection
      let afterHandshake: string = '';
      try {
        // Short timeout since handshake is optional
        afterHandshake = await this.transport.readHandshake(1000);
        log('info', 'Handshake completed successfully');
      } catch (err: any) {
        log('debug', 'Handshake not received (optional, continuing anyway)', {
          error: err.message
        });
        // Don't throw - handshake is optional, pipes are already connected
        // If C# process doesn't send handshake, we can still communicate
      }

      // Start reading messages from the transport
      // The transport will handle buffering and delimiter parsing
      this.transport.startReceiving((messageText: string) => {
        this.processMessage(messageText).catch(err => {
          log('error', 'Error processing IPC message', { error: err.message });
        });
      });

      // If there was data after the handshake, inject it as the first message
      if (afterHandshake.length > 0) {
        log('debug', 'Processing data that came after handshake', {
          length: afterHandshake.length
        });
      }

      log('info', 'IPC connection established successfully', {
        transportType: this.transport.type
      });
    } catch (err: any) {
      log('error', 'Failed to initialize IPC connection', {
        error: err.message,
        transportType: this.transport?.type
      });

      // Don't cleanup here - let the caller (initialize()) handle it
      // This allows fallback to next strategy
      throw err;
    }
  }

  /**
   * Cleanup resources from a failed connection attempt
   * This is a lighter version of dispose() that doesn't send quit commands
   * Used when trying multiple strategies - cleans up before trying next strategy
   */
  private async cleanupFailedStrategy(): Promise<void> {
    try {
      // Dispose transport (handles socket cleanup internally)
      if (this.transport) {
        try {
          await this.transport.dispose();
        } catch (err: any) {
          log('debug', 'Error disposing transport during cleanup', { error: err.message });
        }
      }

      // Kill process if still running
      if (this.process && !this.process.killed) {
        try {
          this.process.kill();
          // Give it a moment to exit
          await new Promise<void>((resolve) => {
            const timeout = setTimeout(() => {
              try {
                this.process?.kill('SIGKILL');
              } catch (err) {
                // Process may already be dead
              }
              resolve();
            }, 1000);

            this.process?.once('exit', () => {
              clearTimeout(timeout);
              resolve();
            });
          });
        } catch (err: any) {
          log('debug', 'Error killing process during cleanup', { error: err.message });
        }
      }

      // Cleanup launcher resources (e.g., delete App Container)
      if (this.launcher) {
        try {
          await this.launcher.cleanup();
          log('debug', 'Launcher cleanup completed during fallback', {
            securityLevel: this.launcher.getSecurityLevel()
          });
        } catch (err: any) {
          log('debug', 'Error during launcher cleanup', {
            securityLevel: this.launcher.getSecurityLevel(),
            error: err.message
          });
        }
      }
    } catch (err: any) {
      // Swallow all errors during failed strategy cleanup
      log('debug', 'Error during failed strategy cleanup', { error: err.message });
    }
  }

  /**
   * Find the executable in various possible locations
   */
  private async findExecutable(): Promise<string> {
    const possiblePaths = this.getExecutablePaths('ModInstallerIPC.exe');

    // Check each path
    for (const testPath of possiblePaths) {
      try {
        const normalizedPath = path.resolve(testPath);
        const exists = await this.fileExists(normalizedPath);
        if (exists) {
          log('info', 'Found executable', { path: normalizedPath });
          return normalizedPath;
        }
      } catch (err) {
        // File doesn't exist, try next path
      }
    }

    // If not found, throw error with all attempted paths
    const errorMsg = `Executable not found. Tried paths:\n${possiblePaths.map(p => `  - ${p}`).join('\n')}`;
    log('error', errorMsg);
    throw new Error(errorMsg);
  }

  /**
   * Register a callback function that can be invoked by the server
   */
  public registerCallback(name: string, callback: DelegateCallback): void {
    this.callbacks.set(name, callback);
  }

  /**
   * Check if connection is healthy
   */
  public isConnected(): boolean {
    return !this.disconnected && this.transport !== null && this.process !== null;
  }

  /**
   * Get information about the active connection strategy
   * Returns details about which strategy is currently in use
   */
  public getActiveStrategy(): { index: number; transportType: string; securityLevel: SecurityLevel } | null {
    if (this.currentStrategyIndex === -1 || !this.transport || !this.launcher) {
      return null;
    }

    return {
      index: this.currentStrategyIndex,
      transportType: this.transport.type,
      securityLevel: this.launcher.getSecurityLevel()
    };
  }

  /**
   * Send a command to the IPC process
   * @param command Command name
   * @param payload Command payload
   * @param timeout Optional timeout (default: use connectionTimeout)
   */
  protected async sendCommand<T = any>(command: string, payload: any, timeout?: number): Promise<T> {
    const id = this.generateId();
    const message: IPCMessage = {
      id,
      payload: {
        command,
        ...payload,
      },
    };

    return await this.sendAndReceive(id, message, timeout);
  }

  /**
   * Send a message and wait for its response with timeout
   * @param id Message identifier
   * @param message Message to send
   * @param timeout Timeout in milliseconds (0 = no timeout, useful during dialog)
   */
  private async sendAndReceive(id: string, message: IPCMessage, timeout: number = this.connectionTimeout): Promise<any> {
    const promise = new Promise<any>((resolve, reject) => {
      const pending: AwaitingPromise = {
        resolve,
        reject,
        command: message.payload?.command
      };

      // Helper to schedule timeout with user dialog
      const scheduleTimeout = (ms: number) => {
        if (pending.timeout) {
          clearTimeout(pending.timeout);
        }

        pending.timeout = setTimeout(async () => {
          try {
            log('info', 'Operation timeout reached', {
              timeout: ms,
              command: message.payload?.command
            });

            // Generate unique dialog ID for this timeout dialog
            const dialogId = `ipc-timeout-${id}`;
            pending.dialogId = dialogId;

            // Check if we should show dialog and have a callback
            if (this.timeoutOptions.showDialog && this.timeoutOptions.onTimeoutDialog) {
              const shouldContinue = await this.timeoutOptions.onTimeoutDialog(dialogId, message.payload?.command);

              // Clear dialog ID after dialog completes (user responded or dialog was dismissed)
              pending.dialogId = undefined;

              if (shouldContinue) {
                log('info', 'User chose to continue waiting for response');
                // Reset timeout and continue waiting
                scheduleTimeout(ms); // Use the same timeout duration
                return; // Don't reject, keep waiting
              }

              log('info', 'User chose to cancel operation or dialog was dismissed');
            }
          } catch (err: any) {
            log('error', 'Error handling timeout', { error: err.message });
          }

          // User chose to cancel, or no dialog available - reject the promise
          this.pendingReplies.delete(id);

          const timeoutError = new Error(
            `IPC operation timed out after ${ms}ms (command: ${message.payload?.command}). ` +
            `Connection may be lost or process may be unresponsive.`
          );
          timeoutError.name = 'IPCTimeoutError';
          reject(timeoutError);
        }, ms);
      };

      // Set up timeout unless timeout is 0 (which means wait indefinitely during dialog)
      if (timeout > 0) {
        scheduleTimeout(timeout);
      }

      this.pendingReplies.set(id, pending);
    });

    // Check if already disconnected
    if (this.disconnected) {
      // Clean up pending reply before throwing
      const pending = this.pendingReplies.get(id);
      if (pending?.timeout) {
        clearTimeout(pending.timeout);
      }
      this.pendingReplies.delete(id);
      throw new Error('IPC connection has been disconnected');
    }

    try {
      await this.sendMessage(message);
    } catch (err) {
      // If sendMessage fails, clean up the pending promise to prevent unhandled rejection
      const pending = this.pendingReplies.get(id);
      if (pending?.timeout) {
        clearTimeout(pending.timeout);
      }
      this.pendingReplies.delete(id);
      throw err;
    }

    return promise;
  }

  /**
   * Send a message to the server
   */
  private async sendMessage(message: IPCMessage): Promise<void> {
    if (!this.transport) {
      throw new Error('IPC connection is not active');
    }

    const json = JSON.stringify(message);

    log('debug', 'Sending IPC message', {
      id: message.id,
      command: message.payload?.command,
      length: json.length
    });

    await this.transport.sendMessage(json);
  }

  /**
   * Process a received message
   */
  private async processMessage(messageText: string): Promise<void> {
    try {
      const message: IPCMessage = JSON.parse(messageText);

      log('debug', 'Received IPC message', {
        id: message.id,
        hasCallback: !!message.callback,
        hasData: message.data !== undefined,
        hasError: !!message.error
      });

      // Is this a callback invocation from the server?
      if (message.callback) {
        await this.handleCallback(message);
      }
      // Is this a response to our request?
      else {
        this.handleResponse(message);
      }
    } catch (err: any) {
      log('error', 'Failed to parse IPC message', { error: err.message, messageText: messageText.substring(0, 500) });
    }
  }

  /**
   * Handle a callback invocation from the server
   */
  private async handleCallback(message: IPCMessage): Promise<void> {
    const name = message.data?.name;
    const args = message.data?.args || [];

    if (!name) {
      log('warn', 'Callback message missing name', { messageId: message.id });
      return;
    }

    const callback = this.callbacks.get(name);
    if (!callback) {
      log('warn', 'No callback registered for name', { name, messageId: message.id });
      return;
    }

    try {
      const result = await callback(...args);

      // Send reply
      await this.sendReply(message.id, result, null);
    } catch (err: any) {
      // Send error reply
      await this.sendReply(message.id, null, err);
    }
  }

  /**
   * Send a reply to a callback invocation
   */
  private async sendReply(requestId: string, data: any, error: Error | null): Promise<void> {
    const replyMessage: IPCMessage = {
      id: this.generateId(),
      payload: {
        command: 'Reply',
        request: { id: requestId },
        data: data ?? {},
        error: error ? { message: error.message, name: error.name, stack: error.stack } : null,
      },
    };

    await this.sendMessage(replyMessage);
  }

  /**
   * Handle a response to our request
   */
  private handleResponse(message: IPCMessage): void {
    const pending = this.pendingReplies.get(message.id);
    if (!pending) {
      log('warn', 'Received response for unknown request', { messageId: message.id });
      return;
    }

    this.pendingReplies.delete(message.id);

    // Clear timeout if it exists
    if (pending.timeout) {
      clearTimeout(pending.timeout);
      log('debug', 'Cleared pending timeout for response', { messageId: message.id });
    }

    // Dismiss timeout dialog if it's currently showing
    // This happens when user hasn't responded to the timeout dialog yet,
    // but the installer completes (e.g., user completed an untracked Windows Forms dialog)
    if (pending.dialogId && this.timeoutOptions.onDismissDialog) {
      log('info', 'Dismissing timeout dialog as response arrived', {
        messageId: message.id,
        dialogId: pending.dialogId
      });
      this.timeoutOptions.onDismissDialog(pending.dialogId);
      pending.dialogId = undefined;
    }

    if (message.error) {
      const err = new Error(message.error.message);
      if (message.error.name) {
        err.name = message.error.name;
      }
      if (message.error.stack) {
        err.stack = message.error.stack;
      }
      pending.reject(err);
    } else {
      pending.resolve(message.data);
    }
  }

  /**
   * Handle process exit
   */
  private handleProcessExit(code: number | null): void {
    this.disconnected = true;

    // Always reject pending replies when process exits unexpectedly
    // (unless it's a clean exit with code 0)
    if (code !== 0 && code !== null) {
      const err = new Error(`Process exited unexpectedly with code ${code}`);
      err.name = 'ProcessExitError';

      log('error', 'Process exited unexpectedly', {
        code,
        pendingReplies: this.pendingReplies.size
      });

      // Reject all pending replies
      for (const [_id, pending] of this.pendingReplies.entries()) {
        // Clear timeout if it exists
        if (pending.timeout) {
          clearTimeout(pending.timeout);
        }
        pending.reject(err);
      }
      this.pendingReplies.clear();
    }
  }

  /**
   * Send quit command and dispose resources
   */
  public async dispose(): Promise<void> {
    if (this.disposed) {
      return;
    }

    this.disposed = true;

    try {
      // Send quit command if connected
      if (this.transport) {
        const quitMessage: IPCMessage = {
          id: this.generateId(),
          payload: {
            command: 'Quit',
          },
        };
        await this.sendMessage(quitMessage).catch(() => {
          // Ignore errors during shutdown
        });
      }
    } catch (err: any) {
      log('warn', 'Error sending quit command', { error: err.message });
    }

    // Dispose transport (handles socket cleanup internally)
    if (this.transport) {
      await this.transport.dispose();
      this.transport = null;
    }

    // Kill process if still running
    if (this.process && !this.process.killed) {
      try {
        this.process.kill();
        await new Promise<void>((resolve) => {
          const timeout = setTimeout(() => {
            this.process?.kill('SIGKILL');
            resolve();
          }, 2000);

          this.process?.once('exit', () => {
            clearTimeout(timeout);
            resolve();
          });
        });
      } catch (err: any) {
        log('warn', 'Error killing process', { error: err.message });
      }
    }

    this.process = null;

    // Cleanup launcher resources (e.g., delete App Container)
    if (this.launcher) {
      try {
        await this.launcher.cleanup();
        log('debug', 'Launcher cleanup completed', {
          securityLevel: this.launcher.getSecurityLevel()
        });
      } catch (err: any) {
        log('warn', 'Error during launcher cleanup', {
          securityLevel: this.launcher.getSecurityLevel(),
          error: err.message
        });
      }
    }
  }

  /**
   * Grant additional file system access to the sandboxed process
   * Only works if using a launcher that supports sandboxing
   */
  protected async grantAdditionalAccess(paths: string[]): Promise<void> {
    if (this.launcher && this.launcher.grantAdditionalAccess) {
      try {
        log('debug', 'Granting additional access', { paths });
        await this.launcher.grantAdditionalAccess(paths);
      } catch (err: any) {
        log('warn', 'Failed to grant additional access', {
          paths,
          error: err.message
        });
        // Don't throw - the operation might still work if permissions are sufficient
      }
    }
  }

  /**
   * Generate a unique message ID
   */
  private generateId(): string {
    return Math.random().toString(36).substring(2, 15) +
      Math.random().toString(36).substring(2, 15);
  }
}
