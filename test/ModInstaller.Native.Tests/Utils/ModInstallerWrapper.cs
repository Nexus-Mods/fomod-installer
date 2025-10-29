using System.Diagnostics;
using System.Runtime.InteropServices;
using BUTR.NativeAOT.Shared;
using FluentAssertions;
using FomodInstaller.Interface.ui;
using Microsoft.Win32.SafeHandles;
using ModInstaller.Native.Tests.Utils;
using TestData;

namespace ModInstaller.Native.Tests;

public sealed class ModInstallerWrapper
{

    private readonly InstallData _data;
    private readonly List<SelectedOption>? _dialogChoices;
    private readonly bool _unattended;

    private unsafe param_ptr* _callback_handler;
    private unsafe delegate* unmanaged[Cdecl] <param_ptr*, param_int, param_int, param_json*, return_value_void*, void> _select;
    private unsafe delegate* unmanaged[Cdecl] <param_ptr*, param_bool, param_int, return_value_void*, void> _cont;
    private unsafe delegate* unmanaged[Cdecl] <param_ptr*, return_value_void*, void> _cancel;

    private InstallerStep[]? _installerSteps;
    private int? _currentStep;

    public ModInstallerWrapper(InstallData data)
    {
        _data = data;
        _dialogChoices = _data.DialogChoices?.ToList();
        _unattended = _data.DialogChoices == null;
    }

    private static unsafe byte* Copy(in ReadOnlySpan<byte> data, bool isOwner)
    {
        var dst = (byte*) Allocator.Alloc((uint) data.Length);
        data.CopyTo(new Span<byte>(dst, data.Length));
        return dst;
    }

    public static unsafe return_value_data* ReadFileContent(param_ptr* handler, param_string* pFilePath, param_int offset, param_int length)
    {
        //Utils2.LibraryAliveCount().Should().Be(0);
        
        Stream? stream = null;
        try
        {
            var filePath = new string(param_string.ToSpan(pFilePath));
            
            var modInstallerWrapper = (ModInstallerWrapper) GCHandle.FromIntPtr((IntPtr) handler).Target!;
            var entry = modInstallerWrapper._data.ModArchive.Entries.FirstOrDefault(x => x.GetNormalizedName() == filePath);
            if (entry == null)
                return return_value_data.AsValue(null, 0, false);
            
            stream = entry.OpenEntryStream();

            if (length == -1) length = (int) entry.Size;

            if (length == 0)
            {
                return return_value_data.AsValue(Copy([], false), 0, false);
            }

            if (offset > 0)
            {
                var discard = new byte[offset];
                stream.ReadExactly(discard);
            }

            var bufferPtr = (byte*) Allocator.Alloc((uint) (int) length);
            var buffer = new Span<byte>(bufferPtr, length);
            stream.ReadExactly(buffer);
            return return_value_data.AsValue(bufferPtr, length, false);
        }
        catch
        {
            return return_value_data.AsValue(null, 0, false);
        }
        finally
        {
            stream?.Dispose();
            //Utils2.LibraryAliveCount().Should().Be(2);
        }
    }

    public static unsafe return_value_data* ReadFileContent2(param_ptr* handler, param_string* pFilePath, param_int offset, param_int length)
    {
        SafeFileHandle? fileHandle = null;
        try
        {
            var filePath = new string(param_string.ToSpan(pFilePath));

            fileHandle = File.OpenHandle(filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                FileOptions.RandomAccess);

            if (length == -1)
                length = (int) RandomAccess.GetLength(fileHandle);

            if (length == 0)
            {
                return return_value_data.AsValue(Copy([], false), 0, false);
            }

            var bufferPtr = (byte*) Allocator.Alloc((uint) (int) length);
            var buffer = new Span<byte>(bufferPtr, length);
            RandomAccess.Read(fileHandle, buffer, offset);
            return return_value_data.AsValue(bufferPtr, length, false);
        }
        catch (Exception e)
        {
            return return_value_data.AsException(e, false);
        }
        finally
        {
            fileHandle?.Dispose();
        }
    }

