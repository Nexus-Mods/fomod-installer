using System.Text.Json.Nodes;
using TestData;

namespace ModInstaller.IPC.Tests.Delegates;

/// <summary>
/// Handles UI callbacks for IPC testing with deterministic option selection
/// Follows the pattern from DeterministicUIContext
/// </summary>
internal class IPCUIHandler
{
    private readonly List<SelectedOption>? _dialogChoices;
    private readonly bool _unattended;

    // Callback IDs from the server
    private string? _selectCallbackId;
    private string? _contCallbackId;
    private string? _cancelCallbackId;

    // Request ID of the message that sent the callbacks (needed for Invoke)
    private string? _callbackRequestId;

    // Function to invoke callbacks on the server
    private readonly Func<string, string, JsonArray, Task<object?>> _invokeCallback;

    // Dialog state
    private bool _dialogInProgress = false;

    public IPCUIHandler(IEnumerable<SelectedOption>? dialogChoices, Func<string, string, JsonArray, Task<object?>> invokeCallback)
    {
        _dialogChoices = dialogChoices?.ToList();
        _unattended = dialogChoices == null;
        _invokeCallback = invokeCallback;
    }

    public Task StartDialog(string messageId, JsonArray args)
    {
        // The server sends StartParameters as a flat object: { moduleName, image, select, cont, cancel }
        // The callbacks are serialized as { "__callback": "id" }

        if (args.Count > 0 && args[0] is JsonObject startParams)
        {
            var selectCallback = startParams["select"]?.AsObject();
            var contCallback = startParams["cont"]?.AsObject();
            var cancelCallback = startParams["cancel"]?.AsObject();

            _selectCallbackId = selectCallback?["__callback"]?.GetValue<string>();
            _contCallbackId = contCallback?["__callback"]?.GetValue<string>();
            _cancelCallbackId = cancelCallback?["__callback"]?.GetValue<string>();

            // Store the message ID that sent these callbacks
            _callbackRequestId = messageId;
        }

        return Task.CompletedTask;
    }

    public Task EndDialog()
    {
        _selectCallbackId = null;
        _contCallbackId = null;
        _cancelCallbackId = null;
        _dialogInProgress = false;
        return Task.CompletedTask;
    }

    public Task UpdateState(string messageId, JsonArray args)
    {
        // The server sends UpdateParameters as a flat object: { installSteps, currentStep }
        if (args.Count == 0 || args[0] is not JsonObject updateParams) return Task.CompletedTask;

        // Prevent re-entry while dialog is in progress
        if (_dialogInProgress)
            return Task.CompletedTask;

        var currentStep = updateParams["currentStep"]!.GetValue<int>();

        if (_contCallbackId == null || _callbackRequestId == null)
        {
            throw new InvalidOperationException("UpdateState called before StartDialog");
        }

        // Return immediately (like DeterministicUIContext) to send Reply first
        // Then invoke callbacks asynchronously
        if (_unattended)
        {
            // Auto-continue in unattended mode
            Task.Delay(200).ContinueWith(_ =>
            {
                _ = InvokeContCallback(true, currentStep);
            });
        }
        else if (_dialogChoices is { Count: > 0 })
        {
            if (_selectCallbackId == null)
            {
                throw new InvalidOperationException("Select callback not available");
            }

            var option = _dialogChoices.First(x => x.StepId == currentStep);

            _dialogInProgress = true;

            // Call select callback first - send as single object parameter: { stepId, groupId, plugins }
            var selectArgs = new JsonObject
            {
                ["stepId"] = option.StepId,
                ["groupId"] = option.GroupId,
                ["plugins"] = JsonValue.Create(option.PluginIds)
            };
            _ = _invokeCallback(_callbackRequestId, _selectCallbackId, new JsonArray(selectArgs))
                .ContinueWith(_ =>
                {
                    // Wait for select to complete, then call continue
                    return Task.Delay(200).ContinueWith(__ =>
                    {
                        _ = InvokeContCallback(true, currentStep);
                        _dialogInProgress = false;
                    });
                });
        }

        return Task.CompletedTask;
    }

    public Task ReportError(JsonArray args)
    {
        // args is actually [actualArgs] where actualArgs = [title, message, details]
        if (args.Count > 0 && args[0] is JsonArray actualArgs && actualArgs.Count >= 3)
        {
            var title = actualArgs[0]?.GetValue<string>() ?? "Error";
            var message = actualArgs[1]?.GetValue<string>() ?? "Unknown error";
            var details = actualArgs[2]?.GetValue<string>() ?? "";

            throw new Exception($"{title}: {message}\n{details}");
        }

        throw new Exception("Unknown error");
    }

    private async Task InvokeContCallback(bool shouldContinue, int stepId)
    {
        if (_contCallbackId != null && _callbackRequestId != null)
        {
            // Send the arguments that TypeScript expects: { direction: "forward"|"backward", currentStepId: number }
            var contArgs = new JsonObject
            {
                ["direction"] = shouldContinue ? "forward" : "backward",
                ["currentStepId"] = stepId
            };
            await _invokeCallback(_callbackRequestId, _contCallbackId, new JsonArray(contArgs));
        }
    }
}
