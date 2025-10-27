#!/usr/bin/env node

/**
 * Installation script for FOMOD Installer meta package
 * Builds both Native and IPC subpackages
 */

const { execSync } = require('child_process');
const path = require('path');
const fs = require('fs');

// Get build mode from arguments (Debug or Release)
const buildMode = process.argv[2] || 'Release';
const isDebug = buildMode === 'Debug';

console.log(`\n🔧 Building FOMOD Installer (${buildMode} mode)...\n`);

const nativeDir = path.join(__dirname, 'src', 'ModInstaller.Native.TypeScript');
const ipcDir = path.join(__dirname, 'src', 'ModInstaller.IPC.TypeScript');

/**
 * Runs a command and streams output to console
 */
function runCommand(command, cwd, description) {
  console.log(`\n📦 ${description}...`);
  console.log(`   Directory: ${path.relative(__dirname, cwd)}`);
  console.log(`   Command: ${command}\n`);

  try {
    execSync(command, {
      cwd: cwd,
      stdio: 'inherit',
      shell: true,
    });
    console.log(`✅ ${description} - Success\n`);
    return true;
  } catch (error) {
    console.error(`❌ ${description} - Failed`);
    console.error(`   Error: ${error.message}\n`);
    return false;
  }
}

/**
 * Install dependencies for a subpackage
 */
function installDependencies(dir, packageName) {
  if (!fs.existsSync(path.join(dir, 'package.json'))) {
    console.log(`⚠️  Skipping ${packageName} - package.json not found\n`);
    return false;
  }

  // Check if node_modules exists
  const nodeModulesExists = fs.existsSync(path.join(dir, 'node_modules'));

  if (!nodeModulesExists) {
    console.log(`📥 Installing dependencies for ${packageName}...`);
    return runCommand('yarn install --frozen-lockfile --ignore-scripts', dir, `Install ${packageName} dependencies`);
  } else {
    console.log(`✓ Dependencies already installed for ${packageName}\n`);
    return true;
  }
}

/**
 * Build a subpackage
 */
function buildPackage(dir, packageName, buildScript) {
  if (!fs.existsSync(path.join(dir, 'package.json'))) {
    console.log(`⚠️  Skipping ${packageName} - package.json not found\n`);
    return false;
  }

  return runCommand(buildScript, dir, `Build ${packageName}`);
}

// Main installation flow
(async function main() {
  let hasErrors = false;

  console.log('═'.repeat(60));
  console.log('  FOMOD Installer Meta Package Installation');
  console.log('═'.repeat(60));

  // Step 1: Install dependencies for Native package
  if (!installDependencies(nativeDir, 'Native Package')) {
    hasErrors = true;
  }

  // Step 2: Install dependencies for IPC package
  if (!installDependencies(ipcDir, 'IPC Package')) {
    hasErrors = true;
  }

  // Step 3: Build Native package (skip postinstall to avoid recursion)
  if (fs.existsSync(nativeDir)) {
    const nativeBuildCmd = isDebug ? 'yarn run build Debug' : 'yarn run build';
    if (!buildPackage(nativeDir, 'Native Package', nativeBuildCmd)) {
      console.warn('⚠️  Native package build failed - native bindings may not be available');
      // Don't set hasErrors - native is optional
    }
  }

  // Step 4: Build IPC package
  if (fs.existsSync(ipcDir)) {
    const ipcBuildCmd = isDebug ? 'yarn run buildDev' : 'yarn run build';
    if (!buildPackage(ipcDir, 'IPC Package', ipcBuildCmd)) {
      console.error('❌ IPC package build failed - this is critical');
      hasErrors = true;
    }
  }

  console.log('═'.repeat(60));

  if (hasErrors) {
    console.error('\n❌ Installation completed with errors\n');
    process.exit(1);
  } else {
    console.log('\n✅ Installation completed successfully!\n');
    console.log('You can now use:');
    console.log('  const { native, ipc } = require("fomod-installer");');
    console.log('  // or');
    console.log('  import { native, ipc } from "fomod-installer";\n');
  }
})();
