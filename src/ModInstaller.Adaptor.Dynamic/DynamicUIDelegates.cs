using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;

namespace FomodInstaller.Interface.ui;

using SelectCB = Action<int, int, int[]>;
using ContinueCB = Action<bool, int>;
using CancelCB = Action;

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

struct StartParameters
{
    public string moduleName;
    public HeaderImage image;
    public Func<object, Task<object>> select;
    public Func<object, Task<object>> cont;
    public Func<object, Task<object>> cancel;

    public StartParameters(string moduleName, HeaderImage image, SelectCB select, ContinueCB cont, CancelCB cancel)
    {
        this.moduleName = moduleName;
        this.image = image;
        this.select = async (dynamic selectPar) =>
        {
            object[] pluginObjs = selectPar.plugins;
            IEnumerable<int> pluginIds = pluginObjs.Select(id => (int)id);
            select(selectPar.stepId, selectPar.groupId, pluginIds.ToArray());
            return await Task.FromResult<object>(null);
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
            return await Task.FromResult<object>(null);
        };

        this.cancel = async (dynamic dummy) =>
        {
            cancel();
            return await Task.FromResult<object>(null);
        };
    }
}

public class DynamicUIDelegates : UIDelegates
{
    private Func<object, Task<object>> mStartDialog;
    private Func<object, Task<object>> mEndDialog;
    private Func<object, Task<object>> mUpdateState;
    private Func<object, Task<object>> mReportError;

    public DynamicUIDelegates(dynamic source)
    {
        mStartDialog = source.startDialog;
        mEndDialog = source.endDialog;
        mUpdateState = source.updateState;
        mReportError = source.reportError;
    }

    public override async void StartDialog(string moduleName, HeaderImage image, Action<int, int, int[]> select, Action<bool, int> cont, Action cancel)
    {
        try
        {
            await Shared.TimeoutRetry(() => mStartDialog(new StartParameters(moduleName, image, select, cont, cancel)));
        } catch (Exception e)
        {
            Console.WriteLine("exception in start dialog: {0}", e);
        }
    }

    public override async void EndDialog()
    {
        try
        {
            await Shared.TimeoutRetry(() => mEndDialog(null));
        } catch (Exception e)
        {
            Console.WriteLine("exception in end dialog: {0}", e);
        }
    }

    public override async void UpdateState(InstallerStep[] installSteps, int currentStep)
    {
        try
        {
            await Shared.TimeoutRetry(() => mUpdateState(new UpdateParameters(installSteps, currentStep)));
        } catch (Exception e)
        {
            Console.WriteLine("exception in update state: {0}", e);
        }
    }

    public override async void ReportError(string title, string message, string details)
    {
        try
        {
            await Shared.TimeoutRetry(() => mReportError(new Dictionary<string, dynamic> {
                { "title", title },
                { "message", message },
                { "details", details }
            }));
        }
        catch (Exception e)
        {
            Console.WriteLine("exception in report error: {0}", e);
        }
    }
}