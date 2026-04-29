/**
 * Runtime verification: UnsupportedFunctionalityWarning emitted on Linux
 * for a C# script FOMOD when CSharpScriptType is not registered.
 *
 * Run: pnpm vitest run test/verify-warning.spec.ts
 */
import { test, expect } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import StreamZip from 'node-stream-zip';

import {
  BaseIPCConnection,
  ConnectionStrategy,
  TCPTransport,
  RegularProcessLauncher,
} from '../src';

class VerifyConnection extends BaseIPCConnection {
  private archiveFiles: string[] = [];

  constructor(strategies: ConnectionStrategy | ConnectionStrategy[], timeout?: number) {
    super(strategies, timeout);
  }

  protected async fileExists(filePath: string): Promise<boolean> {
    return fs.existsSync(filePath);
  }

  protected getExecutablePaths(_exeName: string): string[] {
    const packageRoot = path.resolve(__dirname, fs.existsSync(path.resolve(__dirname, '../package.json')) ? '..' : '../..');
    // On Linux the binary is a native ELF (not Mono) — use the extension-less name to
    // avoid RegularProcessLauncher's .exe → mono detection, which doesn't apply here.
    const exeName = process.platform === 'win32' ? 'ModInstallerIPC.exe' : 'ModInstallerIPC';
    return [path.join(packageRoot, 'dist', exeName)];
  }

  public setArchiveFiles(files: string[]): void {
    this.archiveFiles = files;
  }

  public async install(
    stopPatterns: string[],
    pluginPath: string,
    scriptPath: string,
    fomodChoices: null,
    preselect: boolean,
    validate: boolean,
  ): Promise<{ message: string; instructions: Array<{ type: string; source?: string; destination?: string; reason?: string; platform?: string }> }> {
    return this.sendCommand('Install', {
      files: this.archiveFiles,
      stopPatterns,
      pluginPath,
      scriptPath,
      fomodChoices,
      preselect,
      validate,
    }, 30000);
  }
}

const REPO_ROOT = path.resolve(__dirname, '../../..');
const archivePath = path.join(REPO_ROOT, 'test/TestData/Data/CSharpTestCase.zip');
const packageRoot = path.resolve(__dirname, fs.existsSync(path.resolve(__dirname, '../package.json')) ? '..' : '../..');
const executablePath = path.join(packageRoot, 'dist', 'ModInstallerIPC.exe');
const executableExists = fs.existsSync(executablePath);

test.skipIf(!executableExists)(
  'Linux: C# script FOMOD emits UnsupportedFunctionalityWarning when CSharpScriptType not registered',
  async () => {
    // Extract archive to temp dir
    const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'verify-warning-'));
    const zip = new StreamZip.async({ file: archivePath, skipEntryNameValidation: true });
    try {
      await zip.extract(null, tempDir);
    } finally {
      await zip.close();
    }

    // Collect files with backslash-separated paths (IPC convention)
    const files: string[] = [];
    function scan(dir: string, prefix: string = ''): void {
      for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const rel = prefix ? `${prefix}\\${entry.name}` : entry.name;
        if (entry.isDirectory()) {
          scan(path.join(dir, entry.name), rel);
        } else {
          files.push(path.join(tempDir, rel));
        }
      }
    }
    scan(tempDir);

    console.log('Archive files:', files.map(f => path.relative(tempDir, f)));

    const transport = new TCPTransport();
    const launcher = new RegularProcessLauncher();
    const strategy: ConnectionStrategy = { transport, launcher };
    const conn = new VerifyConnection(strategy, 15000);

    conn.setArchiveFiles(files);
    conn.registerCallback('pluginsGetAll', (_activeOnly: boolean) => []);
    conn.registerCallback('pluginsIsActive', (_name: string) => false);
    conn.registerCallback('iniGetBool', () => null);
    conn.registerCallback('iniGetInt', () => null);
    conn.registerCallback('iniGetString', () => null);
    conn.registerCallback('contextGetAppVersion', () => '1.0.0');
    conn.registerCallback('contextGetCurrentGameVersion', () => '1.0.0');
    conn.registerCallback('contextGetExtenderVersion', (_extender: string) => '');
    conn.registerCallback('contextIsExtenderPresent', () => false);
    conn.registerCallback('contextCheckIfFileExists', (_fileName: string) => false);
    conn.registerCallback('contextGetExistingDataFile', (_fileName: string) => null);
    conn.registerCallback('contextGetExistingDataFileList', (_folderPath: string, _pattern: string, _searchType: number) => []);
    conn.registerCallback('uiStartDialog', () => {});
    conn.registerCallback('uiEndDialog', () => {});
    conn.registerCallback('uiUpdateState', () => {});
    conn.registerCallback('uiReportError', (_title: string, msg: string) => {
      console.error('UI Error:', msg);
    });

    try {
      await conn.initialize();

      // Deliberately skip testSupported — on Linux with no CSharpScriptType registered,
      // TestSupported returns false for this archive. We test Install directly.
      const result = await conn.install(
        ['(^|/)fomod(/|$)'],
        'Data',
        tempDir,
        null,
        false,
        false,
      );

      console.log('Instructions:', JSON.stringify(result.instructions, null, 2));

      const warning = result.instructions.find(
        (i: { type: string; source?: string; reason?: string; platform?: string }) =>
          i.type === 'unsupported' && i.source === 'CSharpScript',
      );

      expect(warning).toBeTruthy();
      expect(warning!.reason).toBe('CSharpScript not supported on Linux');
      expect(warning!.platform).toBe('linux');
      console.log('UnsupportedFunctionalityWarning("CSharpScript") present with reason=%s platform=%s', warning!.reason, warning!.platform);
    } finally {
      await conn.dispose();
      fs.rmSync(tempDir, { recursive: true, force: true });
    }
  },
  60000,
);
