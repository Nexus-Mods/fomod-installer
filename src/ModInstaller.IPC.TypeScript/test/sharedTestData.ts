import * as path from 'path';
import * as fs from 'fs';
import StreamZip from 'node-stream-zip';
// eslint-disable-next-line @typescript-eslint/no-var-requires
const Seven = require('node-7z');
// eslint-disable-next-line @typescript-eslint/no-var-requires
const sevenBin = require('7zip-bin');

// Path to the shared test data directory
const SHARED_DATA_DIR = path.resolve(__dirname, '../../../../test/TestData/Shared');
const TEST_DATA_DIR = path.resolve(__dirname, '../../../../test/TestData/Data');

// Types matching the JSON schema
export interface SelectedOption {
  stepId: number;
  groupId: number;
  pluginIds: number[];
}

export interface Instruction {
  type: string;
  source?: string;
  destination?: string;
  section?: string;
  key?: string;
  value?: string;
  priority?: number;
  data?: any;
}

export interface TestCase {
  name: string;
  game: string;
  mod: string;
  archiveFile: string;
  stopPatterns: string[];
  pluginPath: string;
  appVersion?: string;
  gameVersion?: string;
  extenderVersion?: string;
  installedPlugins?: string[];
  dialogChoices?: SelectedOption[];
  preset?: any;
  validate?: boolean;
  installerType?: string;
  expectedMessage?: string;
  expectedInstructions: Instruction[];
}

export interface GameTestFile {
  game: string;
  stopPatterns: string[];
  testCases: Array<Omit<TestCase, 'game' | 'stopPatterns'>>;
}

// Cache for loaded data per game
const gameDataCache: Map<string, GameTestFile> = new Map();

// List of game files to load - IPC tests only run CSharpScript installers
// Note: falloutnv.json is excluded because its C# script creates a WinForms UI
// that requires manual interaction and cannot be automated in tests
const GAME_FILES = [
  'csharp-script.json'
  // 'falloutnv.json' - excluded: requires interactive WinForms UI
];

/**
 * Loads a single game test data file.
 */
function loadGameFile(filename: string): GameTestFile | null {
  const filePath = path.join(SHARED_DATA_DIR, filename);
  if (!fs.existsSync(filePath)) {
    return null;
  }

  const json = fs.readFileSync(filePath, 'utf-8');
  return JSON.parse(json) as GameTestFile;
}

/**
 * Loads all game test files and returns combined test cases.
 */
function loadAllTestCases(): TestCase[] {
  const allTestCases: TestCase[] = [];

  for (const filename of GAME_FILES) {
    const cached = gameDataCache.get(filename);
    let gameData: GameTestFile | null;

    if (cached) {
      gameData = cached;
    } else {
      gameData = loadGameFile(filename);
      if (gameData) {
        gameDataCache.set(filename, gameData);
      }
    }

    if (gameData) {
      // Expand test cases with game and stopPatterns
      for (const tc of gameData.testCases) {
        allTestCases.push({
          ...tc,
          game: gameData.game,
          stopPatterns: gameData.stopPatterns
        });
      }
    }
  }

  return allTestCases;
}

/**
 * Gets the stop patterns for a test case.
 */
export function getStopPatterns(testCase: TestCase): string[] {
  return testCase.stopPatterns || [];
}

/**
 * Gets all C# script test cases.
 * Only includes tests with installerType: 'CSharpScript'.
 */
export function getCSharpScriptTestCases(): TestCase[] {
  return loadAllTestCases().filter(tc => tc.installerType === 'CSharpScript');
}

/**
 * Gets all test cases (only CSharpScript for IPC tests).
 */
export function getAllTestCases(): TestCase[] {
  return getCSharpScriptTestCases();
}

/**
 * Resolves the full path to an archive file.
 */
function resolveArchivePath(archiveFile: string, game: string): string {
  // Archive files are stored in TestData/Data/{Game}/ or TestData/Data/
  let fullPath = path.join(TEST_DATA_DIR, game, archiveFile);
  if (!fs.existsSync(fullPath)) {
    // Try without game subdirectory
    fullPath = path.join(TEST_DATA_DIR, archiveFile);
  }
  return fullPath;
}

/**
 * Archive file system helper class for reading zip archives.
 */
class ZipArchiveFileSystem {
  private zip: StreamZip.StreamZipAsync;
  private entries: Map<string, StreamZip.ZipEntry> = new Map();

  private constructor(zip: StreamZip.StreamZipAsync) {
    this.zip = zip;
  }

  static async open(fullPath: string): Promise<ZipArchiveFileSystem> {
    // Disable name validation to allow Windows-style backslash paths in archives
    const zip = new StreamZip.async({ file: fullPath, skipEntryNameValidation: true });
    const afs = new ZipArchiveFileSystem(zip);
    await afs.loadEntries();
    return afs;
  }

  private async loadEntries(): Promise<void> {
    const entries = await this.zip.entries();
    for (const [name, entry] of Object.entries(entries)) {
      const normalizedName = name.replace(/\\/g, '/');
      this.entries.set(normalizedName.toLowerCase(), entry);
    }
  }

  getFileList(): string[] {
    const result: string[] = [];
    for (const [_, entry] of this.entries) {
      if (!entry.isDirectory) {
        result.push(entry.name.replace(/\\/g, '/'));
      }
    }
    return result;
  }

