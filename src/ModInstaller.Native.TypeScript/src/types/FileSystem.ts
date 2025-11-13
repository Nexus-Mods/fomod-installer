
export interface FileSystemConstructor {
  new(
    readFileContent: (filePath: string, offset: number, length: number) => Uint8Array | null,
    readDirectoryFileList: (directoryPath: string, pattern: string, searchType: number) => string[] | null,
    readDirectoryList: (directoryPath: string) => string[] | null
  ): FileSystem;

  setDefaultCallbacks(): void;
}

export interface FileSystem {
  setCallbacks(): void;
}

export interface IFileSystemExtension {
  FileSystem: FileSystemConstructor;
}
