import test from 'ava';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import {
  BaseIPCConnection,
  ConnectionStrategy,
  TCPTransport,
  RegularProcessLauncher
} from '../src';
import {
  getAllTestCases,
  getStopPatterns,
  extractArchiveToTemp,
  TestCase,
  Instruction
} from './sharedTestData';

// Test implementation of BaseIPCConnection
class TestIPCConnection extends BaseIPCConnection {
  private archiveFiles: string[] = [];

  constructor(strategies: ConnectionStrategy | ConnectionStrategy[], connectionTimeout?: number) {
    super(strategies, connectionTimeout);
  }

  protected async fileExists(filePath: string): Promise<boolean> {
    return fs.existsSync(filePath);
  }

  protected getExecutablePaths(exeName: string): string[] {
    // Look for the executable in the dist folder
    const distPath = path.join(__dirname, '..', '..', 'dist', exeName);
    return [distPath];
  }

  public setArchiveFiles(files: string[]): void {
    this.archiveFiles = files;
  }

  public async testSupported(allowedTypes: string[]): Promise<{ supported: boolean; requiredFiles: string[] }> {
    return this.sendCommand('TestSupported', {
      files: this.archiveFiles,
      allowedTypes
    });
  }

  public async install(
    stopPatterns: string[],
    pluginPath: string,
    scriptPath: string,
    fomodChoices: any,
    validate: boolean
  ): Promise<{ message: string; instructions: any[] }> {
    return this.sendCommand('Install', {
      files: this.archiveFiles,
      stopPatterns,
      pluginPath,
      scriptPath,
      fomodChoices,
      validate
    }, 60000); // 60 second timeout for install
  }

  public setupCallbacks(
    gameVersion: string,
    extenderVersion: string,
    appVersion: string,
    installedPlugins: string[]
  ): void {
    // Plugin context callbacks
    this.registerCallback('pluginsGetAll', (_activeOnly: boolean) => installedPlugins);
    this.registerCallback('pluginsIsActive', (_pluginName: string) => false);

    // INI context callbacks
    this.registerCallback('iniGetBool', (_file: string, _section: string, _key: string) => null);
    this.registerCallback('iniGetInt', (_file: string, _section: string, _key: string) => null);
    this.registerCallback('iniGetString', (_file: string, _section: string, _key: string) => null);

    // Application context callbacks
    this.registerCallback('contextGetAppVersion', () => appVersion);
    this.registerCallback('contextGetCurrentGameVersion', () => gameVersion);
    this.registerCallback('contextGetExtenderVersion', (_extender: string) => extenderVersion);
    this.registerCallback('contextIsExtenderPresent', () => extenderVersion !== '');
    this.registerCallback('contextCheckIfFileExists', (_fileName: string) => false);
    this.registerCallback('contextGetExistingDataFile', (_fileName: string) => null);
    this.registerCallback('contextGetExistingDataFileList', (_folderPath: string, _pattern: string, _searchType: number) => []);

    // UI callbacks - auto-advance through dialogs
    let contCallback: ((forward: boolean, currentStep: number) => void) | null = null;

    this.registerCallback('uiStartDialog', (
      _moduleName: string,
      _image: any,
      _select: any,
      cont: any,
      _cancel: any
    ) => {
      contCallback = cont;
    });

    this.registerCallback('uiEndDialog', () => {
      contCallback = null;
    });

    this.registerCallback('uiUpdateState', (_installSteps: any[], currentStep: number) => {
      // Auto-advance in unattended mode
      if (contCallback) {
        contCallback(true, currentStep);
      }
    });

    this.registerCallback('uiReportError', (
      _title: string,
      message: string,
      _details: string
    ) => {
      console.error(`UI Error: ${message}`);
    });
  }
}

// Helper to normalize instruction for comparison
const normalizeInstruction = (inst: Instruction): string => {
  const parts = [inst.type];
  if (inst.source) parts.push(inst.source.replace(/\\/g, '/').toLowerCase());
  if (inst.destination) parts.push(inst.destination.replace(/\\/g, '/').toLowerCase());
  return parts.join('|');
};

// Helper to compare instructions
const compareInstructions = (actual: any[], expected: Instruction[]): boolean => {
  const actualNormalized = actual.map(i => normalizeInstruction({
    type: i.type,
    source: i.source,
    destination: i.destination
  })).sort();
  const expectedNormalized = expected.map(normalizeInstruction).sort();

  if (actualNormalized.length !== expectedNormalized.length) {
    return false;
  }

  for (let i = 0; i < actualNormalized.length; i++) {
    if (actualNormalized[i] !== expectedNormalized[i]) {
      return false;
    }
  }

  return true;
};

// Check if the executable exists
const executablePath = path.join(__dirname, '..', '..', 'dist', 'ModInstallerIPC.exe');
const executableExists = fs.existsSync(executablePath);

// Run a single test case
async function runTestCase(t: any, testCase: TestCase): Promise<void> {
  if (!executableExists) {
    t.pass(`Skipped: ModInstallerIPC.exe not found at ${executablePath}`);
    return;
  }

  // Extract archive to temp directory - IPC requires files on disk
  const extracted = await extractArchiveToTemp(testCase.archiveFile, testCase.game);

  // Create transport and launcher
  const transport = new TCPTransport();
  const launcher = new RegularProcessLauncher();
  const strategy: ConnectionStrategy = { transport, launcher };

  const connection = new TestIPCConnection(strategy, 30000);

  try {
    const stopPatterns = getStopPatterns(testCase);

    // Set file list (with paths relative to temp dir, using backslashes)
    connection.setArchiveFiles(extracted.files);

    // Set up callbacks
    connection.setupCallbacks(
      testCase.gameVersion ?? '1.0.0',
      testCase.extenderVersion ?? '1.0.0',
      testCase.appVersion ?? '1.0.0',
      testCase.installedPlugins ?? []
    );

    // Initialize connection (starts the C# process)
    await connection.initialize();

    // Test if supported
    const supported = await connection.testSupported(['XmlScript', 'CSharpScript']);
    t.truthy(supported, 'TestSupported should return a result');

    // Run install
    const result = await connection.install(
      stopPatterns,
      testCase.pluginPath,
      extracted.tempDir, // scriptPath - point to temp dir where files are extracted
      testCase.preset ?? null,
      testCase.validate ?? true
    );

    // Assertions
    t.truthy(result, 'Install should return a result');
    t.truthy(result.instructions, 'Result should have instructions');

    // Compare instructions
    const instructionsMatch = compareInstructions(result.instructions, testCase.expectedInstructions);
    if (!instructionsMatch) {
      t.log('Expected instructions:', testCase.expectedInstructions);
      t.log('Actual instructions:', result.instructions.map((i: any) => ({
        type: i.type,
        source: i.source,
        destination: i.destination
      })));
    }
    t.true(instructionsMatch, 'Instructions should match expected');

  } finally {
    await extracted.cleanup();
    await connection.dispose();
  }
}

// Generate tests from shared JSON data (only CSharpScript tests)
const testCases = getAllTestCases();

if (testCases.length === 0) {
  test('No C# script test cases found', (t) => {
    t.pass('No test cases to run - this is expected if no CSharpScript test data files exist');
  });
} else {
  for (const testCase of testCases) {
    test(`${testCase.game}: ${testCase.name}`, async (t) => {
      await runTestCase(t, testCase);
    });
  }
}
