#!/usr/bin/env node

/**
 * Cleanup script for FOMOD Installer meta package
 * Removes build artifacts and node_modules from all subpackages
 */

const { execSync } = require('child_process');
const path = require('path');
const fs = require('fs');

console.log('\n🧹 Cleaning FOMOD Installer workspace...\n');

const nativeDir = path.join(__dirname, 'src', 'ModInstaller.Native.TypeScript');
const ipcDir = path.join(__dirname, 'src', 'ModInstaller.IPC.TypeScript');
const rootDir = __dirname;

/**
 * Remove directory recursively
 */
function removeDir(dirPath, description) {
  if (fs.existsSync(dirPath)) {
    console.log(`🗑️  Removing ${description}...`);
    try {
      fs.rmSync(dirPath, { recursive: true, force: true });
      console.log(`   ✅ Removed: ${path.relative(__dirname, dirPath)}`);
      return true;
    } catch (error) {
      console.error(`   ❌ Failed to remove: ${error.message}`);
      return false;
    }
  } else {
    console.log(`   ⊘ Not found: ${path.relative(__dirname, dirPath)}`);
    return true;
  }
}

/**
 * Run clean script in a subpackage
 */
function runCleanScript(dir, packageName) {
  const packageJsonPath = path.join(dir, 'package.json');
  if (!fs.existsSync(packageJsonPath)) {
    console.log(`⊘ Skipping ${packageName} - package.json not found\n`);
    return true;
  }

  const packageJson = require(packageJsonPath);
  if (packageJson.scripts && packageJson.scripts.clean) {
    console.log(`\n🧽 Running clean script for ${packageName}...`);
    try {
      execSync('npm run clean', {
        cwd: dir,
        stdio: 'inherit',
        shell: true,
      });
      console.log(`✅ Clean script completed for ${packageName}\n`);
      return true;
    } catch (error) {
      console.error(`❌ Clean script failed for ${packageName}: ${error.message}\n`);
      return false;
    }
  }
  return true;
}

// Main cleanup flow
(function main() {
  console.log('═'.repeat(60));
  console.log('  FOMOD Installer Workspace Cleanup');
  console.log('═'.repeat(60));
  console.log();

  let hasErrors = false;

  // Run clean scripts for subpackages
  if (!runCleanScript(nativeDir, 'Native Package')) {
    hasErrors = true;
  }

  if (!runCleanScript(ipcDir, 'IPC Package')) {
    hasErrors = true;
  }

  // Remove node_modules from subpackages
  console.log('\n📁 Removing node_modules directories...\n');
  removeDir(path.join(nativeDir, 'node_modules'), 'Native node_modules');
  removeDir(path.join(ipcDir, 'node_modules'), 'IPC node_modules');

  // Remove build artifacts
  console.log('\n📁 Removing build artifacts...\n');
  removeDir(path.join(nativeDir, 'dist'), 'Native dist');
  removeDir(path.join(ipcDir, 'dist'), 'IPC dist');
  removeDir(path.join(rootDir, 'dist'), 'Root dist');
  removeDir(path.join(rootDir, 'Build'), 'C# Build artifacts');

  // Remove root node_modules
  console.log('\n📁 Removing root node_modules...\n');
  removeDir(path.join(rootDir, 'node_modules'), 'Root node_modules');

  console.log('\n═'.repeat(60));

  if (hasErrors) {
    console.error('\n⚠️  Cleanup completed with some errors\n');
    process.exit(1);
  } else {
    console.log('\n✅ Cleanup completed successfully!\n');
  }
})();
