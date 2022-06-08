var cp = require('child_process');
var fs = require('fs');
var path = require('path');
var fsExtra = require('fs-extra');


function spawnAsync(exe, args, options, out) {
  if (options === undefined) {
    options = {};
  }
  if (out === undefined) {
    out = console;
  }
  return new Promise((resolve, reject) => {
    let desc = `${options.cwd || '.'}/${exe} ${args.join(' ')}`;
    out.log('started: ' + desc);
    const outBufs = [];
    try {
      let proc = cp.spawn(exe, args, options);
      proc.stdout.on('data', (data) => outBufs.push(data));
      proc.stderr.on('data', (data) => out.error(data.toString()));
      proc.on('error', (err) => {
        out.log(Buffer.concat(outBufs).toString());
        reject(err);
      });
      proc.on('close', (code) => {
        out.log('done: ' + desc + ': ' + code);
        if (code === 0) {
          resolve();
        } else {
          out.log(Buffer.concat(outBufs).toString());
          reject(new Error(`${desc} failed with code ${code}`));
        }
      });
    } catch (err) {
      out.error(`failed to spawn ${desc}: ${err.message}`);
      reject(err);
    }
  });
}

async function dotnet(args) {
  return spawnAsync('dotnet', args, {}, console);
}

function sign(filePath) {
  if (process.env['SIGN_TOOL'] !== undefined) {
    return spawnAsync(process.env['SIGN_TOOL'], ['sign', '/sha1', process.env['SIGN_THUMBPRINT'], '/t', 'http://timestamp.verisign.com/scripts/timestamp.dll', filePath]);
  }
}

async function main() {
  try {
    await dotnet(['restore']);
    await dotnet(['build', '-c', 'Release']);
    await fsExtra.remove(path.join(__dirname, 'dist'));
    await dotnet(['publish', '-c', 'Release', '--self-contained', '-r', 'win-x64', '-o', 'dist']);
    // await dotnet(['publish', '-c', 'Release', '-a', 'x64', '-o', 'dist']);
    await sign('dist\\ModInstallerIPC.exe');
  } catch (err) {
    console.error('failed', err.message);
  }
}

main();
