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