    public static unsafe return_value_json* ReadDirectoryFileList(param_ptr* handler, param_string* pDirectoryPath, param_string* pPattern, param_int searchOption)
    {
        var directoryPath = new string(param_string.ToSpan(pDirectoryPath));
        var pattern = new string(param_string.ToSpan(pPattern));
        var data = Directory.Exists(directoryPath) ? Directory.GetFiles(directoryPath, pattern, (SearchOption) (int) searchOption) : null;
        return data is null ? return_value_json.AsValue(null, false) : return_value_json.AsValue(data, Utils2.CustomSourceGenerationContext.StringArray, false);
    }

    public static unsafe return_value_json* ReadDirectoryList(param_ptr* handler, param_string* pDirectoryPath)
    {
        var directoryPath = new string(param_string.ToSpan(pDirectoryPath));
        var data = Directory.Exists(directoryPath) ? Directory.GetDirectories(directoryPath) : null;
        return data is null ? return_value_json.AsValue(null, false) : return_value_json.AsValue(data, Utils2.CustomSourceGenerationContext.StringArray, false);
    }
    
    public static unsafe return_value_json* PluginsGetAll(param_ptr* handler, param_bool includeDisabled)
    {
        Utils2.LibraryAliveCount().Should().Be(0);
        try
        {
            var modInstallerWrapper = (ModInstallerWrapper) GCHandle.FromIntPtr((IntPtr) handler).Target!;
            return return_value_json.AsValue(modInstallerWrapper._data.InstalledPlugins, Utils2.CustomSourceGenerationContext.ListString, false);
        }
        catch (Exception e)
        {
            return return_value_json.AsException(e, false);
        }
        finally
        {
            Utils2.LibraryAliveCount().Should().Be(1);
        }
    }

    public static unsafe return_value_void* ContextGetAppVersion(param_ptr* handler, param_ptr* p_context, delegate* unmanaged[Cdecl] <param_ptr*, return_value_string*, void> p_callback)
    {
        Utils2.LibraryAliveCount().Should().Be(0);
        try
        {
            var modInstallerWrapper = (ModInstallerWrapper) GCHandle.FromIntPtr((IntPtr) handler).Target!;
            var result = return_value_string.AsValue(BUTR.NativeAOT.Shared.Utils.Copy(modInstallerWrapper._data.AppVersion, false), false);
            p_callback(p_context, result);
            return return_value_void.AsValue(false);
        }
        catch (Exception e)
        {
            return return_value_void.AsException(e, false);
        }
        finally
        {
            Utils2.LibraryAliveCount().Should().Be(1);
        }
    }

    public static unsafe return_value_void* ContextGetCurrentGameVersion(param_ptr* handler, param_ptr* p_context, delegate* unmanaged[Cdecl] <param_ptr*, return_value_string*, void> p_callback)
    {
        //Utils2.LibraryAliveCount().Should().Be(0);
        try
        {
            var modInstallerWrapper = (ModInstallerWrapper) GCHandle.FromIntPtr((IntPtr) handler).Target!;
            var result = return_value_string.AsValue(BUTR.NativeAOT.Shared.Utils.Copy(modInstallerWrapper._data.GameVersion, false), false);
            p_callback(p_context, result);
            return return_value_void.AsValue(false);
        }
        catch (Exception e)
        {
            return return_value_void.AsException(e, false);
        }
        finally
        {
            //Utils2.LibraryAliveCount().Should().Be(1);
        }
    }

    public static unsafe return_value_void* ContextGetExtenderVersion(param_ptr* handler, param_string* p_extender_name, param_ptr* p_context, delegate* unmanaged[Cdecl] <param_ptr*, return_value_string*, void> p_callback)
    {
        Utils2.LibraryAliveCount().Should().Be(0);
        try
        {
            var modInstallerWrapper = (ModInstallerWrapper) GCHandle.FromIntPtr((IntPtr) handler).Target!;
            var result = return_value_string.AsValue(BUTR.NativeAOT.Shared.Utils.Copy(modInstallerWrapper._data.ExtenderVersion, false), false);
            p_callback(p_context, result);
            return return_value_void.AsValue(false);
        }
        catch (Exception e)
        {
            return return_value_void.AsException(e, false);
        }
        finally
        {
            Utils2.LibraryAliveCount().Should().Be(2);
        }
    }

