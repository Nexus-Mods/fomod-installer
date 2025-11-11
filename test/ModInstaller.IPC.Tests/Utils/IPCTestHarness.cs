using FomodInstaller.Interface;

using ModInstaller.IPC.Tests.Delegates;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using TestData;

namespace ModInstaller.IPC.Tests.Utils;

/// <summary>
/// Test harness that spawns ModInstallerIPC.exe and communicates via TCP
/// </summary>
internal class IPCTestHarness : IAsyncDisposable
{
    private Process? _process;
    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private Task? _readerTask;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonNode?>> _pendingReplies = new();
    private readonly ConcurrentDictionary<string, Func<JsonArray?, Task<object?>>> _callbacks = new();
    private bool _disposed;
    private string? _currentRequestId;  // Tracks the current Install request ID for callback invocations

    public IPCTestHarness(TestSupportData data)
    {
        // No callbacks needed
    }

    public IPCTestHarness(InstallData data)
    {
        // Register callbacks
        var delegates = new IPCDelegates(data);
        RegisterCallback("getAppVersion", _ => Task.FromResult<object?>(delegates.GetAppVersion()));
        RegisterCallback("getCurrentGameVersion", _ => Task.FromResult<object?>(delegates.GetCurrentGameVersion()));
        RegisterCallback("getExtenderVersion", args => Task.FromResult<object?>(delegates.GetExtenderVersion(args![0]!.GetValue<string>())));
        RegisterCallback("isExtenderPresent", async _ => await delegates.IsExtenderPresent());
        RegisterCallback("checkIfFileExists", async args => await delegates.CheckIfFileExists(args![0]!.GetValue<string>()));
        RegisterCallback("getExistingDataFile", async args => await delegates.GetExistingDataFile(args![0]!.GetValue<string>()));
        RegisterCallback("getExistingDataFileList", async args => await delegates.GetExistingDataFileList(args![0]!.GetValue<string>(), args[1]!.GetValue<string>(), args[2]!.GetValue<bool>()));
        RegisterCallback("getAllPlugins", args => Task.FromResult<object?>(delegates.GetAllPlugins(args![0]!.GetValue<bool>())));
        RegisterCallback("getIniString", async args => await delegates.GetIniString(args![0]!.GetValue<string>(), args[1]!.GetValue<string>(), args[2]!.GetValue<string>()));
        RegisterCallback("getIniInt", async args => await delegates.GetIniInt(args![0]!.GetValue<string>(), args[1]!.GetValue<string>(), args[2]!.GetValue<string>()));

        // Register UI callbacks using the UI handler
        var uiHandler = new IPCUIHandler(data.DialogChoices, InvokeServerCallback);
        RegisterCallback("startDialog", args =>
        {
            // Pass the message ID that sent this callback
            uiHandler.StartDialog(_currentRequestId!, args!);
            return Task.FromResult<object?>(null);
        });
        RegisterCallback("endDialog", args =>
        {
            uiHandler.EndDialog();
            return Task.FromResult<object?>(null);
        });
        RegisterCallback("updateState", args =>
        {
            // Pass the message ID that sent this callback
            uiHandler.UpdateState(_currentRequestId!, args!);
            return Task.FromResult<object?>(null);
        });
        RegisterCallback("reportError", async args =>
        {
            await uiHandler.ReportError(args!);
            return null;
        });
    }

