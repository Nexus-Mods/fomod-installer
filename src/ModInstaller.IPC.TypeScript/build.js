#!/usr/bin/env node

/**
 * Build script for fomod-installer-ipc
 * Builds TypeScript sources for both CommonJS and ES module formats
 * and packages the C# IPC executable
 *
 * Usage: node build.js [type] [configuration]
 * Types: build, clean, build-csharp, build-ts, build-content
 * Configuration: Release (default) or Debug
 */

const { execSync, spawn } = require('child_process');
const fs = require('fs');
const path = require('path');

// Valid build types
const VALID_TYPES = [
  'build',
  'clean',
  'build-csharp',
  'build-ts',
  'build-content'
];

// Parse command line arguments
const args = process.argv.slice(2);
const type = args[0] || 'build';
const configuration = args[1] || 'Release';

// Validate build type
if (!VALID_TYPES.includes(type)) {
  console.error(`Error: Invalid build type '${type}'`);
  console.error(`Valid types: ${VALID_TYPES.join(', ')}`);
  process.exit(1);
}

// Validate configuration
if (!['Release', 'Debug'].includes(configuration)) {
  console.error(`Error: Invalid configuration '${configuration}'`);
  console.error('Valid configurations: Release, Debug');
  process.exit(1);
}

/**
 * Custom exception for missing .NET SDK
 */
class MissingDotNetSDKException extends Error {
  constructor() {
    super('Missing .NET SDK - Install a .NET SDK from https://dotnet.microsoft.com/en-us/download/dotnet/9.0');
    this.name = 'MissingDotNetSDKException';
  }
}

/**
 * Error code handlers for specific error conditions
 */
const ERROR_CODE_HANDLER = {
  2147516561: {
    genError: () => new MissingDotNetSDKException(),
  },
};

/**
 * Spawns a process and returns a promise
 * @param {string} exe - Executable to run
 * @param {string[]} args - Arguments
 * @param {Object} options - Spawn options
 * @param {Object} out - Output logger
 * @returns {Promise<void>}
 */
