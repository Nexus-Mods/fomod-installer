import * as types from './types';

export class NativeLogger implements types.Logger {
  private manager: types.Logger;

  public constructor(
    log: (level: number, message: string) => void
  ) {
    const addon: types.ILoggerExtension = require('./../build/modinstaller.node');
    this.manager = new addon.Logger(
      log
    );
  }

  public setCallbacks(): void {
    return this.manager.setCallbacks();
  }

  public disposeDefaultLogger(): void {
    return this.manager.disposeDefaultLogger();
  }

  public static setDefaultCallbacks = (): void => {
    const addon: types.ILoggerExtension = require('./../build/modinstaller.node');
    return addon.Logger.setDefaultCallbacks();
  }
}