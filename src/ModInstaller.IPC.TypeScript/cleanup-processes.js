#!/usr/bin/env node

/**
 * Utility script to clean up stuck ModInstallerIPC.exe processes
 */

const cp = require('child_process');
const { promisify } = require('util');

const exec = promisify(cp.exec);

async function findStuckProcesses() {
  try {
    const { stdout } = await exec('tasklist /FI "IMAGENAME eq ModInstallerIPC.exe" /FO CSV');
    const lines = stdout.trim().split('\n');

    if (lines.length <= 1 || lines[0].includes('No tasks')) {
      console.log('No ModInstallerIPC.exe processes found.');
      return [];
    }

    const processes = [];
    for (let i = 1; i < lines.length; i++) {
      const columns = lines[i].split('","');
      if (columns.length >= 2) {
        const pid = columns[1].replace(/"/g, '');
        processes.push(parseInt(pid));
      }
    }

    return processes;
  } catch (error) {
    console.error('Error finding processes:', error.message);
    return [];
  }
}

async function killProcess(pid) {
  try {
    await exec(`taskkill /F /PID ${pid}`);
    console.log(`Successfully killed process ${pid}`);
    return true;
  } catch (error) {
    console.error(`Failed to kill process ${pid}:`, error.message);
    return false;
  }
}

async function main() {
  console.log('Searching for stuck ModInstallerIPC.exe processes...');

  const processes = await findStuckProcesses();

  if (processes.length === 0) {
    console.log('No processes to clean up.');
    return;
  }

  console.log(`Found ${processes.length} ModInstallerIPC.exe process(es):`, processes);

  const args = process.argv.slice(2);
  const force = args.includes('--force') || args.includes('-f');

  if (!force) {
    console.log('To kill these processes, run this script with --force flag:');
    console.log('node cleanup-processes.js --force');
    return;
  }

  console.log('Attempting to kill processes...');

  let killed = 0;
  for (const pid of processes) {
    if (await killProcess(pid)) {
      killed++;
    }
  }

  console.log(`Successfully killed ${killed} out of ${processes.length} processes.`);
}

if (require.main === module) {
  main().catch(console.error);
}

module.exports = { findStuckProcesses, killProcess };