function spawnAsync(exe, args, options = {}, out = console) {
  return new Promise((resolve, reject) => {
    const desc = `${options.cwd || '.'}/${exe} ${args.join(' ')}`;
    out.log('started: ' + desc);
    const outBufs = [];

    try {
      const proc = spawn(exe, args, options);
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

/**
 * Signs a file using the configured signing tool
 * @param {string} filePath - Path to file to sign
 * @returns {Promise<void>}
 */
async function sign(filePath) {
  if (process.env['SIGN_TOOL'] !== undefined) {
    console.log(`  Signing: ${filePath}`);
    return spawnAsync(
      process.env['SIGN_TOOL'],
      [
        'sign',
        '/sha1', process.env['SIGN_THUMBPRINT'],
        '/td', 'sha256',
        '/fd', 'sha256',
        '/tr', 'http://timestamp.comodoca.com',
        filePath
      ]
    );
  } else {
    console.log(`  Skipping signing (SIGN_TOOL not configured)`);
  }
}

/**
 * Recursively removes files and directories with glob pattern support
 * @param {string[]} patterns - File patterns to remove
 */
function removeItems(patterns) {
  patterns.forEach(pattern => {
    try {
      // Handle glob patterns (*.tgz, *.dll, etc.)
      if (pattern.includes('*')) {
        const dir = process.cwd();
        const regex = new RegExp('^' + pattern.replace(/\*/g, '.*').replace(/\?/g, '.') + '$');

        const files = fs.readdirSync(dir);
        files.forEach(file => {
          if (regex.test(file)) {
            try {
              const fullPath = path.join(dir, file);
              if (fs.existsSync(fullPath)) {
                const stats = fs.statSync(fullPath);
                if (stats.isDirectory()) {
                  fs.rmSync(fullPath, { recursive: true, force: true });
                  console.log(`  Removed directory: ${file}`);
                } else {
                  fs.unlinkSync(fullPath);
                  console.log(`  Removed file: ${file}`);
                }
              }
            } catch (err) {
              // Ignore individual file errors
            }
          }
        });
      } else {
        // Handle direct paths
        if (fs.existsSync(pattern)) {
          const stats = fs.statSync(pattern);
          if (stats.isDirectory()) {
            fs.rmSync(pattern, { recursive: true, force: true });
            console.log(`  Removed directory: ${pattern}`);
          } else {
            fs.unlinkSync(pattern);
            console.log(`  Removed file: ${pattern}`);
          }
        }
      }
    } catch (err) {
      // Ignore errors (equivalent to -ErrorAction Ignore)
    }
  });
}

/**
 * Checks if a command exists in PATH (cross-platform)
 * @param {string} command - Command to check
 * @returns {boolean} True if command exists
 */
function commandExists(command) {
  try {
    const isWindows = process.platform === 'win32';
    const checkCommand = isWindows ? `where ${command}` : `which ${command}`;
    execSync(checkCommand, { stdio: 'ignore' });
    return true;
  } catch {
    return false;
  }
}

/**
 * Executes a command and logs output
 * @param {string} command - Command to execute
 * @param {Object} options - Execution options
 */
function execCommand(command, options = {}) {
  console.log(`  Running: ${command}`);
  try {
    execSync(command, {
      stdio: 'inherit',
      cwd: process.cwd(),
      ...options
    });
  } catch (err) {
    console.error(`  Command failed: ${command}`);
    throw err;
  }
}

/**
 * Main build function
 */
async function main() {
  try {
    console.log(`\n=== Build Configuration ===`);
    console.log(`Type: ${type}`);
    console.log(`Configuration: ${configuration}`);
    console.log(`Working Directory: ${process.cwd()}`);
    console.log(`===========================\n`);

    // Validate prerequisites
    if (['build', 'build-csharp'].includes(type)) {
      if (!commandExists('dotnet')) {
        throw new Error('dotnet CLI not found. Please install .NET SDK from https://dotnet.microsoft.com/en-us/download/dotnet/9.0');
      }
    }

    if (['build', 'build-ts'].includes(type)) {
      if (!commandExists('npx')) {
        throw new Error('npx not found. Please install Node.js and npm.');
      }
    }

    // Clean
    if (['build', 'clean'].includes(type)) {
      console.log('Cleaning build artifacts...');
      removeItems([
        '*.tgz',
        'dist',
        'coverage',
        '.nyc_output'
      ]);
      console.log('');
    }

    // Build C# IPC Module
    if (['build', 'build-csharp'].includes(type)) {
      console.log(`Building ModInstaller.IPC (${configuration})`);

      // Verify source directory exists
      const ipcDir = path.resolve('../ModInstaller.IPC');
      if (!fs.existsSync(ipcDir)) {
        throw new Error(`ModInstaller.IPC directory not found at: ${ipcDir}`);
      }

      // Run dotnet publish with retry logic (handles locked files)
      const outputDir = path.resolve('dist');
      const buildArgs = [
        'publish',
        ipcDir,
        '-c', configuration,
        '-f', 'net9.0-windows',
        '-o', outputDir
      ];

      try {
        await spawnAsync('dotnet', buildArgs);
      } catch (err) {
        // The build may fail because of locked files (sigh) so just try again...
        console.log('  Build failed, retrying after 500ms...');
        await new Promise((resolve) => setTimeout(resolve, 500));
        await spawnAsync('dotnet', buildArgs);
      }

      // Sign the executable if signing is configured
      const exePath = path.join(outputDir, 'ModInstallerIPC.exe');
      if (fs.existsSync(exePath)) {
        await sign(exePath);
      }

      console.log('');
    }

    // Build TypeScript
    if (['build', 'build-ts'].includes(type)) {
      console.log('Building TypeScript sources');

      // Verify tsconfig files exist
      if (!fs.existsSync('tsconfig.json')) {
        throw new Error('tsconfig.json not found');
      }
      if (!fs.existsSync('tsconfig.module.json')) {
        throw new Error('tsconfig.module.json not found');
      }

      // Compile TypeScript with both configs
      // Main build (CommonJS/UMD)
      execCommand('npx tsc -p tsconfig.json');

      // Module build (ES modules)
      execCommand('npx tsc -p tsconfig.module.json');
      console.log('');
    }

    console.log('✓ Build completed successfully!');
    process.exit(0);

  } catch (err) {
    // Handle specific error codes
    const error = ERROR_CODE_HANDLER[err?.code] !== undefined
      ? ERROR_CODE_HANDLER[err.code].genError()
      : err;

    console.error('\n✗ Build failed:', error.message);
    if (process.env.DEBUG) {
      console.error('\nStack trace:', error.stack);
    }

    const exitCode = err?.code || -1;
    process.exit(exitCode);
  }
}

// Run main function
main();