    public async Task<IPCTestHarness> InitializeAsync()
    {
        // Find a free port by binding to port 0
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        var port = ((IPEndPoint) _listener.LocalEndpoint).Port;

        // Spawn ModInstallerIPC.exe with the port
        var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\src\ModInstaller.IPC\bin\Debug\net9.0-windows\win-x64\ModInstallerIPC.exe");
        //var exePath = @"D:\Git\NexusMods\Vortex\node_modules\fomod-installer-ipc\dist\ModInstallerIPC.exe";
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"ModInstallerIPC.exe not found at {exePath}. Run 'dotnet build' first.");
        }

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = port.ToString(),  // No --pipe flag for TCP
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        _process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[IPC stdout] {e.Data}");
            }
        };

        _process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[IPC stderr] {e.Data}");
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // Wait for the process to connect to us
        _client = await _listener.AcceptTcpClientAsync();
        _stream = _client.GetStream();

        // Read "connected" message
        var buffer = new byte[9];
        await _stream.ReadExactlyAsync(buffer);
        var connected = Encoding.UTF8.GetString(buffer);
        if (connected != "connected")
        {
            throw new InvalidOperationException($"Expected 'connected' message, got: {connected}");
        }

        // Start reading responses in background
        _readerTask = Task.Run(ReaderLoop);

        return this;
    }

    public async Task<TestSupportedResult> TestSupportedAsync(IEnumerable<string> files, IEnumerable<string> allowedTypes)
    {
        var id = Guid.NewGuid().ToString("N");
        var message = new
        {
            id,
            payload = new
            {
                command = "TestSupported",
                files,
                allowedTypes
            }
        };

        var response = await SendAndReceiveAsync(id, message);
        if (response == null) throw new InvalidOperationException("Received null response");

        return new TestSupportedResult
        {
            Supported = response["supported"]?.GetValue<bool>() ?? false,
            RequiredFiles = response["requiredFiles"]?.AsArray().Select(x => x?.GetValue<string>() ?? "").ToArray() ?? Array.Empty<string>()
        };
    }

    public async Task<InstallResult> InstallAsync(
        IEnumerable<string> files,
        IEnumerable<string> stopPatterns,
        string pluginPath,
        string scriptPath,
        JsonDocument? fomodChoices,
        bool validate)
    {
        var id = Guid.NewGuid().ToString("N");

        // Convert JsonDocument to JsonNode if needed
        JsonNode? fomodChoicesNode = fomodChoices != null
            ? JsonNode.Parse(fomodChoices.RootElement.GetRawText())
            : null;

        var message = new
        {
            id,
            payload = new
            {
                command = "Install",
                files,
                stopPatterns,
                pluginPath,
                scriptPath,
                fomodChoices = fomodChoicesNode,
                validate
            }
        };

        var response = await SendAndReceiveAsync(id, message);
        if (response == null) throw new InvalidOperationException("Received null response");

        var instructionsJson = response["instructions"]?.AsArray();
        var instructionJsonWithObjects = instructionsJson?.Select(x => JsonSerializer.Deserialize<InstallInstruction>(x)).ToList();

        return new InstallResult
        {
            Instructions = instructionJsonWithObjects
        };
    }

    private void RegisterCallback(string name, Func<JsonArray?, Task<object?>> callback)
    {
        _callbacks[name] = callback;
    }

    private async Task<object?> InvokeServerCallback(string requestId, string callbackId, JsonArray args)
    {
        // Send an Invoke command to the server to call its callback
        var invokeId = Guid.NewGuid().ToString("N");
        var message = new
        {
            id = invokeId,
            payload = new
            {
                command = "Invoke",
                requestId = requestId,  // ID of the message that sent the callbacks
                callbackId,
                args
            }
        };

        // Wait for the response from the server
        var response = await SendAndReceiveAsync(invokeId, message);
        return response;
    }

    private async Task<JsonNode?> SendAndReceiveAsync(string id, object message)
    {
        var tcs = new TaskCompletionSource<JsonNode?>();
        _pendingReplies[id] = tcs;

        var json = JsonSerializer.Serialize(message);
        //await File.AppendAllTextAsync("D:\\Git\\writes.txt", json);
        //await File.AppendAllTextAsync("D:\\Git\\writes.txt", Environment.NewLine);
        //await File.AppendAllTextAsync("D:\\Git\\writes.txt", Environment.NewLine);
        var bytes = Encoding.UTF8.GetBytes(json + "\uFFFF");

        await _stream!.WriteAsync(bytes);
        await _stream!.FlushAsync();

        return await tcs.Task;
    }

    private async Task ReaderLoop()
    {
        var buffer = new byte[64 * 1024];
        var offset = 0;

        while (!_disposed && _stream != null)
        {
            try
            {
                var bytesRead = await _stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset));
                if (bytesRead == 0) break;

                var text = Encoding.UTF8.GetString(buffer, 0, offset + bytesRead);
                var messages = text.Split('\uFFFF');

                for (int i = 0; i < messages.Length - 1; i++)
                {
                    if (!string.IsNullOrEmpty(messages[i]))
                    {
                        await ProcessMessageAsync(messages[i]);
                    }
                }

                // Keep the incomplete message
                var lastMessage = messages[^1];
                if (!string.IsNullOrEmpty(lastMessage))
                {
                    var lastBytes = Encoding.UTF8.GetBytes(lastMessage);
                    Array.Copy(lastBytes, buffer, lastBytes.Length);
                    offset = lastBytes.Length;
                }
                else
                {
                    offset = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReaderLoop error: {ex}");
                break;
            }
        }
    }

    private async Task ProcessMessageAsync(string json)
    {
        //await File.AppendAllTextAsync("D:\\Git\\read.txt", json);
        //await File.AppendAllTextAsync("D:\\Git\\read.txt", Environment.NewLine);
        //await File.AppendAllTextAsync("D:\\Git\\read.txt", Environment.NewLine);

        try
        {
            var message = JsonNode.Parse(json);
            if (message == null) return;

            var id = message["id"]?.GetValue<string>();
            var callback = message["callback"];
            var data = message["data"];
            var error = message["error"];

            if (error != null)
                ;

            // Is this a callback invocation from the server?
            if (callback != null)
            {
                var callbackId = callback["id"]?.GetValue<string>();  // Original Install request ID
                var callbackType = callback["type"]?.GetValue<string>();
                var name = data?["name"]?.GetValue<string>();
                var args = data?["args"]?.AsArray();

                // Store the MESSAGE ID (not callback.id) - this is what the server uses to look up callbacks
                _currentRequestId = id;

                if (!string.IsNullOrEmpty(name) && _callbacks.TryGetValue(name, out var callbackFunc))
                {
                    try
                    {
                        var result = await callbackFunc(args);

                        // Send reply wrapped in id+payload structure like all messages
                        // IMPORTANT: Use the message's "id" field, not "callback.id"
                        // When result is null, send empty object instead to avoid JValue(null) issues
                        var replyMessage = new
                        {
                            id = Guid.NewGuid().ToString("N"),
                            payload = new
                            {
                                command = "Reply",
                                request = new { id = id },  // Use the callback message ID, not callbackId
                                data = result ?? new { },  // Empty object instead of null
                                error = (object?) null
                            }
                        };

                        var replyJson = JsonSerializer.Serialize(replyMessage);
                        //await File.AppendAllTextAsync("D:\\Git\\replies.txt", replyJson);
                        //await File.AppendAllTextAsync("D:\\Git\\replies.txt", Environment.NewLine);
                        //await File.AppendAllTextAsync("D:\\Git\\replies.txt", Environment.NewLine);
                        var replyBytes = Encoding.UTF8.GetBytes(replyJson + "\uFFFF");
                        await _stream!.WriteAsync(replyBytes);
                        await _stream!.FlushAsync();
                    }
                    catch (Exception ex)
                    {
                        // Send error reply wrapped in id+payload structure like all messages
                        // IMPORTANT: Use the message's "id" field, not "callback.id"
                        var replyMessage = new
                        {
                            id = Guid.NewGuid().ToString("N"),
                            payload = new
                            {
                                command = "Reply",
                                request = new { id = id },  // Use the callback message ID, not callbackId
                                data = (object?) null,
                                error = new { message = ex.Message }
                            }
                        };

                        var replyJson = JsonSerializer.Serialize(replyMessage);
                        var replyBytes = Encoding.UTF8.GetBytes(replyJson + "\uFFFF");
                        await _stream!.WriteAsync(replyBytes);
                        await _stream!.FlushAsync();
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown callback: {callbackType} {name}");
                }
            }
            // Is this a response to our request?
            else if (!string.IsNullOrEmpty(id) && _pendingReplies.TryRemove(id, out var tcs))
            {
                if (error != null)
                {
                    var errorMessage = error["message"]?.GetValue<string>() ?? "Unknown error";
                    tcs.SetException(new Exception(errorMessage));
                }
                else
                {
                    tcs.SetResult(data);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ProcessMessageAsync error: {ex}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Send quit command
        try
        {
            if (_stream != null)
            {
                var quitMessage = new { id = Guid.NewGuid().ToString("N"), payload = new { command = "Quit" } };
                var json = JsonSerializer.Serialize(quitMessage);
                var bytes = Encoding.UTF8.GetBytes(json + "\uFFFF");
                await _stream.WriteAsync(bytes);
                await _stream.FlushAsync();
            }
        }
        catch { }

        _stream?.Dispose();
        _client?.Dispose();
        _listener?.Stop();

        if (_readerTask != null)
        {
            try
            {
                await _readerTask.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Timeout is fine, we're disposing anyway
            }
        }

        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Kill(true);
                await _process.WaitForExitAsync();
            }
            catch
            {
                // Process may have already exited
            }
        }

        _process?.Dispose();
    }
}

public class TestSupportedResult
{
    public bool Supported { get; set; }
    public string[] RequiredFiles { get; set; } = Array.Empty<string>();
}

public record InstallResult
{
    public string Message { get; set; }
    public required List<InstallInstruction> Instructions { get; set; }
}

// Stub types for UI delegates (matching the interface)
public class HeaderImage { }
public class InstallerStep { }