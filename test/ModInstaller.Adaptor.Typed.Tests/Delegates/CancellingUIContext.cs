using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;

namespace ModInstaller.Adaptor.Typed.Tests.Delegates;

internal class CancellingUIContext : UIDelegates
{
    private Action? _cancel;
    private bool _hasReceivedFirstUpdate;

    public bool CancelWasCalled { get; private set; }

    public override void StartDialog(string moduleName, HeaderImage image, Action<int, int, int[]> select, Action<bool, int> cont, Action cancel)
    {
        _cancel = cancel;
        _hasReceivedFirstUpdate = false;
    }

    public override void EndDialog()
    {
        _cancel = null;
        _hasReceivedFirstUpdate = false;
    }

    public override void UpdateState(InstallerStep[] installSteps, int currentStep)
    {
        if (!_hasReceivedFirstUpdate && _cancel != null)
        {
            _hasReceivedFirstUpdate = true;
            // Invoke the cancel callback after the first UpdateState call
            _cancel();
            CancelWasCalled = true;
        }
    }

    public override void ReportError(string title, string message, string details)
    {
        throw new NotImplementedException();
    }
}
