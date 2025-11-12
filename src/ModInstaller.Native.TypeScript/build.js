#!/usr/bin/env node

/**
 * Build script for fomod-installer-native
 * Converted from commands.ps1 to native JavaScript
 *
 * Usage: node build.js <type> [configuration]
 * Types: build, test, clear, build-native, build-napi, build-ts, build-content, test-build
 * Configuration: Release (default) or Debug
 */

const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

// Valid build types
const VALID_TYPES = [
  'build',
  'test',
  'clear',
  'build-native',
  'build-napi',
  'build-ts',
  'build-webpack',
  'build-content',
  'test-build'
];

// Parse command line arguments
const args = process.argv.slice(2);
const type = args[0];
const configuration = args[1] || 'Release';

// Validate build type
if (!type) {
  console.error('Error: Build type required');
  console.error('Usage: node build.js <type> [configuration]');
  console.error(`Types: ${VALID_TYPES.join(', ')}`);
  process.exit(1);
}

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

try {
  console.log(`\n=== Build Configuration ===`);
  console.log(`Type: ${type}`);
  console.log(`Configuration: ${configuration}`);
  console.log(`Working Directory: ${process.cwd()}`);
  console.log(`===========================\n`);

  // Validate prerequisites
  if (['build', 'test', 'build-native'].includes(type)) {
    if (!commandExists('dotnet')) {
      throw new Error('dotnet CLI not found. Please install .NET SDK.');
    }
  }

  if (['build', 'test', 'build-napi', 'build-ts', 'build-webpack'].includes(type)) {
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

    // Run dotnet publish
    execCommand(
      `dotnet publish -r win-x64 --self-contained -c ${configuration} ../ModInstaller.Native`
    );

    // Copy native artifacts
    const basePath = `../ModInstaller.Native`;
    copyItem(`${basePath}/bin/${configuration}/net9.0/win-x64/native/ModInstaller.Native.dll`, 'ModInstaller.Native.dll');
    copyItem(`${basePath}/bin/${configuration}/net9.0/win-x64/native/ModInstaller.Native.lib`, 'ModInstaller.Native.lib');
    copyItem(`${basePath}/ModInstaller.Native.h`, 'ModInstaller.Native.h');
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

    // Verify webpack config exists
    if (!fs.existsSync('webpack.config.js')) {
      throw new Error('webpack.config.js not found');
    }

    // Build with webpack
    execCommand('npx webpack --config webpack.config.js');
    console.log('');
  }

  console.log('✓ Build completed successfully!');
  process.exit(0);

} catch (err) {
  console.error('\n✗ Build failed:', err.message);
  if (process.env.DEBUG) {
    console.error('\nStack trace:', err.stack);
  }
  process.exit(1);
}
