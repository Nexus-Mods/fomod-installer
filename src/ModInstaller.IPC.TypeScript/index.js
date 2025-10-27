const cp = require('child_process');
const path = require('path');
const os = require('os');

let winapi;
try {
  winapi = require('winapi-bindings');
} catch (err) {}

// Helper function to detect critical errors that might require process termination
function isCriticalError(errorText) {
  const criticalPatterns = [
    'OutOfMemoryException',
    'StackOverflowException',
    'AccessViolationException',
    'failed to connect to local port',
    'No compatible .Net Framework',
    'Unable to load assembly',
    'System.IO.FileNotFoundException',
    'System.UnauthorizedAccessException'
  ];

  // Exclude certain recoverable Windows API errors
  const recoverablePatterns = [
    'The system cannot find the file specified',
    'WinApiException',
    'Access is denied' // Sometimes this is recoverable depending on context
  ];

  if (recoverablePatterns.some(pattern => errorText.includes(pattern))) {
    return false;
  }

  return criticalPatterns.some(pattern =>
    errorText.includes(pattern)
  );
}

// Helper function to extract meaningful error information
function extractErrorInfo(text) {
  // Look for exception details
  const exceptionMatch = text.match(/(\w+Exception): (.+?)(?:\n|$)/);
  if (exceptionMatch) {
    return {
      type: exceptionMatch[1],
      message: exceptionMatch[2],
      isCritical: isCriticalError(text)
    };
  }

  // Look for "Failed to" patterns
  const failureMatch = text.match(/Failed to (.+?)(?:\n|$)/);
  if (failureMatch) {
    return {
      type: 'Failure',
      message: `Failed to ${failureMatch[1]}`,
      isCritical: isCriticalError(text)
    };
  }

  return {
    type: 'Unknown',
    message: text.trim(),
    isCritical: isCriticalError(text)
  };
}

async function startRegular(exePath, cwd, args, onExit, onStdout) {
  return new Promise((resolve, reject) => {
    // Configure spawn options for resource isolation
    const spawnOptions = {
      detached: true,
      stdio: ['ignore', 'pipe', 'pipe'],
      windowsHide: true,
      env: {
        PATH: process.env.PATH,
        TEMP: process.env.TEMP || process.env.TMP || os.tmpdir(),
        TMP: process.env.TMP || process.env.TEMP || os.tmpdir(),
        USERPROFILE: process.env.USERPROFILE,
        APPDATA: process.env.APPDATA,
        LOCALAPPDATA: process.env.LOCALAPPDATA,
        // Add .NET specific environment variables to help with path resolution
        DOTNET_BUNDLE_EXTRACT_BASE_DIR: cwd || path.dirname(exePath),
        COMPlus_EnableDiagnostics: '0', // Disable diagnostics which can cause issues
        // Add .NET specific environment variables if they exist
        ...(process.env.DOTNET_ROOT && { DOTNET_ROOT: process.env.DOTNET_ROOT }),
        ...(process.env.DOTNET_HOST_PATH && { DOTNET_HOST_PATH: process.env.DOTNET_HOST_PATH }),
        ...(process.env.DOTNET_SYSTEM_GLOBALIZATION_INVARIANT && {
          DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: process.env.DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
        }),
      },
      cwd: cwd || path.dirname(exePath),
    };

    const proc = cp.spawn(exePath, args, spawnOptions)
      .on('error', err => {
        reject?.(err);
        resolve = reject = undefined;
      })
      .on('exit', (code, signal) => {
        if (code === 0x80131700) {
          reject?.(new Error('No compatible .Net Framework, you need .Net framework 9.0 or newer'));
        } else if (code !== null) {
          reject?.(new Error(`Failed to run fomod installer. Errorcode ${code.toString(16)}`));
        } else {
          reject?.(new Error(`The fomod installer was terminated. Signal: ${signal}`));
        }
        resolve = reject = undefined;
        onExit(code);
      });

    proc.stdout.on('data', dat => onStdout(dat.toString()));
    proc.stderr.on('data', dat => {
      const stderrData = dat.toString();
      const errorInfo = extractErrorInfo(stderrData);

      // Send error information to stdout for the caller to handle
      onStdout(stderrData);

      // Log the error for debugging but don't kill the process
      if (errorInfo.isCritical) {
        console.error(`CRITICAL ERROR in process ${proc.pid} [${errorInfo.type}]:`, errorInfo.message);
      } else {
        console.warn(`Process ${proc.pid} stderr [${errorInfo.type}]:`, errorInfo.message);
      }
    });

    // resolve slightly delayed to allow the error event to be triggered if the process fails to
    // start. Unfortunately cp.spawn seems to flip a coin on whether it reports events at all or not.
    setTimeout(() => {
      if ((proc.exitCode !== null) && (proc.exitCode !== 0)) {
        reject?.(new Error('Failed to spawn fomod installer'));
      } else {
        resolve?.(proc.pid);
      }
      resolve = reject = undefined;
    }, 100);
  });
}

