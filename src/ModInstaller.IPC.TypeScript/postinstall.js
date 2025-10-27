#!/usr/bin/env node

/**
 * Postinstall script for IPC package
 * Skips build if being installed as part of meta package
 */

// Check if we're being installed by the meta package
if (process.env.FOMOD_METAPACKAGE_INSTALL === '1') {
  console.log('⊘ Skipping IPC package postinstall (meta package will handle build)');
  process.exit(0);
}

// Otherwise, run the build
console.log('Building IPC package...');
const { execSync } = require('child_process');

try {
  execSync('npm run build', {
    stdio: 'inherit',
    shell: true,
  });
  console.log('✅ IPC package build complete');
} catch (error) {
  console.error('❌ IPC package build failed:', error.message);
  process.exit(1);
}
