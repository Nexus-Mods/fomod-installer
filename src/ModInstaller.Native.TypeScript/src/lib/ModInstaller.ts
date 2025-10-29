import * as types from './types';

type Omit<T, K extends keyof T> = Pick<T, Exclude<keyof T, K>>
type ModInstallerWithoutConstructor = Omit<types.ModInstaller, "constructor">;
export class NativeModInstaller implements ModInstallerWithoutConstructor {
  private manager: types.ModInstaller;

  public constructor(
    pluginsGetAllAsync: (activeOnly: boolean) => Promise<string[]>,
    contextGetAppVersionAsync: () => Promise<string>,
    contextGetCurrentGameVersionAsync: () => Promise<string>,
    contextGetExtenderVersionAsync: (extender: string) => Promise<string>,
    uiStartDialog: (moduleName: string, image: types.IHeaderImage, selectCallback: types.SelectCallback, contCallback: types.ContinueCallback, cancelCallback: types.CancelCallback) => void | Promise<void>,
    uiEndDialog: () => void | Promise<void>,
    uiUpdateState: (installSteps: types.IInstallStep[], currentStep: number) => void | Promise<void>,
    readFileContent: (filePath: string, offset: number, length: number) => Uint8Array | null,
    readDirectoryFileList: (directoryPath: string, pattern: string, searchType: number) => string[] | null,
    readDirectoryList: (directoryPath: string) => string[] | null
  ) {
    const addon: types.INativeExtension = require('./../../modinstaller.node');
    this.manager = new addon.ModInstaller(
      pluginsGetAllAsync,
      contextGetAppVersionAsync,
      contextGetCurrentGameVersionAsync,
      contextGetExtenderVersionAsync,
      uiStartDialog,
      uiEndDialog,
      uiUpdateState,
      readFileContent,
      readDirectoryFileList,
      readDirectoryList
    );
  }

  public install(files: string[], stopPatterns: string[], pluginPath: string,
    scriptPath: string, preset: any, validate: boolean): Promise<types.InstallResult> {
    return this.manager.install(files, stopPatterns, pluginPath, scriptPath, preset, validate);
  }
}

export const testSupported = (files: string[], allowedTypes: string[]): types.SupportedResult => {
  const addon: types.INativeExtension = require('./../../modinstaller.node');
  return addon.testSupported(files, allowedTypes);
}