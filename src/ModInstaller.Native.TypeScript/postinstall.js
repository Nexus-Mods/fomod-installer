#!/usr/bin/env node

/**
 * Postinstall script for Native package
 * Skips build if being installed as part of meta package
 */

// Check if we're being installed by the meta package
if (process.env.FOMOD_METAPACKAGE_INSTALL === '1') {
  console.log('⊘ Skipping Native package postinstall (meta package will handle build)');
  process.exit(0);
}

// Otherwise, run the build
console.log('Building Native package...');
const { execSync } = require('child_process');

try {
  execSync('npm run build', {
    stdio: 'inherit',
    shell: true,
  });
  console.log('✅ Native package build complete');
} catch (error) {
  console.error('❌ Native package build failed:', error.message);
  process.exit(1);
}
