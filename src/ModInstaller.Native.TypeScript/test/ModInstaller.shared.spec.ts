import test from 'ava';
import { NativeModInstaller, NativeFileSystem, allocAliveCount } from '../src';
import * as types from '../src/types';
import {
  getAllTestCases,
  getStopPatterns,
  preloadArchive,
  TestCase,
  Instruction,
  SelectedOption
} from './sharedTestData';

const isDebug = process.argv.includes('Debug');

// Deterministic UI context that auto-advances through installation steps
const createDeterministicUICallbacks = (
  dialogChoices?: SelectedOption[],
  gameVersion?: string,
  extenderVersion?: string
) => {
  let selectCallback: types.SelectCallback | null = null;
  let contCallback: types.ContinueCallback | null = null;
  let _cancelCallback: types.CancelCallback | null = null;
  const unattended = dialogChoices === undefined;
  let dialogInProgress = false;

  return {
    pluginsGetAll: (_activeOnly: boolean): string[] => [],
    contextGetAppVersion: (): string => '1.0.0',
    contextGetCurrentGameVersion: (): string => gameVersion ?? '1.0.0',
    contextGetExtenderVersion: (_extender: string): string => extenderVersion ?? '1.0.0',
    uiStartDialog: (
      _moduleName: string,
      _image: types.IHeaderImage,
      select: types.SelectCallback,
      cont: types.ContinueCallback,
      cancel: types.CancelCallback
    ): void => {
      selectCallback = select;
      contCallback = cont;
      _cancelCallback = cancel;
    },
    uiEndDialog: (): void => {
      selectCallback = null;
      contCallback = null;
      _cancelCallback = null;
    },
    uiUpdateState: (_installSteps: types.IInstallStep[], currentStep: number): void => {
      if (dialogInProgress) {
        return;
      }
      if (!contCallback) {
        return;
      }

      dialogInProgress = true;

      if (unattended) {
        contCallback(true, currentStep);
      } else if (dialogChoices && dialogChoices.length > 0) {
        const choice = dialogChoices.find(c => c.stepId === currentStep);
        if (choice && selectCallback) {
          selectCallback(choice.stepId, choice.groupId, choice.pluginIds);
        }
        contCallback(true, currentStep);
      }

      dialogInProgress = false;
    }
  };
};

// Helper to normalize instruction for comparison
const normalizeInstruction = (inst: Instruction): string => {
  const parts = [inst.type];
  if (inst.source) parts.push(inst.source.replace(/\\/g, '/').toLowerCase());
  if (inst.destination) parts.push(inst.destination.replace(/\\/g, '/').toLowerCase());
  return parts.join('|');
};

// Helper to compare instructions
const compareInstructions = (actual: types.InstallInstruction[], expected: Instruction[]): boolean => {
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

// Run a single test case
async function runTestCase(t: any, testCase: TestCase): Promise<void> {
  const archive = await preloadArchive(testCase.archiveFile, testCase.game);

  try {
    const { files, fileCache } = archive;
    const stopPatterns = getStopPatterns(testCase);

    // Create sync file system using the cache
    // Note: The native library uses backslash-separated paths, but our cache uses forward slashes
    const syncFs = new NativeFileSystem(
      (filePath: string, offset: number, length: number): Uint8Array | null => {
        // Native library sends backslash paths, convert to forward slash for cache lookup
        const normalizedPath = filePath.replace(/\\/g, '/').toLowerCase();
        const content = fileCache.get(normalizedPath);
        if (!content) return null;

        if (length === -1) length = content.length;
        if (offset > 0 || length < content.length) {
          const end = Math.min(offset + length, content.length);
          return content.slice(offset, end);
        }
        return content;
      },
      (directoryPath: string, _pattern: string, _searchType: number): string[] | null => {
        // Native library sends backslash paths, normalize to forward slash for matching
        const normalizedDir = directoryPath.replace(/\\/g, '/').toLowerCase();
        const result: string[] = [];
        // files array has backslash paths (matching native library expectations)
        for (const file of files) {
          // Convert file to forward slash for comparison
          const normalizedFile = file.replace(/\\/g, '/').toLowerCase();
          if (normalizedDir === '' || normalizedFile.startsWith(normalizedDir + '/') || normalizedFile.startsWith(normalizedDir)) {
            // Return the original backslash-separated path
            result.push(file);
          }
        }
        return result;
      },
      (directoryPath: string): string[] | null => {
        // Native library sends backslash paths, normalize to forward slash for matching
        const normalizedDir = directoryPath.replace(/\\/g, '/').toLowerCase();
        const dirs = new Set<string>();
        for (const file of files) {
          // Convert file to forward slash for comparison
          const normalizedFile = file.replace(/\\/g, '/').toLowerCase();
          if (normalizedDir === '' || normalizedFile.startsWith(normalizedDir + '/') || normalizedFile.startsWith(normalizedDir)) {
            const remaining = normalizedDir === '' ? normalizedFile : normalizedFile.slice(normalizedDir.length + 1);
            const parts = remaining.split('/').filter(p => p.length > 0);
            if (parts.length > 1) {
              dirs.add(parts[0]);
            }
          }
        }
        return Array.from(dirs);
      }
    );
    syncFs.setCallbacks();

    // Create callbacks
    const callbacks = createDeterministicUICallbacks(
      testCase.dialogChoices,
      testCase.gameVersion,
      testCase.extenderVersion
    );

    const installer = new NativeModInstaller(
      callbacks.pluginsGetAll,
      callbacks.contextGetAppVersion,
      callbacks.contextGetCurrentGameVersion,
      callbacks.contextGetExtenderVersion,
      callbacks.uiStartDialog,
      callbacks.uiEndDialog,
      callbacks.uiUpdateState
    );

    // Run install
    const result = await installer.install(
      files,
      stopPatterns,
      testCase.pluginPath,
      '', // scriptPath - empty, auto-detected
      testCase.preset ?? null,
      testCase.validate ?? true
    );

    // Assertions
    t.truthy(result, 'Install should return a result');
    t.truthy(result!.instructions, 'Result should have instructions');

    // Compare instructions
    const instructionsMatch = compareInstructions(result!.instructions, testCase.expectedInstructions);
    if (!instructionsMatch) {
      t.log('Expected instructions:', testCase.expectedInstructions);
      t.log('Actual instructions:', result!.instructions.map(i => ({
        type: i.type,
        source: i.source,
        destination: i.destination
      })));
    }
    t.true(instructionsMatch, 'Instructions should match expected');

    if (isDebug) {
      t.is(allocAliveCount(), 0, 'No memory leaks');
    }

  } finally {
    await archive.close();
  }
}

// Generate tests from shared JSON data (supports both .zip and .7z)
for (const testCase of getAllTestCases()) {
  test(`${testCase.game}: ${testCase.name}`, async (t) => {
    await runTestCase(t, testCase);
  });
}
