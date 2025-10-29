# ModInstallerIPC.Tests

This test project provides comprehensive testing for the ModInstallerIPC executable by spawning the IPC process and communicating with it via TCP sockets, reusing the same test data infrastructure used by other test projects.

## Overview

The ModInstallerIPC.Tests project tests the full IPC stack:
- **Process spawning**: Launches ModInstallerIPC.exe as a child process
- **TCP communication**: Establishes JSON-based message protocol over TCP
- **Callback handling**: Implements client-side delegates that respond to server callbacks
- **Test data reuse**: Shares test data with ModInstaller.Adaptor.Typed.Tests and ModInstaller.Native.Tests

## Architecture

```
ModInstallerIPC.Tests (C# test runner)
    ↓ spawns and connects via TCP
ModInstallerIPC.exe (IPC server)
    ↓ uses
ModInstaller.Adaptor.Dynamic (dynamic delegates)
    ↓ calls
Scripting engines (XmlScript, CSharpScript)
    ↓ callbacks to
ModInstallerIPC.Tests (via JSON-RPC)
```

## Project Structure

```
ModInstallerIPC.Tests/
├── ModInstallerIPC.Tests.csproj
├── GlobalSetup.cs                    - Assembly-level test configuration
├── TestSupportedTests.cs             - Tests for TestSupported() IPC method
├── InstallTests.cs                   - Tests for Install() IPC method
├── Utils/
│   └── IPCTestHarness.cs            - Main IPC client/server communication
└── Delegates/
    ├── ArchiveFileSystem.cs         - File system implementation reading from test archives
    └── IPCDelegates.cs              - Delegate implementations for test scenarios
```

## Key Components

### IPCTestHarness

The `IPCTestHarness` class is the core of the testing infrastructure:

- **Process Management**: Spawns ModInstallerIPC.exe with a dynamically assigned TCP port
- **Message Protocol**: Implements JSON-based message serialization with `\uFFFF` delimiter
- **Bidirectional Communication**:
  - Client → Server: TestSupported, Install commands
  - Server → Client: Callback invocations (Invoke) and responses
- **Async Coordination**: Uses `TaskCompletionSource` for request/response correlation
- **Callback Registry**: Maps callback names to delegate functions

#### Message Flow Example

1. **Test → Harness**: Call `InstallAsync()` with test data
2. **Harness → IPC**: Send JSON Install command with file list and delegates
3. **IPC → Harness**: Invoke callbacks (e.g., "getAppVersion")
4. **Harness → Test Delegates**: Call registered C# functions
5. **Test Delegates → Harness**: Return result
6. **Harness → IPC**: Send Reply message with result
7. **IPC → Harness**: Return final installation instructions
8. **Harness → Test**: Return structured result

### IPCDelegates

Wraps all delegate implementations for IPC testing:

- **Context Delegates**: GetAppVersion, GetCurrentGameVersion, CheckIfFileExists, etc.
- **Plugin Delegates**: GetAllPlugins, IsPluginActive, IsPluginPresent
- **INI Delegates**: GetIniString, GetIniInt (stub implementations)
- **UI Delegates**: StartDialog, UpdateState, EndDialog (auto-continue mode)

### ArchiveFileSystem

Provides `IFileSystem` implementation that reads from SharpCompress archives:
- Reads file contents from test ZIP/7z archives
- Supports directory listing
- Used to simulate game data folder for condition checks

## Test Data Sources

The project uses the shared `TestData` project for test cases:

### TestSupportedTests
- **BasicData**: Basic installer format detection
- **XmlData**: XML script format detection
- **LiteData**: Lite script format detection

### InstallTests
- **SkyrimData**: Real-world Skyrim mod installation
- **Fallout4Data**: Real-world Fallout 4 mod installation
- **FomodComplianceTestsData**: FOMOD specification compliance tests
- **CSharpTestCaseData**: C# script-based installers (commented out)

Each test case includes:
- Embedded mod archive (ZIP/7z)
- Expected installation instructions
- Mock plugin list
- Version information
- Optional dialog choices for deterministic UI testing

## Running Tests

### Prerequisites

1. **Build ModInstallerIPC**: Run `npm run build` from repository root to create `dist/ModInstallerIPC.exe`
2. **.NET 9.0 SDK**: Required for building and running tests
3. **Test Data Archives**: Embedded in TestData project (automatically extracted)

### Running via dotnet

```bash
# Run all tests
dotnet test ModInstallerIPC.Tests

# Run with verbose output
dotnet test ModInstallerIPC.Tests -v detailed

# Run specific test class
dotnet test ModInstallerIPC.Tests --filter "FullyQualifiedName~InstallTests"
```

### Running via IDE

Open the solution in Visual Studio or Rider and use the built-in test runner. Tests will appear in the Test Explorer.