  async readFileContent(filePath: string): Promise<Uint8Array | null> {
    const normalizedPath = filePath.replace(/\\/g, '/').toLowerCase();
    const entry = this.entries.get(normalizedPath);
    if (!entry || entry.isDirectory) {
      return null;
    }

    try {
      const buffer = await this.zip.entryData(entry);
      return new Uint8Array(buffer);
    } catch {
      return null;
    }
  }

  async close(): Promise<void> {
    await this.zip.close();
  }
}

/**
 * Archive file system helper class for reading 7z archives.
 * Extracts to a temp directory and reads from there.
 */
class SevenZipArchiveFileSystem {
  private tempDir: string;
  private files: string[] = [];

  private constructor(tempDir: string) {
    this.tempDir = tempDir;
  }

  static async open(fullPath: string): Promise<SevenZipArchiveFileSystem> {
    // Create temp directory
    const tempDir = fs.mkdtempSync(path.join(require('os').tmpdir(), '7z-test-'));

    // Extract archive
    await new Promise<void>((resolve, reject) => {
      const stream = Seven.extractFull(fullPath, tempDir, {
        $bin: sevenBin.path7za,
        recursive: true
      });
      stream.on('end', () => resolve());
      stream.on('error', (err: Error) => reject(err));
    });

    const afs = new SevenZipArchiveFileSystem(tempDir);
    afs.scanFiles();
    return afs;
  }

  private scanFiles(dir: string = this.tempDir, prefix: string = ''): void {
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
      const relativePath = prefix ? `${prefix}/${entry.name}` : entry.name;
      if (entry.isDirectory()) {
        this.scanFiles(path.join(dir, entry.name), relativePath);
      } else {
        this.files.push(relativePath);
      }
    }
  }

  getFileList(): string[] {
    return this.files;
  }

  async readFileContent(filePath: string): Promise<Uint8Array | null> {
    const normalizedPath = filePath.replace(/\\/g, '/');
    const fullPath = path.join(this.tempDir, normalizedPath);

    if (!fs.existsSync(fullPath)) {
      return null;
    }

    try {
      const buffer = fs.readFileSync(fullPath);
      return new Uint8Array(buffer);
    } catch {
      return null;
    }
  }

  async close(): Promise<void> {
    // Clean up temp directory
    fs.rmSync(this.tempDir, { recursive: true, force: true });
  }
}

/**
 * Preloads all files from an archive into a synchronous cache.
 * This is needed because native callbacks must be synchronous.
 * Supports both .zip and .7z archives.
 *
 * Note: The native library expects backslash-separated paths (Windows style),
 * matching the C# GetNormalizedName() behavior which replaces "/" with "\\".
 */
export async function preloadArchive(archiveFile: string, game: string): Promise<{
  files: string[];
  fileCache: Map<string, Uint8Array>;
  close: () => Promise<void>;
}> {
  const fullPath = resolveArchivePath(archiveFile, game);
  const is7z = archiveFile.toLowerCase().endsWith('.7z');

  const afs = is7z
    ? await SevenZipArchiveFileSystem.open(fullPath)
    : await ZipArchiveFileSystem.open(fullPath);

  const rawFiles = afs.getFileList();
  const fileCache = new Map<string, Uint8Array>();

  for (const file of rawFiles) {
    const content = await afs.readFileContent(file);
    if (content) {
      // Cache uses forward slash lowercase keys for internal lookups
      fileCache.set(file.replace(/\\/g, '/').toLowerCase(), content);
    }
  }

  // Convert to backslash-separated paths for the native library
  // This matches C# GetNormalizedName() which does: entry.Key?.Replace("/", "\\")
  const files = rawFiles.map(f => f.replace(/\//g, '\\'));

  return {
    files,
    fileCache,
    close: () => afs.close()
  };
}

/**
 * Extracts an archive to a temp directory on disk.
 * This is needed for IPC tests because the C# process reads files from disk.
 *
 * Returns the temp directory path, the list of files with full paths,
 * and a cleanup function to remove the temp directory.
 */
export async function extractArchiveToTemp(archiveFile: string, game: string): Promise<{
  tempDir: string;
  files: string[];
  cleanup: () => Promise<void>;
}> {
  const fullPath = resolveArchivePath(archiveFile, game);
  const is7z = archiveFile.toLowerCase().endsWith('.7z');
  const os = require('os');

  // Create temp directory
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'ipc-test-'));

  if (is7z) {
    // Extract 7z archive
    await new Promise<void>((resolve, reject) => {
      const stream = Seven.extractFull(fullPath, tempDir, {
        $bin: sevenBin.path7za,
        recursive: true
      });
      stream.on('end', () => resolve());
      stream.on('error', (err: Error) => reject(err));
    });
  } else {
    // Extract zip archive
    const zip = new StreamZip.async({ file: fullPath, skipEntryNameValidation: true });
    try {
      await zip.extract(null, tempDir);
    } finally {
      await zip.close();
    }
  }

  // Scan extracted files
  const files: string[] = [];

  function scanDir(dir: string, prefix: string = ''): void {
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
      const relativePath = prefix ? `${prefix}\\${entry.name}` : entry.name;
      if (entry.isDirectory()) {
        scanDir(path.join(dir, entry.name), relativePath);
      } else {
        // Return full path on disk for IPC
        files.push(path.join(tempDir, relativePath));
      }
    }
  }

  scanDir(tempDir);

  return {
    tempDir,
    files,
    cleanup: async () => {
      fs.rmSync(tempDir, { recursive: true, force: true });
    }
  };
}
