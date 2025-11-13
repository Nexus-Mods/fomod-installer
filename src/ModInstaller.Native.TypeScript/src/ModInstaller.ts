import * as types from './types';

export class NativeModInstaller implements types.ModInstaller {
  private manager: types.ModInstaller;

  public constructor(
    pluginsGetAll: (activeOnly: boolean) => string[],
    contextGetAppVersion: () => string,
    contextGetCurrentGameVersion: () => string,
    contextGetExtenderVersion: (extender: string) => string,
    uiStartDialog: (moduleName: string, image: types.IHeaderImage, selectCallback: types.SelectCallback, contCallback: types.ContinueCallback, cancelCallback: types.CancelCallback) => void,
    uiEndDialog: () => void,
    uiUpdateState: (installSteps: types.IInstallStep[], currentStep: number) => void
  ) {
    const addon: types.IModInstallerExtension = require('./../build/modinstaller.node');
    this.manager = new addon.ModInstaller(
      pluginsGetAll,
      contextGetAppVersion,
      contextGetCurrentGameVersion,
      contextGetExtenderVersion,
      uiStartDialog,
      uiEndDialog,
      uiUpdateState
    );
  }

  public install(files: string[], stopPatterns: string[], pluginPath: string,
    scriptPath: string, preset: any, validate: boolean): Promise<types.InstallResult | null> {
    return this.manager.install(files, stopPatterns, pluginPath, scriptPath, preset, validate);
  }

  public static testSupported = (files: string[], allowedTypes: string[]): types.SupportedResult => {
    const addon: types.IModInstallerExtension = require('./../build/modinstaller.node');
    return addon.ModInstaller.testSupported(files, allowedTypes);
  }
}