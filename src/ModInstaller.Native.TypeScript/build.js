#!/usr/bin/env node

/**
 * Build script for fomod-installer-native
 * Converted from commands.ps1 to native JavaScript
 *
 * Usage: node build.js [type] [configuration]
 * Types: build, test, clear, build-native, build-napi, build-webpack, build-content, test-build
 * Configuration: Release (default) or Debug
 */

const { execSync, spawn } = require('child_process');
const fs = require('fs');
const path = require('path');

// Valid build types
const VALID_TYPES = [
  'build',
  'test',
  'clear',
  'build-native',
  'build-napi',
  'build-webpack',
  'build-content',
  'test-build'
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
 * Recursively creates directory and copies file
 * @param {string} sourcePath - Source file path
 * @param {string} destPath - Destination file path
 */
function copyItem(sourcePath, destPath) {
  const resolvedSource = path.resolve(sourcePath);
  const resolvedDest = path.resolve(destPath);

  // Check if source exists
  if (!fs.existsSync(resolvedSource)) {
    throw new Error(`Source file not found: ${sourcePath}`);
  }

  const destDir = path.dirname(resolvedDest);

  // Create directory if it doesn't exist
  if (!fs.existsSync(destDir)) {
    fs.mkdirSync(destDir, { recursive: true });
  }

  // Copy file
  fs.copyFileSync(resolvedSource, resolvedDest);
  console.log(`  Copied: ${sourcePath} -> ${destPath}`);
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
    if (['build', 'test', 'build-native'].includes(type)) {
      if (!commandExists('dotnet')) {
        throw new Error('dotnet CLI not found. Please install .NET SDK from https://dotnet.microsoft.com/en-us/download/dotnet/9.0');
      }
    }

    if (['build', 'test', 'build-napi', 'build-webpack'].includes(type)) {
      if (!commandExists('npx')) {
        throw new Error('npx not found. Please install Node.js and npm.');
      }
    }

    // Clean
    if (['build', 'test', 'clear'].includes(type)) {
      console.log('Cleaning build artifacts...');
      removeItems([
        '*.tgz',
        '*.h',
        '*.dll',
        '*.lib',
        'build',
        'dist',
        'coverage',
        '.nyc_output'
      ]);
      console.log('');
    }

    // Build C# Native Module
    if (['build', 'test', 'build-native'].includes(type)) {
      console.log(`Building ModInstaller.Native (${configuration})`);

      // Verify source directory exists
      const nativeDir = path.resolve('../ModInstaller.Native');
      if (!fs.existsSync(nativeDir)) {
        throw new Error(`ModInstaller.Native directory not found at: ${nativeDir}`);
      }

      // Run dotnet publish with retry logic
      const buildArgs = [
        'publish',
        '-r', 'win-x64',
        '--self-contained',
        '-c', configuration,
        '../ModInstaller.Native'
      ];

      try {
        await spawnAsync('dotnet', buildArgs);
      } catch (err) {
        // The build may fail because of locked files (sigh) so just try again...
        console.log('  Build failed, retrying after 500ms...');
        await new Promise((resolve) => setTimeout(resolve, 500));
        await spawnAsync('dotnet', buildArgs);
      }

      // Copy native artifacts
      const basePath = `../ModInstaller.Native`;
      copyItem(`${basePath}/bin/${configuration}/net9.0/win-x64/native/ModInstaller.Native.dll`, 'ModInstaller.Native.dll');
      copyItem(`${basePath}/bin/${configuration}/net9.0/win-x64/native/ModInstaller.Native.lib`, 'ModInstaller.Native.lib');
      copyItem(`${basePath}/ModInstaller.Native.h`, 'ModInstaller.Native.h');

      // Sign the DLL if signing is configured
      if (fs.existsSync('ModInstaller.Native.dll')) {
        await sign('ModInstaller.Native.dll');
      }

      console.log('');
    }

    // Build NAPI
    if (['build', 'test', 'build-napi'].includes(type)) {
      console.log(`Building NAPI (${configuration})`);

      // Verify binding.gyp exists
      if (!fs.existsSync('binding.gyp')) {
        throw new Error('binding.gyp not found in current directory');
      }

      // Determine build tag
      let tag = '';
      if (configuration === 'Release') {
        tag = '--release';
      } else if (configuration === 'Debug') {
        tag = '--debug';
      }

      // Run node-gyp rebuild
      execCommand(`npx node-gyp rebuild --arch=x64 ${tag}`.trim());
      console.log('');
    }

    // Copy content to dist
    if (['build', 'test', 'test-build', 'build-content'].includes(type)) {
      console.log('Copying content to dist');

      copyItem('ModInstaller.Native.dll', 'dist/ModInstaller.Native.dll');
      copyItem(`build/${configuration}/modinstaller.node`, 'dist/modinstaller.node');
      console.log('');
    }

    // Build Webpack Bundle
    if (['build', 'build-webpack'].includes(type)) {
      console.log('Building Webpack bundle');

      // Verify TypeScript config exists
      if (!fs.existsSync('tsconfig.json')) {
        throw new Error('tsconfig.json not found');
      }

      // Verify webpack config exists
      if (!fs.existsSync('webpack.config.js')) {
        throw new Error('webpack.config.js not found');
      }
      
      // Compile TypeScript declarations
      execCommand('npx tsc --emitDeclarationOnly');

      // Build with webpack
      execCommand('npx webpack --config webpack.config.js');
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
