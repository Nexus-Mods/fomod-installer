import { spawn, ChildProcess } from 'child_process';
import { log } from 'vortex-api';
import { IProcessLauncher, ProcessLaunchOptions } from './IProcessLauncher';
import { SecurityLevel } from './SecurityLevel';

/**
 * Regular process launcher - no security restrictions
 * Used as fallback when sandboxing is unavailable or disabled
 */
export class RegularProcessLauncher implements IProcessLauncher {
  public getSecurityLevel(): SecurityLevel {
    return SecurityLevel.Regular;
  }

  public async launch(
    exePath: string,
    args: string[],
    options: ProcessLaunchOptions
  ): Promise<ChildProcess> {
    log('info', '[PROCESS] Launching process with regular security', {
      exePath,
      args,
      argsJoined: args.join(' '),
      cwd: options.cwd
    });

    const process = spawn(exePath, args, options);

    log('info', '[PROCESS] Process launched successfully (regular security)', {
      pid: process.pid
    });

    // Log stdout
    if (process.stdout) {
      process.stdout.on('data', (data: Buffer) => {
        const output = data.toString().trim();
        if (output) {
          log('info', '[PROCESS] STDOUT', {
            pid: process.pid,
            output: output
          });
        }
      });
    }

    // Log stderr
    if (process.stderr) {
      process.stderr.on('data', (data: Buffer) => {
        const output = data.toString().trim();
        if (output) {
          log('warn', '[PROCESS] STDERR', {
            pid: process.pid,
            output: output
          });
        }
      });
    }

    // Log process exit
    process.on('exit', (code, signal) => {
      log('info', '[PROCESS] Process exited', {
        pid: process.pid,
        exitCode: code,
        signal: signal
      });
    });

    // Log process errors
    process.on('error', (err) => {
      log('error', '[PROCESS] Process error', {
        pid: process.pid,
        error: err.message,
        stack: err.stack
      });
    });

    return process;
  }

  public async cleanup(): Promise<void> {
    // No cleanup needed for regular processes
  }
}
