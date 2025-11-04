import {
  SupportedResult, InstallResult, IHeaderImage,
  SelectCallback, ContinueCallback, CancelCallback, IInstallStep
} from ".";

export interface INativeExtension {
  ModInstaller: new (
    pluginsGetAllAsync: (activeOnly: boolean) => Promise<string[]>,
    contextGetAppVersionAsync: () => Promise<string>,
    contextGetCurrentGameVersionAsync: () => Promise<string>,
    contextGetExtenderVersionAsync: (extender: string) => Promise<string>,
    uiStartDialog: (moduleName: string, image: IHeaderImage, selectCallback: SelectCallback, contCallback: ContinueCallback, cancelCallback: CancelCallback) => void | Promise<void>,
    uiEndDialog: () => void | Promise<void>,
    uiUpdateState: (installSteps: IInstallStep[], currentStep: number) => void | Promise<void>,
    readFileContent: (filePath: string, offset: number, length: number) => Uint8Array | null,
    readDirectoryFileList: (directoryPath: string, pattern: string, searchType: number) => string[] | null,
    readDirectoryList: (directoryPath: string) => string[] | null
  ) => ModInstaller
  testSupported(files: string[], allowedTypes: string[]): SupportedResult;
}

export type ModInstaller = {
  constructor(): ModInstaller;

  install(files: string[], stopPatterns: string[], pluginPath: string, scriptPath: string,
    preset: any, validate: boolean): Promise<InstallResult | null>;
};
