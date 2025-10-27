#!/usr/bin/env node

/**
 * Installation script for FOMOD Installer meta package
 *
 * For GitHub installs: Builds both Native and IPC subpackages
 * For npm installs: Pre-built packages are already included
 */

const { execSync } = require('child_process');
const path = require('path');
const fs = require('fs');

// Get build mode from arguments (Debug or Release)
const buildMode = process.argv[2] || 'Release';
const isDebug = buildMode === 'Debug';

const nativeDir = path.join(__dirname, 'src', 'ModInstaller.Native.TypeScript');
const ipcDir = path.join(__dirname, 'src', 'ModInstaller.IPC.TypeScript');

// Check if this is a development install (has src/ with package.json files)
const isDevInstall = fs.existsSync(path.join(ipcDir, 'package.json'));

if (!isDevInstall) {
  console.log('✓ Using pre-built packages (npm install)');
  console.log('✓ Installation complete!\n');
  process.exit(0);
}

console.log(`\n🔧 Building FOMOD Installer from source (${buildMode} mode)...\n`);

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
    // Use npm with FOMOD_METAPACKAGE_INSTALL environment variable to prevent subpackage postinstall
    const env = Object.assign({}, process.env, { FOMOD_METAPACKAGE_INSTALL: '1' });

    console.log(`\n📦 ${`Install ${packageName} dependencies`}...`);
    console.log(`   Directory: ${path.relative(__dirname, dir)}`);
    console.log(`   Command: npm install --legacy-peer-deps\n`);

    try {
      const { execSync } = require('child_process');
      execSync('npm install --legacy-peer-deps', {
        cwd: dir,
        stdio: 'inherit',
        shell: true,
        env: env,
      });
      console.log(`✅ ${`Install ${packageName} dependencies`} - Success\n`);
      return true;
    } catch (error) {
      console.error(`❌ ${`Install ${packageName} dependencies`} - Failed`);
      console.error(`   Error: ${error.message}\n`);
      return false;
    }
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
  console.log('  FOMOD Installer Meta Package Installation (Dev Mode)');
  console.log('═'.repeat(60));

  // Step 1: Install dependencies for Native package
  if (!installDependencies(nativeDir, 'Native Package')) {
    hasErrors = true;
  }

  // Step 2: Install dependencies for IPC package
  if (!installDependencies(ipcDir, 'IPC Package')) {
    hasErrors = true;
  }

  // Step 3: Build Native package
  if (fs.existsSync(nativeDir)) {
    const nativeBuildCmd = isDebug ? 'npm run build Debug' : 'npm run build';
    if (!buildPackage(nativeDir, 'Native Package', nativeBuildCmd)) {
      console.warn('⚠️  Native package build failed - native bindings may not be available');
      // Don't set hasErrors - native is optional
    }
  }

  // Step 4: Build IPC package
  if (fs.existsSync(ipcDir)) {
    const ipcBuildCmd = isDebug ? 'npm run buildDev' : 'npm run build';
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
    console.log('  const { native, ipc } = require("fomod-installer-meta");');
    console.log('  // or');
    console.log('  import { native, ipc } from "fomod-installer-meta";\n');
  }
})();