## Comparison with Other Test Projects

| Project | Tests | IPC Layer | Delegates | Use Case |
|---------|-------|-----------|-----------|----------|
| **ModInstaller.Adaptor.Typed.Tests** | Direct API | No | Strongly-typed | Library integration |
| **ModInstaller.Native.Tests** | Native DLL | No | Native callbacks | C++/FFI integration |
| **ModInstallerIPC.Tests** | Process + TCP | Yes | JSON-RPC | Node.js integration |

## Implementation Notes

### Why a Separate Frontend?

The term "frontend" refers to the adaptor layer that wraps the core ModInstaller functionality. Each frontend provides:

1. **Different calling conventions**: Typed (C# Task), Dynamic (JSON objects), Native (C pointers)
2. **Different delegate mechanisms**: Direct callbacks, dynamic invocation, unmanaged function pointers
3. **Different return types**: Strongly-typed objects, dictionaries, marshaled structs

ModInstallerIPC.Tests creates a **test-specific frontend** that:
- Communicates via the IPC protocol (JSON over TCP)
- Registers callbacks that return test data
- Validates that the full IPC stack works end-to-end

### Key Differences from Other Tests

1. **Process Isolation**: Tests spawn a separate process, testing process management and cleanup
2. **Protocol Testing**: Validates JSON serialization, message framing, and error handling
3. **Async Callbacks**: Tests bidirectional async communication (not just request/response)
4. **Real IPC**: Uses actual TCP sockets (not in-process mocks)

### Current Limitations

1. **UI Delegates**: Currently implements auto-continue mode only
   - TODO: Support predetermined dialog choices like `DeterministicUIContext`
   - Would enable testing interactive installer paths through IPC

2. **Error Handling**: Basic error propagation
   - TODO: Test timeout scenarios
   - TODO: Test process crash recovery

3. **Performance**: Slower than direct API tests due to process spawning
   - Each test spawns a new process (~500ms overhead)
   - Tests run serially (`[NotInParallel]`) to avoid port conflicts

## Future Enhancements

### 1. Dialog Choice Support

Implement deterministic UI interaction through IPC:

```csharp
// In IPCDelegates.cs
public Task UpdateState(object[] installSteps, int currentStep)
{
    if (_dialogChoices is { Count: > 0 })
    {
        var option = _dialogChoices.FirstOrDefault(x => x.StepId == currentStep);
        // Send selection back to IPC server
        // Wait for continuation
    }
}
```

### 2. Named Pipe Support

Test Windows named pipe transport in addition to TCP:

```csharp
var harness = await new IPCTestHarness()
    .UseNamedPipe("ModInstallerIPC_Test_" + Guid.NewGuid())
    .InitializeAsync();
```

### 3. Sandboxing Tests

Test the AppContainer and low-integrity process modes:

```csharp
var harness = await new IPCTestHarness()
    .UseSandboxMode(SandboxMode.AppContainer)
    .InitializeAsync();
```

### 4. Stress Testing

Long-running tests for memory leaks and resource cleanup:

```csharp
[Test]
public async Task StressTest_MultipleInstalls()
{
    for (int i = 0; i < 100; i++)
    {
        await using var harness = await new IPCTestHarness().InitializeAsync();
        await harness.InstallAsync(...);
    }
    // Verify no orphaned processes
}
```

## Troubleshooting

### ModInstallerIPC.exe not found

**Error**: `FileNotFoundException: ModInstallerIPC.exe not found at ...`

**Solution**: Run `npm run build` or `dotnet publish ModInstallerIPC` to create the executable

### Port already in use

**Error**: `Failed to connect to local port 12345`

**Solution**: The test harness dynamically assigns free ports, but if you see this:
1. Ensure no orphaned ModInstallerIPC processes: `npm run cleanup`
2. Check firewall settings

### Tests hang indefinitely

**Cause**: Deadlock in callback handling or missing Reply message

**Solution**:
1. Check stderr output: `dotnet test ModInstallerIPC.Tests -v detailed`
2. Enable IPC logging in `IPCTestHarness.cs` (uncomment `Console.WriteLine` statements)
3. Verify callback names match between client and server

### Timeout on slow machines

**Cause**: Process startup takes longer than expected

**Solution**: Increase startup delay in `IPCTestHarness.InitializeAsync()`:
```csharp
// Wait for the process to start listening
await Task.Delay(1000); // Increase from 500ms
```

## Related Documentation

- [CLAUDE.md](../CLAUDE.md) - Overall architecture and IPC protocol
- [ModInstallerIPC/Server.cs](../ModInstallerIPC/Server.cs) - IPC server implementation
- [TestData project](../TestData/) - Shared test data infrastructure
- [index.js](../index.js) - Node.js IPC client (production)
