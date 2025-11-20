using BUTR.NativeAOT.Shared;

using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ModInstaller.Native.Adapters;

internal class CallbackUIDelegates : UIDelegates
{
    private record StartDialogCallbacksData
    {
        public Action<int, int, int[]> Select { get; init; }
        public Action<bool, int> Continue { get; init; }
        public Action Cancel { get; init; }
    }

    private readonly unsafe param_ptr* _pOwner;
    private readonly N_UI_StartDialog _startDialog;
    private readonly N_UI_EndDialog _endDialog;
    private readonly N_UI_UpdateState _updateState;
    private readonly N_UI_ReportError _reportError;

    public unsafe CallbackUIDelegates(param_ptr* pOwner,
        N_UI_StartDialog startDialog,
        N_UI_EndDialog endDialog,
        N_UI_UpdateState updateState,
        N_UI_ReportError reportError)
    {
        _pOwner = pOwner;
        _startDialog = startDialog;
        _endDialog = endDialog;
        _updateState = updateState;
        _reportError = reportError;
    }

    public override unsafe void StartDialog(string moduleName, HeaderImage image, Action<int, int, int[]> select, Action<bool, int> cont, Action cancel)
    {
#if DEBUG
        using var logger = LogMethod(moduleName.ToFormattable());
#else
        using var logger = LogMethod();
#endif

        if (_currentDialogHandle is not null)
            throw new Exception("Should be null");

        _currentDialogHandle = GCHandle.Alloc(new StartDialogCallbacksData
        {
            Select = select,
            Continue = cont,
            Cancel = cancel,
        }, GCHandleType.Normal);

        fixed (char* pModuleName = moduleName)
        fixed (char* pImage = BUTR.NativeAOT.Shared.Utils.SerializeJson(image, Bindings.CustomSourceGenerationContext.HeaderImage))
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_startDialog(_pOwner, (param_string*) pModuleName, (param_json*) pImage, (param_ptr*) GCHandle.ToIntPtr(_currentDialogHandle.Value), &StartDialogSelectCallback, &StartDialogContinueCallback, &StartDialogCancelCallback), true);
                logger.LogResult(result);
                result.ValueAsVoid();
            }
            catch (Exception e)
            {
                logger.LogException(e);
                _currentDialogHandle?.Free();
                _currentDialogHandle = null;
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void StartDialogSelectCallback(param_ptr* pOwner, param_int stepId, param_int groupId, param_json* optionIdsJson, return_value_void* pResult)
    {
#if DEBUG
        using var logger = LogCallbackMethod(&stepId, &groupId, optionIdsJson, pResult);
#else
        using var logger = LogCallbackMethod(pResult);
#endif

        try
        {

            if (pOwner == null)
            {
                logger.LogException(new ArgumentNullException(nameof(pOwner)));
                return;
            }

            if (GCHandle.FromIntPtr((IntPtr) pOwner) is not {Target: StartDialogCallbacksData callbacksData})
            {
                logger.LogException(new InvalidOperationException("Invalid GCHandle."));
                return;
            }

            using var result = SafeStructMallocHandle.Create(pResult, true);
            logger.LogResult(result);
            result.ValueAsVoid();

            var optionIds = BUTR.NativeAOT.Shared.Utils.DeserializeJson<int[]>(optionIdsJson, Bindings.CustomSourceGenerationContext.Int32Array);
            callbacksData.Select(stepId, groupId, optionIds);
        }
        catch (Exception e)
        {
            logger.LogException(e);
            throw;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void StartDialogContinueCallback(param_ptr* pOwner, param_bool forward, param_int currentStepId, return_value_void* pResult)
    {
#if DEBUG
        using var logger = LogCallbackMethod(&forward, &currentStepId, pResult);
#else
        using var logger = LogCallbackMethod(pResult);
#endif

        try
        {

            if (pOwner == null)
            {
                logger.LogException(new ArgumentNullException(nameof(pOwner)));
                return;
            }

            if (GCHandle.FromIntPtr((IntPtr) pOwner) is not {Target: StartDialogCallbacksData callbacksData})
            {
                logger.LogException(new InvalidOperationException("Invalid GCHandle."));
                return;
            }

            using var result = SafeStructMallocHandle.Create(pResult, true);
            logger.LogResult(result);
            result.ValueAsVoid();

            callbacksData.Continue(forward, currentStepId);
        }
        catch (Exception e)
        {
            logger.LogException(e);
            throw;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void StartDialogCancelCallback(param_ptr* pOwner, return_value_void* pResult)
    {
#if DEBUG
        using var logger = LogCallbackMethod(pResult);
#else
        using var logger = LogCallbackMethod(pResult);
#endif

        try
        {
            if (pOwner == null)
            {
                logger.LogException(new ArgumentNullException(nameof(pOwner)));
                return;
            }

            if (GCHandle.FromIntPtr((IntPtr) pOwner) is not {Target: StartDialogCallbacksData callbacksData})
            {
                logger.LogException(new InvalidOperationException("Invalid GCHandle."));
                return;
            }

            using var result = SafeStructMallocHandle.Create(pResult, true);
            logger.LogResult(result);
            result.ValueAsVoid();

            callbacksData.Cancel();
        }
        catch (Exception e)
        {
            logger.LogException(e);
            throw;
        }
    }

    private GCHandle? _currentDialogHandle;

    public override unsafe void EndDialog()
    {
#if DEBUG
        using var logger = LogMethod();
#else
        using var logger = LogMethod();
#endif

        try
        {
            using var result = SafeStructMallocHandle.Create(_endDialog(_pOwner), true);
            logger.LogResult(result);
            result.ValueAsVoid();
        }
        catch (Exception e)
        {
            logger.LogException(e);
        }
    }

    public override unsafe void UpdateState(InstallerStep[] installSteps, int currentStepId)
    {
#if DEBUG
        using var logger = LogMethod(currentStepId);
#else
        using var logger = LogMethod();
#endif

        fixed (char* pInstallSteps = BUTR.NativeAOT.Shared.Utils.SerializeJson(installSteps, Bindings.CustomSourceGenerationContext.InstallerStepArray))
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_updateState(_pOwner, (param_json*) pInstallSteps, (param_int) currentStepId), true);
                logger.LogResult(result);
                result.ValueAsVoid();
            }
            catch (Exception e)
            {
                logger.LogException(e);
            }
        }
    }

    public override void ReportError(string title, string message, string details)
    {
#if DEBUG
        using var logger = LogMethod(title.ToFormattable(), message.ToFormattable(), details.ToFormattable());
#else
        using var logger = LogMethod();
#endif

        try
        {
            var tcs = new TaskCompletionSource();
            ReportErrorNative(title, message, details, tcs);
        }
        catch (Exception e)
        {
            logger.LogException(e);
            throw;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe void ReportErrorCallback(param_ptr* pOwner, return_value_void* pResult)
    {
#if DEBUG
        using var logger = LogCallbackMethod(pResult);
#else
        using var logger = LogCallbackMethod(pResult);
#endif

        try
        {

            if (pOwner == null)
            {
                logger.LogException(new ArgumentNullException(nameof(pOwner)));
                return;
            }

            if (GCHandle.FromIntPtr((IntPtr) pOwner) is not {Target: TaskCompletionSource tcs} handle)
            {
                logger.LogException(new InvalidOperationException("Invalid GCHandle."));
                return;
            }

            using var result = SafeStructMallocHandle.Create(pResult, true);
            logger.LogResult(result);
            result.SetAsVoid(tcs);
            handle.Free();
        }
        catch (Exception e)
        {
            logger.LogException(e);
            throw;
        }
    }

    private unsafe void ReportErrorNative(ReadOnlySpan<char> title, ReadOnlySpan<char> message, ReadOnlySpan<char> details, TaskCompletionSource tcs)
    {
#if DEBUG
        using var logger = LogMethod(title.ToString().ToFormattable(), message.ToString().ToFormattable(), details.ToString().ToFormattable());
#else
        using var logger = LogMethod();
#endif

        var handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

        fixed (char* pTitle = title)
        fixed (char* pMessage = message)
        fixed (char* pDetails = details)
        {
            try
            {
                using var result = SafeStructMallocHandle.Create(_reportError(_pOwner, (param_string*) pTitle, (param_string*) pMessage, (param_string*) pDetails, (param_ptr*) GCHandle.ToIntPtr(handle), &ReportErrorCallback), true);
                logger.LogResult(result);
                result.ValueAsVoid();
            }
            catch (Exception e)
            {
                logger.LogException(e);
                tcs.TrySetException(e);
                handle.Free();
            }
        }
    }
}