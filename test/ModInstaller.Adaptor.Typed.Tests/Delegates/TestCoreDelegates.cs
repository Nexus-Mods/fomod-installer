using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;

namespace ModInstaller.Adaptor.Typed.Tests.Delegates;

internal class TestCoreDelegates : CoreDelegates
{
    public override PluginDelegates plugin { get; }
    public override IniDelegates ini { get; }
    public override ContextDelegates context { get; }
    public override UIDelegates ui { get; }

    public TestCoreDelegates(PluginDelegates plugin, IniDelegates ini, ContextDelegates context, UIDelegates ui)
    {
        this.plugin = plugin;
        this.ini = ini;
        this.context = context;
        this.ui = ui;
    }
}