import {
  SupportedResult, InstallResult, IHeaderImage,
  SelectCallback, ContinueCallback, CancelCallback, IInstallStep
} from ".";

export interface ModInstallerConstructor {
  new (
    pluginsGetAll: (activeOnly: boolean) => string[],
    contextGetAppVersion: () => string,
    contextGetCurrentGameVersion: () => string,
    contextGetExtenderVersion: (extender: string) => string,
    uiStartDialog: (moduleName: string, image: IHeaderImage, selectCallback: SelectCallback, contCallback: ContinueCallback, cancelCallback: CancelCallback) => void,
    uiEndDialog: () => void,
    uiUpdateState: (installSteps: IInstallStep[], currentStep: number) => void
  ): ModInstaller;

  testSupported(files: string[], allowedTypes: string[]): SupportedResult;
}

export interface ModInstaller {
  install(files: string[], stopPatterns: string[], pluginPath: string, scriptPath: string,
    preset: any, validate: boolean): Promise<InstallResult | null>;
}

export interface IModInstallerExtension {
  ModInstaller: ModInstallerConstructor;
}
