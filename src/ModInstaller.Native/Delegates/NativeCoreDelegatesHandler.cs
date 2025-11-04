using BUTR.NativeAOT.Shared;

using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;

using System;
using System.Runtime.InteropServices;

namespace ModInstaller.Native.Adapters;

internal class NativeCoreDelegatesHandler : CoreDelegates, IDisposable
{
    public static unsafe NativeCoreDelegatesHandler? FromPointer(void* ptr) => GCHandle.FromIntPtr(new IntPtr(ptr)).Target as NativeCoreDelegatesHandler;

    private CallbackPluginDelegates _pluginDelegates;
    private CallbackContextDelegates _contextDelegates;
    private CallbackIniDelegates _iniDelegates;
    private CallbackUIDelegates _uiDelegates;

    public override PluginDelegates plugin => _pluginDelegates;
    public override IniDelegates ini => _iniDelegates;
    public override ContextDelegates context => _contextDelegates;
    public override UIDelegates ui => _uiDelegates;

    public unsafe param_ptr* OwnerPtr { get; }
    public unsafe VoidPtr* HandlePtr { get; }

    public unsafe NativeCoreDelegatesHandler(param_ptr* pOwner,
        CallbackPluginDelegates pluginDelegates,
        CallbackContextDelegates contextDelegates,
        CallbackIniDelegates iniDelegates,
        CallbackUIDelegates uiDelegates)
    {
        _pluginDelegates = pluginDelegates;
        _contextDelegates = contextDelegates;
        _iniDelegates = iniDelegates;
        _uiDelegates = uiDelegates;

        OwnerPtr = pOwner;
        HandlePtr = (VoidPtr*) GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Normal)).ToPointer();
    }
    private unsafe void ReleaseUnmanagedResources()
    {
        var handle = GCHandle.FromIntPtr(new IntPtr(HandlePtr));
        if (handle.IsAllocated) handle.Free();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~NativeCoreDelegatesHandler()
    {
        ReleaseUnmanagedResources();
    }
}