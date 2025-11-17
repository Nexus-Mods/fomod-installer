import * as types from './types';

export class NativeFileSystem implements types.FileSystem {
  private manager: types.FileSystem;

  public constructor(
    readFileContent: (filePath: string, offset: number, length: number) => Uint8Array | null,
    readDirectoryFileList: (directoryPath: string, pattern: string, searchType: number) => string[] | null,
    readDirectoryList: (directoryPath: string) => string[] | null
  ) {
    const addon: types.IFileSystemExtension = require('./../build/modinstaller.node');
    this.manager = new addon.FileSystem(
      readFileContent,
      readDirectoryFileList,
      readDirectoryList
    );
  }

  public setCallbacks(): void {
    return this.manager.setCallbacks();
  }

  public static setDefaultCallbacks = (): void => {
    const addon: types.IFileSystemExtension = require('./../build/modinstaller.node');
    return addon.FileSystem.setDefaultCallbacks();
  }
}