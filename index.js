const cp = require('child_process');
const path = require('path');

const CONTAINER_NAME = 'fomod_installer';

let winapi;
try {
  winapi = require('winapi-bindings');
} catch (err) {}

async function createIPC(usePipe, id, onExit, onStdout) {
  // it does actually get named .exe on linux as well
  const exeName = 'ModInstallerIPC.exe';

  const exePath = path.join(__dirname, 'dist', exeName);

  const args = [id];
  if (usePipe) {
    args.push('--pipe');
  }

  if (winapi !== undefined) {
    return new Promise((resolve, reject) => {
      // in case the container wasn't cleaned up before
      try {
        winapi.DeleteAppContainer(CONTAINER_NAME);
        winapi.CreateAppContainer(CONTAINER_NAME, 'FOMOD', 'Container for fomod installers');
        process.on('exit', () => {
          winapi.DeleteAppContainer(CONTAINER_NAME);
        });
        winapi.GrantAppContainer(CONTAINER_NAME, path.join(__dirname, 'dist'), 'file_object', ['generic_execute', 'list_directory']);
        winapi.GrantAppContainer(CONTAINER_NAME, `\\\\?\\pipe\\${id}`, 'named_pipe', ['all_access']);
        winapi.GrantAppContainer(CONTAINER_NAME, `\\\\?\\pipe\\${id}_reply`, 'named_pipe', ['all_access']);

        const pid = winapi.RunInContainer(CONTAINER_NAME, `${exePath} ${args.join(' ')}`, __dirname, onExit, onStdout);
        resolve(pid);
      } catch (err) {
        reject(err);
      }
    });
  } else {
    return new Promise((resolve, reject) => {
      const proc = cp.spawn(path.join(__dirname, 'dist', exeName), args)
        .on('error', err => {
          reject?.(err);
          resolve = reject = undefined;
        })
        .on('exit', (code, signal) => {
          if (code === 0x80131700) {
            reject?.(new Error('No compatible .Net Framework, you need .Net framework 4.6 or newer'));
          } else if (code !== null) {
            reject?.(new Error(`Failed to run fomod installer. Errorcode ${code.toString(16)}`));
          } else {
            reject?.(new Error(`The fomod installer was terminated. Signal: ${signal}`));
          }
          resolve = reject = undefined;
          onExit(code);
        });

      proc.stdout.on('data', dat => dat.toString());
      proc.stderr.on('data', dat => dat.toString());

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
}

module.exports = {
  __esModule: true,
  createIPC,
};
