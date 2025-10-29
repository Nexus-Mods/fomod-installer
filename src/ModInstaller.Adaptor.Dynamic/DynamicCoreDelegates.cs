using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;

namespace FomodInstaller.ModInstaller;

public class DynamicCoreDelegates : CoreDelegates
{
    private DynamicPluginDelegates mPluginDelegates;
    private DynamicContextDelegates _mContextDelegates;
    private DynamicIniDelegates _mDynamicIniDelegates;
    private DynamicUIDelegates mUIDelegates;

    public override PluginDelegates plugin => mPluginDelegates;
    public override IniDelegates ini => _mDynamicIniDelegates;
    public override ContextDelegates context => _mContextDelegates;
    public override UIDelegates ui => mUIDelegates;
    
    public DynamicCoreDelegates(dynamic source)
    {
        mPluginDelegates = new DynamicPluginDelegates(source.plugin);
        _mDynamicIniDelegates = new DynamicIniDelegates(source.ini);
        _mContextDelegates = new DynamicContextDelegates(source.context);
        mUIDelegates = new DynamicUIDelegates(source.ui);
    }
}