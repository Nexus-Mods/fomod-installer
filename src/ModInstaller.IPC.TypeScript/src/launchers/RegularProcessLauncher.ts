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
    // On Linux, use Mono to run .exe files
    let actualExePath = exePath;
    let actualArgs = args;

    if (process.platform !== 'win32' && exePath.toLowerCase().endsWith('.exe')) {
      actualExePath = 'mono';
      actualArgs = [exePath, ...args];

      log('info', '[PROCESS] Using Mono to launch .exe on non-Windows platform', {
        originalExePath: exePath,
        monoPath: actualExePath
      });
    }

    log('info', '[PROCESS] Launching process with regular security', {
      exePath: actualExePath,
      args: actualArgs,
      argsJoined: actualArgs.join(' '),
      cwd: options.cwd
    });

    const childProcess = spawn(actualExePath, actualArgs, options);

    log('info', '[PROCESS] Process launched successfully (regular security)', {
      pid: childProcess.pid
    });

    // Log stdout
    if (childProcess.stdout) {
      childProcess.stdout.on('data', (data: Buffer) => {
        const output = data.toString().trim();
        if (output) {
          log('info', '[PROCESS] STDOUT', {
            pid: childProcess.pid,
            output: output
          });
        }
      });
    }

    // Log stderr
    if (childProcess.stderr) {
      childProcess.stderr.on('data', (data: Buffer) => {
        const output = data.toString().trim();
        if (output) {
          log('warn', '[PROCESS] STDERR', {
            pid: childProcess.pid,
            output: output
          });
        }
      });
    }

    // Log process exit
    childProcess.on('exit', (code, signal) => {
      log('info', '[PROCESS] Process exited', {
        pid: childProcess.pid,
        exitCode: code,
        signal: signal
      });
    });

    // Log process errors
    childProcess.on('error', (err) => {
      log('error', '[PROCESS] Process error', {
        pid: childProcess.pid,
        error: err.message,
        stack: err.stack
      });
    });

    return childProcess;
  }

  public async cleanup(): Promise<void> {
    // No cleanup needed for regular processes
  }
}
