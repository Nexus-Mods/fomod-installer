using FomodInstaller.Interface.ui;

namespace FomodInstaller.Interface
{
    public abstract class CoreDelegates
    {
        public abstract PluginDelegates plugin { get; }
        public abstract IniDelegates ini { get; }
        public abstract ContextDelegates context { get; }
        public abstract UIDelegates ui { get; }
    }
}