// Keep track of containers that already have exit listeners
const containersWithListeners = new Set();
async function startSandboxed(containerName, id, exePath, cwd, args, onExit, onStdout) {
  return new Promise((resolve, reject) => {
    // Use setImmediate to move blocking Windows API calls off the main event loop
    setImmediate(async () => {
      try {
        await new Promise((resolveStep) => {
          setImmediate(() => {
            try {
              winapi.DeleteAppContainer(containerName);
            } catch (err) {
              if (err.nativeCode !== 0 && !err.message?.includes('does not exist')) {
                console.warn(`Could not delete existing app container:`, err.message);
              }
            }
            resolveStep();
          });
        });

        await new Promise((resolveStep) => {
          setImmediate(() => {
            try {
              winapi.CreateAppContainer(containerName, 'FOMOD', 'Container for fomod installers');
            } catch (err) {
              if (err.nativeCode !== 0) {
                console.warn(`Could not create app container:`, err.message);
                throw err;
              }
            }
            resolveStep();
          });
        });

        // Only add exit listener if we haven't already added one for this container
        if (!containersWithListeners.has(containerName)) {
          const exitHandler = () => {
            setImmediate(() => {
              try {
                winapi.DeleteAppContainer(containerName);
              } catch (err) {
                // Silent cleanup - errors during shutdown are not critical
              }
            });
            containersWithListeners.delete(containerName);
          };
          process.on('exit', exitHandler);
          containersWithListeners.add(containerName);
        }

        // Ensure the executable path uses proper Windows format first
        const windowsExePath = exePath.replace(/\//g, '\\');
        const windowsCwd = cwd.replace(/\//g, '\\');

        const permissions = [
          { path: `\\\\?\\pipe\\${id}`, type: 'named_pipe', access: ['all_access'] },
          { path: `\\\\?\\pipe\\${id}_reply`, type: 'named_pipe', access: ['all_access'] },
          // Add permissions for the executable and its directory
          { path: windowsExePath, type: 'file_object', access: ['read_execute'] },
          { path: windowsCwd, type: 'file_object', access: ['read_execute'] },
          // Add temp directory access for .NET runtime
          { path: process.env.TEMP || process.env.TMP, type: 'file_object', access: ['all_access'] },
        ];

        // Grant permissions in batches to avoid blocking
        for (const perm of permissions) {
          if (!perm.path) continue; // Skip if path is undefined
          await new Promise((resolveStep) => {
            setImmediate(() => {
              try {
                winapi.GrantAppContainer(containerName, perm.path, perm.type, perm.access);
              } catch (err) {
                if (err.nativeCode !== 0) {
                  console.warn(`Could not grant ${perm.type} access for ${perm.path}:`, err.message);
                }
              }
              resolveStep();
            });
          });
        }

        // Build command with properly escaped path (windowsExePath already defined above)
        const command = `"${windowsExePath}" ${args.join(' ')}`;

        let resolvedPid;
        await new Promise((resolveStep, rejectStep) => {
          setImmediate(() => {
            try {
              resolvedPid = winapi.RunInContainer(containerName, command, windowsCwd, onExit, onStdout);
              resolveStep();
            } catch (err) {
              // I think this may be caused by an AV scanning the files after we copied them
              setTimeout(() => {
                setImmediate(() => {
                  try {
                    resolvedPid = winapi.RunInContainer(containerName, command, cwd, onExit, onStdout);
                    resolveStep();
                  } catch (retryErr) {
                    rejectStep(retryErr);
                  }
                });
              }, 1000);
            }
          });
        });

        resolve(resolvedPid);
      } catch (err) {
        reject(err);
      }
    });
  });
}

async function startLowIntegrity(exePath, cwd, args, onExit, onStdout) {
  return new Promise((resolve, reject) => {
    // Use setImmediate to move blocking Windows API call off the main event loop
    setImmediate(() => {
      try {
        // Ensure Windows path format
        const windowsExePath = exePath.replace(/\//g, '\\');
        const windowsCwd = cwd.replace(/\//g, '\\');

        // Prepare command with proper path quoting for security
        const command = `"${windowsExePath}" ${args.join(' ')}`;

        // This is a potentially blocking operation, so we've moved it to setImmediate
        const pid = winapi.CreateProcessWithIntegrity(command, windowsCwd, 'low', onExit, onStdout);
        resolve(pid);
      } catch (err) {
        reject(err);
      }
    });
  });
}

async function createIPC(usePipe, id, onExit, onStdout, containerName, lowIntegrity) {
  // it does actually get named .exe on linux as well
  const exeName = 'ModInstallerIPC.exe';

  // Better path resolution for development and production
  let baseDir = __dirname;
  if (baseDir.includes('app.asar')) {
    baseDir = baseDir.replace('app.asar', 'app.asar.unpacked');
  }

  let cwd = path.join(baseDir, 'dist');
  let exePath = path.join(cwd, exeName);

  // Ensure absolute paths on Windows
  if (process.platform === 'win32') {
    exePath = path.resolve(exePath);
    cwd = path.resolve(cwd);

    // Normalize path separators for Windows
    exePath = exePath.replace(/\//g, '\\');
    cwd = cwd.replace(/\//g, '\\');
  }

  // Verify the executable exists
  const fs = require('fs');
  if (!fs.existsSync(exePath)) {
    const alternativePath = path.join(process.cwd(), 'node_modules', 'fomod-installer', 'dist', exeName);
    if (fs.existsSync(alternativePath)) {
      console.warn(`Using alternative path for ModInstallerIPC.exe: ${alternativePath}`);
      exePath = alternativePath;
      cwd = path.dirname(exePath);
    } else {
      console.error(`ModInstallerIPC.exe not found at expected paths:`);
      console.error(`  Primary: ${exePath}`);
      console.error(`  Alternative: ${alternativePath}`);
      console.error(`  __dirname: ${__dirname}`);
      console.error(`  process.cwd(): ${process.cwd()}`);
    }
  }

  const args = [id];
  if (usePipe) {
    args.push('--pipe');
  }

  const enhancedOnStdout = (data) => {
    if (data.includes('[ERROR:') || data.includes('Exception') || data.includes('Error:') ||
        data.includes('Failed to') || data.includes('Could not') || data.includes('Unable to')) {

      const errorInfo = extractErrorInfo(data);
      if (errorInfo.isCritical) {
        console.error(`CRITICAL ERROR detected in ModInstallerIPC process:`, errorInfo.message);
      } else {
        // Non-critical errors are logged in the Vortex repo itself - no point spamming the console.
        //  leaving this here for now for future debugging.
        // console.warn(`Error detected in ModInstallerIPC process:`, errorInfo.message);
      }
    }
    // Always pass through the output to the caller
    onStdout(data.toString());
  };

  if (winapi !== undefined) {
    if ((winapi?.SupportsAppContainer?.() === true) && (containerName !== undefined)) {
      try {
        return await startSandboxed(containerName, id, exePath, cwd, args, onExit, enhancedOnStdout);
      } catch (err) {
        console.warn('Failed to start in sandbox mode, falling back to regular mode:', err.message);
        // Fall through to regular mode
      }
    } else if (lowIntegrity) {
      try {
        return await startLowIntegrity(exePath, cwd, args, onExit, enhancedOnStdout);
      } catch (err) {
        console.warn('Failed to start with low integrity, falling back to regular mode:', err.message);
        // Fall through to regular mode
      }
    }
  }
  // fallback for other OSes and if the above solutions are disabled
  return await startRegular(exePath, cwd, args, onExit, enhancedOnStdout);
}

// Function to manually kill a specific process by PID
// This is kept for external use by index.ts process management (i.e. fomod_installer extension in the main repo)
function killProcess(pid) {
  try {
    process.kill(pid, 'SIGTERM');
    setTimeout(() => {
      try {
        process.kill(pid, 'SIGKILL');
      } catch (err) {
        // no-op
      }
    }, 2000);
    return true;
  } catch (err) {
    // Try Windows API if available
    if (winapi?.TerminateProcess) {
      try {
        winapi.TerminateProcess(pid);
        return true;
      } catch (winapiErr) {
        console.warn(`Failed to kill process ${pid} via WinAPI:`, winapiErr.message);
      }
    }

    try {
      const cp = require('child_process');
      cp.exec(`taskkill /F /PID ${pid}`, (error) => {
        if (error) {
          console.warn(`Failed to kill process ${pid} via taskkill:`, error.message);
        }
      });
      return true;
    } catch (fallbackErr) {
      console.warn(`All methods failed to kill process ${pid}:`, err.message);
      return false;
    }
  }
}

module.exports = {
  __esModule: true,
  createIPC,
  killProcess,
};
