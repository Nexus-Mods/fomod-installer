namespace FomodInstaller.Interface.ui
{
    public interface IUIDelegates
    {
        void EndDialog();
        void ReportError(string title, string message, string details);
        void StartDialog(string? moduleName, HeaderImage image, Action<int, int, int[]> select, Action<bool, int> cont, Action cancel);
        void UpdateState(InstallerStep[] installSteps, int currentStep);
    }
}