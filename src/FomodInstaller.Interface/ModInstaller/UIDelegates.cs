using System;

namespace FomodInstaller.Interface.ui
{
    public abstract class UIDelegates
    {
        /// <summary>
        /// 
        /// </summary>
        public abstract void StartDialog(string moduleName, HeaderImage image, Action<int, int, int[]> select, Action<bool, int> cont, Action cancel);
        
        /// <summary>
        /// 
        /// </summary>
        public abstract void EndDialog();
        
        /// <summary>
        /// 
        /// </summary>
        public abstract void UpdateState(InstallerStep[] installSteps, int currentStep);
    
        /// <summary>
        /// C# only
        /// </summary>
        public abstract void ReportError(string title, string message, string details);
    }
}