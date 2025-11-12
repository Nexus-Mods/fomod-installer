# Process Launcher Architecture

This directory contains the implementation of a clean, interface-based architecture for launching processes with different security levels.

## Overview

The launcher architecture separates process spawning logic from the main IPC connection code, making it easier to:
- Add new security levels
- Test different launchers independently
- Maintain and debug spawn logic
- Switch between security levels at runtime

## Architecture

```
IProcessLauncher (interface)
    │
    ├── RegularProcessLauncher - No security restrictions
    └── SandboxProcessLauncher - Windows App Container
```

## Files

### Core Interface

- **[IProcessLauncher.ts](../IProcessLauncher.ts)** - Main interface and types
  - `IProcessLauncher` - Interface all launchers must implement
  - `ProcessLaunchOptions` - Standard spawn options
  - `ChildProcessCompatible` - Interface for pseudo-ChildProcess objects

### Launcher Implementations

- **[RegularProcessLauncher.ts](RegularProcessLauncher.ts)** - Regular security level
  - Uses Node.js `spawn()` directly
  - No security restrictions
  - Used as fallback when sandboxing fails

- **[SandboxProcessLauncher.ts](SandboxProcessLauncher.ts)** - App Container sandbox
  - Uses `winapi-bindings` for Windows App Container
  - Grants file system and named pipe access
  - Creates pseudo-ChildProcess for API compatibility

### Index

- **[index.ts](index.ts)** - Clean module exports

## Security Levels

### Regular

**Features**:
- No security restrictions
- Uses standard Node.js `spawn()`
- 100% compatibility
- Used as fallback

**Use Cases**:
- Sandboxing disabled by user
- Sandboxing not supported (Windows 7 and earlier)
- Sandbox launch failed

### Sandbox (App Container)

**Features**:
- Maximum security isolation
- Restricted file system access (only granted paths)
- No registry access
- No network access
- Windows Forms UI supported

**Requirements**:
- Windows 8 or later
- Named Pipe transport only
- `winapi-bindings` library

**Implementation**:
1. Create App Container
2. Grant named pipe access
3. Grant file system access (exe directory, cwd, temp)
4. Spawn process using `RunInContainer()`
5. Cleanup App Container on dispose

## Implementation Details

### Pseudo-ChildProcess

The sandbox launcher returns a `ChildProcessCompatible` object instead of a real `ChildProcess` because `winapi.RunInContainer()` uses callbacks:

```typescript
const pseudoProcess: any = new EventEmitter();
pseudoProcess.stdin = null;
pseudoProcess.stdout = new EventEmitter();
pseudoProcess.stderr = new EventEmitter();
pseudoProcess.killed = false;
pseudoProcess.exitCode = null;

pseudoProcess.kill = () => {
  pseudoProcess.killed = true;
  return true;
};

// RunInContainer callbacks emit to stdout
winapi.RunInContainer(
  containerName,
  commandLine,
  cwd,
  (code) => pseudoProcess.emit('exit', code, null),
  (message) => pseudoProcess.stdout.emit('data', Buffer.from(message))
);
```

### File System Access Grants

The sandbox launcher grants access to:
1. **Executable directory** - All files (.exe, .dll, .config, etc.)
2. **Working directory** - If different from exe directory
3. **TEMP directory** - For .NET runtime temporary files

```typescript
winapi.GrantAppContainer(
  containerName,
  exeDir,
  'file_object',
  ['generic_read', 'generic_execute', 'traverse', 'list_directory']
);
```

### Named Pipe Access Grants

The sandbox launcher grants pipe access **during** pipe server creation via a callback (to avoid race conditions):

```typescript
// Create callback for granting pipe access
const launcher = new SandboxProcessLauncher({ containerName: 'MyContainer' });
const callback = launcher.createPipeAccessCallback(namedPipeTransport);

// Grant pipe access during server creation
await namedPipeTransport.createServers(callback);

// Then spawn process
const process = await launcher.launch(...);
```

This callback pattern keeps the transport layer agnostic about sandboxing details.

## Benefits of This Architecture

### Separation of Concerns
- Spawn logic separated from IPC connection logic
- Each launcher is independent and self-contained
- Easy to understand and maintain

### Testability
- Each launcher can be tested in isolation
- Mock launchers can be created for testing

### Extensibility
- New security levels can be added by implementing `IProcessLauncher`
- No changes needed to IPCConnection or other code

### Error Handling
- Launchers can throw specific errors
- Cleanup is guaranteed via `dispose()` pattern

### Code Reuse
- Common spawn options defined once
- File system access logic in SandboxProcessLauncher
- Pseudo-process creation logic reusable

## Testing

```typescript
import { RegularProcessLauncher } from './launchers';

describe('RegularProcessLauncher', () => {
  it('should launch process successfully', async () => {
    const launcher = new RegularProcessLauncher();

    const process = await launcher.launch(
      'path/to/exe',
      ['--arg1', '--arg2'],
      options
    );

    expect(process).toBeDefined();
    expect(process.pid).toBeGreaterThan(0);

    await launcher.cleanup();
  });
});
```

## Troubleshooting

### "Failed to grant App Container access"
- Ensure `winapi-bindings` is installed
- Check Windows version (requires Windows 8+)
- Verify Named Pipe transport is being used

### "Process exits immediately with code 1"
- Check file system access grants
- Verify TEMP directory access
- Enable detailed logging to see which file access fails

### "Timeout waiting for handshake"
- Ensure named pipe access was granted **before** spawning
- Check that process can write to named pipe
- Verify .NET runtime dependencies are accessible
