using FomodInstaller.Interface.ui;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utils;

namespace FomodInstaller.Interface
{
    using SelectCB = Action<int, int, int[]>;
    using ContinueCB = Action<bool, int>;
    using CancelCB = Action;

    struct Defaults
    {
        public static int TIMEOUT_MS = 30000;
    }

    #region Plugin

    public class PluginDelegates : IPluginDelegates
    {
        private Func<object, Task<object>> mGetAll;
        private Func<object, Task<object>> mIsActive;
        private Func<object, Task<object>> mIsPresent;
        private string[]? mActiveCache;
        private string[]? mPresentCache;

        public PluginDelegates(dynamic source)
        {
            mGetAll = source.getAll;
            mIsActive = source.isActive;
            mIsPresent = source.isPresent;
        }

        public async Task<string[]> GetAll(bool activeOnly)
        {
            dynamic res = await Task.Run(() => mGetAll(activeOnly)).WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS));
            if (res != null)
            {
                IEnumerable enu = res;
                return enu.Cast<object>().Select(x => x.ToString() ?? "").ToArray();
            }
            else
                return new string[0];
        }

        public async Task<bool> IsActive(string pluginName)
        {
            if (mActiveCache == null)
            {
                mActiveCache = await GetAll(true);
            }
            return mActiveCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default(string);
        }

        public async Task<bool> IsPresent(string pluginName)
        {
            if (mPresentCache == null)
            {
                mPresentCache = await GetAll(false);
            }
            return mPresentCache.FirstOrDefault(p => p.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) != default(string);
        }
    }

    #endregion

    #region Ini

    public class IniDelegates : IIniDelegates
    {
        private Func<object[], Task<object>> mGetIniString;
        private Func<object[], Task<object>> mGetIniInt;

        public IniDelegates(dynamic source)
        {
            mGetIniString = source.getIniString;
            mGetIniInt = source.getIniInt;
        }

        public async Task<string> GetIniString(string iniFileName, string iniSection, string iniKey)
        {
            string[] Params = new string[] { iniFileName, iniSection, iniKey };
            object res = await mGetIniString(Params).WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS));
            if (res != null)
            {
                return res.ToString() ?? string.Empty;
            }
            else
                return string.Empty;
        }

        public async Task<int> GetIniInt(string iniFileName, string iniSection, string iniKey)
        {
            string[] Params = new string[] { iniFileName, iniSection, iniKey };
            object res = await mGetIniInt(Params).WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS));
            if (res != null)
            {
                return (int)res;
            }
            else
                return -1;
        }
    }

    #endregion

    #region Context

    public class ContextDelegates : IContextDelegates
    {
        private Func<object?, Task<object>> mGetAppVersion;
        private Func<object?, Task<object>> mGetCurrentGameVersion;
        private Func<object?, Task<object>> mGetExtenderVersion;
        private Func<object?, Task<object>> mIsExtenderPresent;
        private Func<object?, Task<object>> mCheckIfFileExists;
        private Func<object?, Task<object>> mGetExistingDataFile;
        private Func<object[], Task<object>> mGetExistingDataFileList;

        public ContextDelegates(dynamic source)
        {
            mGetAppVersion = source.getAppVersion;
            mGetCurrentGameVersion = source.getCurrentGameVersion;
            mCheckIfFileExists = source.checkIfFileExists;
            mGetExtenderVersion = source.getExtenderVersion;
            mIsExtenderPresent = source.isExtenderPresent;
            mGetExistingDataFile = source.getExistingDataFile;
            mGetExistingDataFileList = source.getExistingDataFileList;
        }

        public async Task<string> GetAppVersion()
        {
            object res = await mGetAppVersion(null).WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS));
            return (string)res;
        }

        public async Task<string> GetCurrentGameVersion()
        {
            object res = await mGetCurrentGameVersion(null).WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS));
            return (string)res;
        }

        public async Task<string> GetExtenderVersion(string extender)
        {
            object res = await mGetExtenderVersion(extender);
            return (string)res;
            // .WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS)));
            // return (string)res;
        }

        public async Task<bool> IsExtenderPresent()
        {
            object res = await mIsExtenderPresent(null);
            return (bool)res;
        }

        public async Task<bool> CheckIfFileExists(string fileName)
        {
            object res = await mCheckIfFileExists(fileName).WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS));
            return (bool)res;
        }

        public async Task<byte[]> GetExistingDataFile(string dataFile)
        {
            object res = await mGetExistingDataFile(dataFile).WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS));
            return (byte[])res;
        }

        public async Task<string[]> GetExistingDataFileList(string folderPath, string searchFilter, bool isRecursive)
        {
            object[] Params = new object[] { folderPath, searchFilter, isRecursive };
            object res = await mGetExistingDataFileList(Params).WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS));
            return ((object[])res).Select(iter => (string)iter).ToArray();
        }
    }

    #endregion

    #region UI

    public struct HeaderImage
    {
        public string? path;
        public bool showFade;
        public int height;

        public HeaderImage(string? path, bool showFade, int height) : this()
        {
            this.path = path;
            this.showFade = showFade;
            this.height = height;
        }
    }

    namespace ui
    {
        public struct Option
        {
            public int id;
            public bool selected;
            public bool preset;
            public string name;
            public string description;
            public string? image;
            public string type;
            public string? conditionMsg;

            public Option(int id, string name, string description, string? image, bool selected, bool preset, string type, string? conditionMsg) : this()
            {
                this.id = id;
                this.name = name;
                this.description = description;
                this.image = image;
                this.selected = selected;
                this.preset = preset;
                this.type = type;
                this.conditionMsg = conditionMsg;
            }
        }

        public struct Group
        {
            public int id;
            public string name;
            public string type;
            public Option[] options;

            public Group(int id, string name, string type, Option[] options) : this()
            {
                this.id = id;
                this.name = name;
                this.type = type;
                this.options = options;
            }
        }

        public struct GroupList
        {
            public Group[] group;
            public string order;
        }

        public struct InstallerStep
        {
            public int id;
            public string name;
            public bool visible;
            public GroupList optionalFileGroups;

            public InstallerStep(int id, string name, bool visible) : this()
            {
                this.id = id;
                this.name = name;
                this.visible = visible;
            }
        }

        struct StartParameters
        {
            public string? moduleName;
            public HeaderImage image;
            public Func<object, Task<object?>> select;
            public Func<object, Task<object?>> cont;
            public Func<object, Task<object?>> cancel;

            public StartParameters(string? moduleName, HeaderImage image, SelectCB select, ContinueCB cont, CancelCB cancel)
            {
                this.moduleName = moduleName;
                this.image = image;
                this.select = async (dynamic selectPar) =>
                {
                    object[] pluginObjs = selectPar.plugins;
                    IEnumerable<int> pluginIds = pluginObjs.Select(id => (int)id);
                    select(selectPar.stepId, selectPar.groupId, pluginIds.ToArray());
                    return await Task.FromResult<object?>(null);
                };

                this.cont = async (dynamic continuePar) =>
                {
                    string direction = continuePar.direction;
                    int currentStep = -1;
                    try
                    {
                        currentStep = continuePar.currentStepId;
                    }
                    catch (RuntimeBinderException)
                    {
                        // no problem, we'll just not validate if the message is for the expected page
                    }
                    cont(direction == "forward", currentStep);
                    return await Task.FromResult<object?>(null);
                };

                this.cancel = async (dynamic dummy) =>
                {
                    cancel();
                    return await Task.FromResult<object?>(null);
                };
            }
        }

        struct UpdateParameters
        {
            public InstallerStep[] installSteps;
            public int currentStep;

            public UpdateParameters(InstallerStep[] installSteps, int currentStep)
            {
                this.installSteps = installSteps;
                this.currentStep = currentStep;
            }
        }

        public class Delegates : IUIDelegates
        {
            private Func<object?, Task<object>> mStartDialog;
            private Func<object?, Task<object>> mEndDialog;
            private Func<object, Task<object>> mUpdateState;
            private Func<object, Task<object>> mReportError;

            public Delegates(dynamic source)
            {
                mStartDialog = source.startDialog;
                mEndDialog = source.endDialog;
                mUpdateState = source.updateState;
                mReportError = source.reportError;
            }

            public async void StartDialog(string? moduleName, HeaderImage image, SelectCB select, ContinueCB cont, CancelCB cancel)
            {
                try
                {
                    await mStartDialog(new StartParameters(moduleName, image, select, cont, cancel)).WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS));
                }
                catch (Exception e)
                {
                    Console.WriteLine("exception in start dialog: {0}", e);
                }
            }

            public async void EndDialog()
            {
                try
                {
                    await mEndDialog(null).WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS));
                }
                catch (Exception e)
                {
                    Console.WriteLine("exception in end dialog: {0}", e);
                }
            }

            public async void UpdateState(InstallerStep[] installSteps, int currentStep)
            {
                try
                {
                    await mUpdateState(new UpdateParameters(installSteps, currentStep)).WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS));
                }
                catch (Exception e)
                {
                    Console.WriteLine("exception in update state: {0}", e);
                }
            }

            public async void ReportError(string title, string message, string details)
            {
                try
                {
                    await mReportError(new Dictionary<string, dynamic> {
                        { "title", title },
                        { "message", message },
                        { "details", details }
                    }).WaitAsync(TimeSpan.FromMilliseconds(Defaults.TIMEOUT_MS));
                }
                catch (Exception e)
                {
                    Console.WriteLine("exception in report error: {0}", e);
                }
            }
        }
    }

    #endregion

    public class CoreDelegates : ICoreDelegates
    {
        private PluginDelegates mPluginDelegates;
        private ContextDelegates mContextDelegates;
        private IniDelegates mIniDelegates;
        private ui.Delegates mUIDelegates;

        public CoreDelegates(dynamic source)
        {
            mPluginDelegates = new PluginDelegates(source.plugin);
            mIniDelegates = new IniDelegates(source.ini);
            mContextDelegates = new ContextDelegates(source.context);
            mUIDelegates = new ui.Delegates(source.ui);
        }

        public IPluginDelegates plugin
        {
            get
            {
                return mPluginDelegates;
            }
        }

        public IIniDelegates ini
        {
            get
            {
                return mIniDelegates;
            }
        }

        public IContextDelegates context
        {
            get
            {
                return mContextDelegates;
            }
        }

        public IUIDelegates ui
        {
            get
            {
                return mUIDelegates;
            }
        }
    }
}
