#!/usr/bin/env node

/**
 * Utility script to clean up stuck ModInstallerIPC processes
 */

import cp from 'child_process';
import { promisify } from 'util';

const exec = promisify(cp.exec);

export async function findStuckProcesses(): Promise<number[]> {
  if (process.platform !== 'win32') {
    // Linux: pgrep -f matches against full command line (avoids 15-char /proc/comm truncation)
    try {
      const { stdout } = await exec('pgrep -f ModInstallerIPC');
      return stdout.trim().split('\n')
        .filter(line => line.trim() !== '')
        .map(line => parseInt(line.trim(), 10))
        .filter(pid => !isNaN(pid));
    } catch {
      // pgrep exits with code 1 when no processes found — not an error
      return [];
    }
  }

  // Windows: existing tasklist logic (unchanged)
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

export async function killProcess(pid: number): Promise<boolean> {
  if (process.platform !== 'win32') {
    // Linux: SIGKILL ensures termination of stuck processes
    try {
      await exec(`kill -9 ${pid}`);
      console.log(`Successfully killed process ${pid}`);
      return true;
    } catch (error) {
      console.error(`Failed to kill process ${pid}:`, error.message);
      return false;
    }
  }

  // Windows: existing taskkill logic (unchanged)
  try {
    await exec(`taskkill /F /PID ${pid}`);
    console.log(`Successfully killed process ${pid}`);
    return true;
  } catch (error) {
    console.error(`Failed to kill process ${pid}:`, error.message);
    return false;
  }
}

async function main(): Promise<void> {
  console.log('Searching for stuck ModInstallerIPC processes...');

  const processes = await findStuckProcesses();

  if (processes.length === 0) {
    console.log('No processes to clean up.');
    return;
  }

  console.log(`Found ${processes.length} ModInstallerIPC process(es):`, processes);

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