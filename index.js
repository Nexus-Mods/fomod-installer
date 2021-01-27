const cp = require('child_process');
const path = require('path');

async function createIPC(usePipe, id, cb) {
  // it does actually get named .exe on linux as well
  const exeName = 'ModInstallerIPC.exe';

  return new Promise((resolve, reject) => {
    const args = [id];
    if (usePipe) {
      args.push('--pipe');
    }
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
      });
    cb(proc);

    // resolve slightly delayed to allow the error event to be triggered if the process fails to
    // start. Unfortunately cp.spawn seems to flip a coin on whether it reports events at all or not.
    setTimeout(() => {
      if ((proc.exitCode !== null) && (proc.exitCode !== 0)) {
        reject?.(new Error('Failed to spawn fomod installer'));
      } else {
        resolve?.(proc);
      }
      resolve = reject = undefined;
    }, 100);
  });
}

module.exports = {
  __esModule: true,
  createIPC,
};