    public static unsafe return_value_void* UiStartDialog(
        param_ptr* handler,
        param_string* p_module_name,
        param_json* p_image,
        param_ptr* p_context,
        delegate* unmanaged[Cdecl]<param_ptr*, param_int, param_int, param_json*, return_value_void*, void> p_select_callback,
        delegate* unmanaged[Cdecl]<param_ptr*, param_bool, param_int, return_value_void*, void> p_cont_callback,
        delegate* unmanaged[Cdecl]<param_ptr*, return_value_void*, void> p_cancel_callback)
    {
        Utils2.LibraryAliveCount().Should().Be(0);
        try
        {
            var modInstallerWrapper = (ModInstallerWrapper) GCHandle.FromIntPtr((IntPtr) handler).Target!;

            modInstallerWrapper._callback_handler = p_context;
            modInstallerWrapper._select = p_select_callback;
            modInstallerWrapper._cont = p_cont_callback;
            modInstallerWrapper._cancel = p_cancel_callback;

            return return_value_void.AsValue(false);
        }
        catch (Exception e)
        {
            return return_value_void.AsException(e, false);
        }
        finally
        {
            Utils2.LibraryAliveCount().Should().Be(1);
        }
    }

    public static unsafe return_value_void* UiEndDialog(param_ptr* handler)
    {
        //Utils2.LibraryAliveCount().Should().Be(0);
        try
        {
            var modInstallerWrapper = (ModInstallerWrapper) GCHandle.FromIntPtr((IntPtr) handler).Target!;

            modInstallerWrapper._callback_handler = null!;
            modInstallerWrapper._select = null!;
            modInstallerWrapper._cont = null!;
            modInstallerWrapper._cancel = null!;

            modInstallerWrapper._installerSteps = null!;
            modInstallerWrapper._currentStep = 0;

            return return_value_void.AsValue(false);
        }
        catch (Exception e)
        {
            return return_value_void.AsException(e, false);
        }
        finally
        {
            //Utils2.LibraryAliveCount().Should().Be(1);
        }
    }

    private bool dialogInProgress = false;
    public static unsafe return_value_void* UiUpdateState(param_ptr* handler, param_json* p_install_steps, param_int current_step)
    {
        //Utils2.LibraryAliveCount().Should().Be(0);
        try
        {
            var modInstallerWrapper = (ModInstallerWrapper) GCHandle.FromIntPtr((IntPtr) handler).Target!;
            
            var installSteps = BUTR.NativeAOT.Shared.Utils.DeserializeJson(p_install_steps, SourceGenerationContext.Default.InstallerStepArray);
            var currentStepValue = (int) current_step;

            modInstallerWrapper._installerSteps = installSteps;
            modInstallerWrapper._currentStep = currentStepValue;
            
            if (modInstallerWrapper.dialogInProgress)
                return return_value_void.AsValue(false);

            if (modInstallerWrapper._callback_handler is null)
                throw new NotSupportedException();

            // Need a copy because after Thread.Sleep
            // modInstallerWrapper._callback_handler is null
            // Does it call EndDialog??
            var callbackHandler = modInstallerWrapper._callback_handler;
            var select = modInstallerWrapper._select;
            var cont = modInstallerWrapper._cont;

            if (modInstallerWrapper._unattended)
            {
                if (modInstallerWrapper._cont is null)
                    throw new NotSupportedException();

                cont(callbackHandler, true, currentStepValue, return_value_void.AsValue(false));
            }
            else if (modInstallerWrapper._dialogChoices is { Count: > 0 })
            {
                var option = modInstallerWrapper._dialogChoices.FirstOrDefault(x => x.StepId == currentStepValue);

                using var pluginIds = Utils2.ToJson(option.PluginIds);
                modInstallerWrapper.dialogInProgress = true;
                select(callbackHandler, option.StepId, option.GroupId, pluginIds, return_value_void.AsValue(false));

                Thread.Sleep(200);
                cont(callbackHandler, true, currentStepValue, return_value_void.AsValue(false));
                modInstallerWrapper.dialogInProgress = false;
            }

            return return_value_void.AsValue(false);
        }
        catch (Exception e)
        {
            return return_value_void.AsException(e, false);
        }
        finally
        {
            //Utils2.LibraryAliveCount().Should().Be(1);
        }
    }
}