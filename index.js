const cp = require('child_process');
const path = require('path');

async function createIPC(port) {
  // it does actually get named .exe on linux as well
  const exeName = 'ModInstallerIPC.exe';

  return new Promise((resolve, reject) => {
    const proc = cp.spawn(path.join(__dirname, 'dist', exeName), [port.toString()]);
    proc.on('error', err => {
      reject(err);
    });
    proc.on('exit', code => {
      if (code === 0x80131700) {
        reject(new Error('No compatible .Net Framework, you need .Net framework 4.6 or newer'));
      }
      reject(new Error(`Failed to run fomod installer. Errorcode ${code.toString(16)}`));
    });
    proc.stdout.on('data', (dat) => {
      if (resolve !== undefined) {
        // the process should log to the console once to signal it started
        resolve(proc);
        resolve = undefined;
      } else {
        console.log('fwd: ', dat.toString());
      }
    });
  });
}

module.exports = {
  __esModule: true,
  createIPC,
};
