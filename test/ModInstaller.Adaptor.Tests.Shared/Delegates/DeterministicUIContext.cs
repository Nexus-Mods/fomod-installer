using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;

using TestData;

namespace ModInstaller.Adaptor.Tests.Shared.Delegates;

public class DeterministicUIContext : UIDelegates
{
    private readonly List<SelectedOption>? _dialogChoices;
    private readonly bool _unattended;

    private Action<int, int, int[]>? _select;
    private Action<bool, int>? _cont;
    private Action? _cancel;

    private InstallerStep[]? _installerSteps;
    private int? _currentStep;

    public DeterministicUIContext(IEnumerable<SelectedOption>? dialogChoices)
    {
        _dialogChoices = dialogChoices?.ToList();
        _unattended = dialogChoices == null;
    }


    public override void StartDialog(string moduleName, HeaderImage image, Action<int, int, int[]> select, Action<bool, int> cont, Action cancel)
    {
        _select = select;
        _cont = cont;
        _cancel = cancel;
    }

    public override void EndDialog()
    {
        _select = null!;
        _cont = null!;
        _cancel = null!;

        _installerSteps = null!;
        _currentStep = 0;
    }

    private bool dialogInProgress = false;

    public override void UpdateState(InstallerStep[] installSteps, int currentStep)
    {
        if (dialogInProgress)
            return;

        _installerSteps = installSteps;
        _currentStep = currentStep;

        if (_cont == null) throw new NotSupportedException();

        if (_unattended)
        {
            _cont(true, currentStep);
        }
        else if (_dialogChoices is { Count: > 0 })
        {
            if (_select == null) throw new NotSupportedException();

            var option = _dialogChoices.First(x => x.StepId == currentStep);

            dialogInProgress = true;
            _select(option.StepId, option.GroupId, option.PluginIds);

            _cont(true, currentStep);
            dialogInProgress = false;
        }
    }

    public override void ReportError(string title, string message, string details)
    {
        throw new NotImplementedException();
    }
}