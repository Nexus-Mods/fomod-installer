
export interface LoggerConstructor {
  new(
    log: (level: number, message: string) => void
  ): Logger;

  setDefaultCallbacks(): void;
}

export interface Logger {
  setCallbacks(): void;
  disposeDefaultLogger(): void;
}

export interface ILoggerExtension {
  Logger: LoggerConstructor;
}
