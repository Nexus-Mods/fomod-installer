# FOMOD IPC Installer

TypeScript implementation for communicating with ModInstallerIPC.exe via TCP sockets.

### Components

#### IPCConnection.ts
Main IPC connection handler that:
- Spawns ModInstallerIPC.exe process
- Establishes TCP socket connection
- Handles message serialization/deserialization
- Manages request/response lifecycle
- Processes callbacks from the server

## Message Protocol

Messages are JSON objects delimited by `\uFFFF` character.

### Message Structure
```typescript
{
  id: string,              // Unique message ID
  payload?: {              // Request payload
    command: string,       // Command name
    ...                    // Command-specific parameters
  },
  callback?: {             // Callback metadata
    id: string,            // Request ID that sent the callback
    type: string           // Callback type
  },
  data?: any,              // Response data or callback arguments
  error?: {                // Error information
    message: string,
    stack?: string,
    name?: string
  }
}
```

## Implementation Status

### ✅ Completed
- TCP socket communication
- Message serialization/deserialization
- Request/response handling
- Callback registration and invocation
- Process lifecycle management
- TestSupported command
- Install command
- Error handling

## Differences from Previous Implementation

This implementation (`installer_fomod_ipc`) differs from the old implementation (`installer_fomod`):

1. **Direct TCP communication** - No intermediate wrapper packages
2. **Simpler message format** - Standard JSON with delimiter
3. **Cleaner separation** - Delegates in separate files
4. **No Edge.js dependency** - Pure Node.js socket communication
5. **Process management** - Better lifecycle handling
6. **Type safety** - Full TypeScript types for messages

### Extending for Other Frameworks

To use this IPC system in a different framework:

```typescript
import { BaseIPCConnection, ConnectionStrategy } from './BaseIPCConnection';

export class MyFrameworkIPCConnection extends BaseIPCConnection {
  protected log(level: string, message: string, metadata?: any): void {
    myFrameworkLogger.log(level, message, metadata);
  }

  protected async fileExists(filePath: string): Promise<boolean> {
    return await myFrameworkFS.exists(filePath);
  }

  // Add framework-specific methods
  public async myCustomCommand(data: any): Promise<any> {
    return await this.sendCommand('MyCustomCommand', data);
  }
}
```
