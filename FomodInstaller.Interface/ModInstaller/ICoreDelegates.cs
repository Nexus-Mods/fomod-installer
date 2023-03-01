using FomodInstaller.Interface.ui;

namespace FomodInstaller.Interface
{
    public interface ICoreDelegates
    {
        IContextDelegates context { get; }
        IIniDelegates ini { get; }
        IPluginDelegates plugin { get; }
        IUIDelegates ui { get; }
    }
}