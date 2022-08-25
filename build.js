const cp = require('child_process');
const fs = require('fs');
const path = require('path');
const fsExtra = require('fs-extra');

const debugBuild = false;

class MissingDotNetSDKException extends Error {
  constructor() {
    super('Missing .NET SDK - Install a .NET SDK from https://dotnet.microsoft.com/en-us/download/dotnet/6.0');
    this.name = 'MissingDotNetSDKException';
  }
}

const ERROR_CODE_HANDLER = {
  2147516561: {
    genError: () => new MissingDotNetSDKException(),
  },
}

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
    const buildType = debugBuild ? 'Debug' : 'Release';
    // await dotnet(['restore']);
    // await dotnet(['build', '-c', buildType]);
    await fsExtra.remove(path.join(__dirname, 'dist'));
    // const args = ['publish', '-c', buildType, '--self-contained', '-r', 'win-x64', '-o', 'dist'];
    const args = ['publish', '-c', buildType, '--no-self-contained', '-r', 'win-x64', '-o', 'dist'];
    if (!debugBuild) {
      args.push('/p:DebugType=None', '/p:DebugSymbols=false');
    }
    await dotnet(args);
    // await dotnet(['publish', '-c', 'Release', '-a', 'x64', '-o', 'dist']);
    await sign('dist\\ModInstallerIPC.exe');
  } catch (err) {
    const error = ERROR_CODE_HANDLER[err?.code] !== undefined
      ? ERROR_CODE_HANDLER[err.code].genError() : err;
    console.error('build failed:', error.message);
    const exitCode = !err.code ? -1 : err.code;
    process.exit(exitCode);
  }
}

main();
