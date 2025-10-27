using System.Text.Json.Nodes;
using TestData;

namespace ModInstallerIPC.Tests.Delegates;

/// <summary>
/// Handles UI callbacks for IPC testing with deterministic option selection
/// </summary>
internal class IPCUIHandler
{
    private readonly IEnumerable<SelectedOption>? _dialogChoices;
    private readonly bool _unattended;

    // Callback IDs from the server
    private string? _selectCallbackId;
    private string? _contCallbackId;
    private string? _cancelCallbackId;

    // Function to invoke callbacks on the server
    private readonly Func<string, JsonArray, Task<object?>> _invokeCallback;

    public IPCUIHandler(IEnumerable<SelectedOption>? dialogChoices, Func<string, JsonArray, Task<object?>> invokeCallback)
    {
        _dialogChoices = dialogChoices;
        _unattended = dialogChoices == null;
        _invokeCallback = invokeCallback;
    }

    public Task StartDialog(JsonArray args)
    {
        // args is actually [actualArgs] where actualArgs = [moduleName, image, selectCallback, contCallback, cancelCallback]
        // The callbacks are serialized as { "__callback": "id" }

        if (args.Count > 0 && args[0] is JsonObject actualArgs && actualArgs.Count >= 5)
        {
            var selectCallback = actualArgs[2]?.AsObject();
            var contCallback = actualArgs[3]?.AsObject();
            var cancelCallback = actualArgs[4]?.AsObject();

            _selectCallbackId = selectCallback?["__callback"]?.GetValue<string>();
            _contCallbackId = contCallback?["__callback"]?.GetValue<string>();
            _cancelCallbackId = cancelCallback?["__callback"]?.GetValue<string>();
        }

        return Task.CompletedTask;
    }

    public Task EndDialog()
    {
        _selectCallbackId = null;
        _contCallbackId = null;
        _cancelCallbackId = null;
        return Task.CompletedTask;
    }

    public async Task UpdateState(JsonArray args)
    {
        // args is actually [actualArgs] where actualArgs = [installSteps, currentStep]
        if (args.Count == 0 || args[0] is not JsonObject actualArgs || actualArgs.Count < 2) return;

        var currentStep = actualArgs[1]!.GetValue<int>();

        if (_contCallbackId == null)
        {
            throw new InvalidOperationException("UpdateState called before StartDialog");
        }

        if (_unattended)
        {
            // Auto-continue in unattended mode
            await InvokeContCallback(true, currentStep);
        }
        else if (_dialogChoices != null)
        {
            // Use predetermined choices
            var option = _dialogChoices.FirstOrDefault(x => x.StepId == currentStep);

            if (option != null && _selectCallbackId != null)
            {
                // Call select callback
                await _invokeCallback(_selectCallbackId, new JsonArray(option.StepId, option.GroupId, JsonValue.Create(option.PluginIds)));

                // Wait a bit to ensure select completes before continue
                await Task.Delay(1000);
            }

            // Call continue callback
            await InvokeContCallback(true, currentStep);
        }
        else
        {
            // No choices provided - just continue
            await InvokeContCallback(true, currentStep);
        }
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
        if (_contCallbackId != null)
        {
            await _invokeCallback(_contCallbackId, new JsonArray(shouldContinue, stepId));
        }
    }
}
