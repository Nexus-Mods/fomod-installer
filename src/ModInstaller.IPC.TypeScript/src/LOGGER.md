# Logger Configuration

The IPC module includes a configurable logging system that allows you to inject Vortex's logging infrastructure.

## Default Behavior

By default, the module uses console logging:
```typescript
[2025-01-11T10:30:45.123Z] [INFO] Message here
```

## Using Vortex's Logger

To use Vortex's logging system, call `setLogger()` before creating any IPC connections:

```typescript
import { setLogger } from 'fomod-installer-ipc';
import { log as vortexLog } from '../../../util/log';

// Configure the IPC module to use Vortex's logger
setLogger(vortexLog);

// Now all IPC module logs will use Vortex's logging system
// including proper log levels, file output, and UI integration
```

## API

### `setLogger(logFn: LogFunction): void`
Sets a custom log function. The function should match the signature:
```typescript
type LogFunction = (level: LogLevel, message: string, metadata?: any) => void;
type LogLevel = 'debug' | 'info' | 'warn' | 'error';
```

### `resetLogger(): void`
Resets to the default console-based logger.

### `getErrorMessage(err: unknown): string`
Safely extracts error messages from unknown error types. Useful in catch blocks.

## Example Integration

```typescript
// In your Vortex extension initialization:
import {
  BaseIPCConnection,
  setLogger,
  NamedPipeTransport,
  RegularProcessLauncher
} from 'fomod-installer-ipc';
import { log as vortexLog } from '../../../util/log';

function init(context: IExtensionContext): boolean {
  // Configure logging first
  setLogger(vortexLog);

  // Now create IPC connections - they'll use Vortex's logger
  const transport = new NamedPipeTransport();
  const launcher = new RegularProcessLauncher();
  const connection = new BaseIPCConnection({
    transport,
    launcher,
    exePath: 'path/to/ModInstallerIPC.exe'
  });

  // All logging from the IPC module will now appear in Vortex's logs
  await connection.connect();

  return true;
}
```

## Benefits

- **Consistent logging**: All logs appear in Vortex's log files
- **Log level control**: Respects Vortex's log level settings
- **UI integration**: Debug logs can be viewed in Vortex's debug interface
- **File rotation**: Logs are managed by Vortex's log rotation system
- **No console pollution**: Logs don't appear in the console during production use
