using FomodInstaller.Interface;
using FomodInstaller.Interface.ui;

namespace FomodInstaller.Scripting.XmlScript
{
    public struct OptionsPresetChoice
    {
        public string name;
        public int idx;
    }

    public struct OptionsPresetGroup
    {
        public string name;
        public OptionsPresetChoice[] choices;
    }

    public struct OptionsPresetStep
    {
        public string name;
        public OptionsPresetGroup[] groups;
    }

    public struct OptionsPreset
    {
        public OptionsPresetStep[] steps;
    }

    /// <summary>
    /// Executes an XML script.
    /// </summary>
    public class XmlScriptExecutor : ScriptExecutorBase
    {
        private Mod ModArchive;
        private ICoreDelegates m_Delegates;
        private ConditionStateManager? m_csmState;
        private ISet<Option>? m_SelectedOptions;
        private OptionsPreset? m_Preset;

        #region Constructors

        /// <summary>
        /// A simple constructor that initializes the object with the required dependencies.
        /// </summary>
        /// <param name="modArchive">The mod object containing the file list in the mod archive.</param>
        /// <param name="UserInteractionDelegate">The utility class to use to install the mod items.</param>
        /// <param name="scxUIContext">The <see cref="SynchronizationContext"/> to use to marshall UI interactions to the UI thread.</param>		
        public XmlScriptExecutor(Mod modArchive, ICoreDelegates coreDelegates)
        {
            ModArchive = modArchive;
            m_Delegates = coreDelegates;
        }

        #endregion

        #region ScriptExecutorBase Members

        /// <summary>
        /// Executes the script.
        /// </summary>
        /// <param name="scpScript">The XML Script to execute.</param>
        /// <param name="dataPath">path where data files for the script are stored</param>
        /// <param name="preset">preset for the installer</param>
        /// <returns><c>true</c> if the script completes successfully;
        /// <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="scpScript"/> is not an
        /// <see cref="XmlScript"/>.</exception>
        public async override Task<IList<Instruction>> DoExecute(IScript scpScript, string dataPath, dynamic preset)
        {
            TaskCompletionSource<IList<Instruction>> Source = new TaskCompletionSource<IList<Instruction>>(); 
            List<InstallableFile> PluginsToActivate = new List<InstallableFile>();

            m_csmState = new ConditionStateManager();

            m_Preset = convertPreset(preset);
            
            if (!(scpScript is XmlScript))
                throw new ArgumentException("The given script must be of type XmlScript.", scpScript.Type.TypeName);

            XmlScript xscScript = (XmlScript)scpScript;

            if ((xscScript.ModPrerequisites != null) && !xscScript.ModPrerequisites.GetIsFulfilled(m_csmState, m_Delegates))
            {
                Source.SetResult(new List<Instruction>(new Instruction[] {
                    Instruction.InstallError("fatal", "Installer Prerequisits not fulfilled: " + xscScript.ModPrerequisites.GetMessage(m_csmState, m_Delegates, false))
                }));
                return await Source.Task;
            }

            IList<InstallStep> lstSteps = xscScript.InstallSteps;
            fixSteps(lstSteps);

            HeaderInfo hifHeaderInfo = xscScript.HeaderInfo;
            if (string.IsNullOrEmpty(hifHeaderInfo.ImagePath))
                hifHeaderInfo.ImagePath = string.IsNullOrEmpty(ModArchive.ScreenshotPath) ? null : Path.Combine(ModArchive.Prefix!, ModArchive.ScreenshotPath);
            if ((hifHeaderInfo.Height < 0) && hifHeaderInfo.ShowImage)
                hifHeaderInfo.Height = 75;

            m_SelectedOptions = new HashSet<Option>();

            int stepIdx = findNextIdx(lstSteps, -1);

            Action<int, int, int[]> select = (int stepId, int groupId, int[] optionIds) =>
            {
                // this needs to happen asynchronously so that this call returns to javascript and js can
                // return to its main loop and respond to delegated requests, otherwise we could dead-lock
                Task.Run(() =>
                {
                    ISet<int> selectedIds = new HashSet<int>(optionIds);
                    IList<Option> options = lstSteps[stepId].OptionGroups[groupId].Options;
                    for (int i = 0; i < options.Count; ++i)
                    {
                        if (selectedIds.Contains(i) || (resolveOptionType(options[i]) == OptionType.Required))
                            enableOption(options[i]);
                        else
                            disableOption(options[i]);
                    }

                    fixSelected(lstSteps[stepIdx]);
                    sendState(lstSteps, ModArchive.Prefix!, stepIdx);
                });
            };

            Action<bool, int> cont = (bool forward, int currentStep) => {
                // this needs to happen asynchronously, see above
                Task.Run(() =>
                {
                    if ((currentStep != -1) && (stepIdx != currentStep))
                    {
                        return;
                    }
                    if (forward)
                    {
                        stepIdx = findNextIdx(lstSteps, stepIdx);
                    }
                    else
                    {
                        stepIdx = findPrevIdx(lstSteps, stepIdx);
                    }

                    processStep(lstSteps, stepIdx, Source, xscScript, PluginsToActivate);
                });
            };
            Action cancel = () => {
                // this needs to happen asynchronously, see above
                Task.Run(() =>
                {
                    Source.SetCanceled();
                });
            };

            string? bannerPath = string.IsNullOrEmpty(hifHeaderInfo.ImagePath)
                ? null
                : Path.Combine(ModArchive.Prefix ?? "", hifHeaderInfo.ImagePath);
            m_Delegates.ui.StartDialog(hifHeaderInfo.Title,
                new HeaderImage(bannerPath, hifHeaderInfo.ShowFade, hifHeaderInfo.Height),
                select, cont, cancel);

            processStep(lstSteps, stepIdx, Source, xscScript, PluginsToActivate);

            return await Source.Task;
        }

        private void processStep(IList<InstallStep> lstSteps, int stepIdx, TaskCompletionSource<IList<Instruction>> Source,
                                 XmlScript xscScript, List<InstallableFile> PluginsToActivate)
        {
            if (stepIdx == -1)
            {
                m_Delegates.ui.EndDialog();

                XmlScriptInstaller xsiInstaller = new XmlScriptInstaller(ModArchive);
                IEnumerable<InstallableFile> FilesToInstall = new List<InstallableFile>();
                foreach (InstallStep step in lstSteps)
                {
                    foreach (OptionGroup group in step.OptionGroups)
                    {
                        foreach (Option option in group.Options)
                        {
                            if (m_SelectedOptions!.Contains(option))
                            {
                                FilesToInstall = FilesToInstall.Union(option.Files);
                            } else
                            {
                                IEnumerable<InstallableFile> installAnyway = option.Files.Where(
                                  file => file.AlwaysInstall ||
                                  (file.InstallIfUsable && option.GetOptionType(m_csmState!, m_Delegates) != OptionType.NotUsable));
                                if (installAnyway.Count() > 0)
                                {
                                    FilesToInstall = FilesToInstall.Union(installAnyway);
                                }
                            }
                        }
                    }
                }

                Source.SetResult(xsiInstaller.Install(xscScript, m_csmState!, m_Delegates, FilesToInstall, PluginsToActivate));
            }
            else
            {
                preselectOptions(lstSteps[stepIdx]);
                sendState(lstSteps, ModArchive.Prefix!, stepIdx);
            }
        }

        private void enableOption(Option option)
        {
            m_SelectedOptions!.Add(option);
            option.Flags.ForEach((ConditionalFlag flag) =>
            {
                m_csmState!.SetFlagValue(flag.Name, flag.ConditionalValue, option);
            });
        }

        private void disableOption(Option option)
        {
            m_SelectedOptions!.Remove(option);
            m_csmState!.RemoveFlags(option);
        }

        private OptionType resolveOptionType(Option opt)
        {
            return opt.GetOptionType(m_csmState!, m_Delegates);
        }


        private void preselectOptions(InstallStep step)
        {
            IList<OptionsPresetStep>? stepPresets = null;
            if (m_Preset.HasValue)
            {
                // step names are not unique :(
                stepPresets = m_Preset.Value.steps.Where(preStep => preStep.name == step.Name).ToList();
            }

            foreach (OptionGroup group in step.OptionGroups)
            {
                List<OptionsPresetGroup> groupPresets = new List<OptionsPresetGroup>();
                if ((stepPresets != null) && stepPresets.Count > 0) {
                    foreach (OptionsPresetStep stepPreset in stepPresets)
                    {
                        // group names not unique either...
                        groupPresets.AddRange(stepPreset.groups.Where(preGroup => preGroup.name == group.Name));
                    }
                }

                if (group.Options.FirstOrDefault(opt => m_SelectedOptions!.Contains(opt)) == null)
                {
                    bool setFirst = group.Type == OptionGroupType.SelectExactlyOne;
                    foreach (Option option in group.Options)
                    {
                        OptionType type = resolveOptionType(option);
                        bool isPreset = false;
                        foreach (OptionsPresetGroup groupPreset in groupPresets)
                        {
                            if (groupPreset.choices.Any(preChoice => preChoice.name == option.Name))
                            {
                                isPreset = true;
                            }
                        }
                        if (isPreset && type == OptionType.NotUsable)
                        {
                            // force preset options to be selectable, otherwise the user might not be able to deselect it
                            // even if it is actually invalid
                            option.OptionTypeResolver = new StaticOptionTypeResolver(OptionType.CouldBeUsable);
                            type = OptionType.CouldBeUsable;
                        }
                        if ((type == OptionType.Required)
                            || (!m_Preset.HasValue && (type == OptionType.Recommended))
                            || (group.Type == OptionGroupType.SelectAll)
                            || isPreset)
                        {
                            // in case there are multiple recommended options in a group that only
                            // supports one selection, disable all other options, otherwise we would
                            // create an invalid pre-selection
                            if (!m_Preset.HasValue
                                && (type == OptionType.Recommended)
                                && ((group.Type == OptionGroupType.SelectExactlyOne)
                                    || (group.Type == OptionGroupType.SelectAtMostOne)))
                            {
                                foreach (Option innerOption in group.Options)
                                {
                                    disableOption(innerOption);
                                }
                            }

                            setFirst = false;

                            if ((groupPresets.Count > 0) && !isPreset)
                            {
                                // We have a groupPreset setting available and this option is _not_
                                //  part of it - disable the option.
                                disableOption(option);
                            } else {
                                enableOption (option);
                            }
                        }
                    }

                    if (setFirst)
                    {
                        enableOption(group.Options[0]);
                    }
                }
            }
        }

        private void fixSteps(IList<InstallStep> steps)
        {
            // fix incompatible step options
            foreach (InstallStep step in steps)
            {
                foreach (OptionGroup group in step.OptionGroups)
                {
                    int requiredCount = group.Options.Count(opt => resolveOptionType(opt) == OptionType.Required);
                    if ((requiredCount > 1) && (group.Type == OptionGroupType.SelectAtMostOne))
                    {
                        // multiple required but there should only be 0-1 selected.
                        group.Type = OptionGroupType.SelectAny;
                    } else if ((requiredCount > 1) && (group.Type == OptionGroupType.SelectExactlyOne))
                    {
                        // multiple required but there should be exactly one selected.
                        group.Type = OptionGroupType.SelectAtLeastOne;
                    }
                }
            }
        }

        private void fixSelected(InstallStep step)
        {
            foreach (OptionGroup group in step.OptionGroups)
            {
                foreach (Option option in group.Options)
                {
                    OptionType type = resolveOptionType(option);
                    if (type == OptionType.Required)
                        enableOption(option);
                    else if (type == OptionType.NotUsable)
                        disableOption(option);
                }
            }
        }

        private void sendState(IList<InstallStep> lstSteps, string strPrefixPath, int stepIdx)
        {
            Func<IEnumerable<InstallStep>, IEnumerable<InstallerStep>> convertSteps = steps =>
            {
                int idx = 0;
                return steps.Select(step => new InstallerStep(idx++, step.Name,
                    step.VisibilityCondition == null || step.VisibilityCondition.GetIsFulfilled(m_csmState!, m_Delegates)));
            };

            Func<IEnumerable<Option>, OptionsPresetGroup?, bool, IEnumerable<Interface.ui.Option>> convertOptions = (options, groupPreset, selectAll) =>
            {
                int idx = 0;
                return options.Select(option =>
                {
                    OptionType type = resolveOptionType(option);

                    bool choicePreset = false;
                    if (groupPreset.HasValue
                        && (type != OptionType.Required)
                        && !selectAll
                        && (groupPreset.Value.choices != null))
                    {
                        choicePreset = groupPreset.Value.choices.Any(preOption => preOption.name == option.Name);
                    }

                    string? conditionMsg = type == OptionType.NotUsable
                    ? option.GetConditionMessage(m_csmState!, m_Delegates)
                    : null;
                    return new Interface.ui.Option(idx++, option.Name, option.Description,
                      string.IsNullOrEmpty(option.ImagePath) ? null : Path.Combine(strPrefixPath, option.ImagePath),
                      m_SelectedOptions!.Contains(option), choicePreset, type.ToString(), conditionMsg);
                });
            };

            Func<IEnumerable<OptionGroup>, OptionsPresetStep?, IEnumerable<Group>> convertGroups = (groups, stepPreset) =>
            {
                int idx = 0;
                return groups.Select(group =>
                {
                    OptionsPresetGroup? groupPreset = null;
                    if ((stepPreset.HasValue) && (stepPreset.Value.groups != null)) {
                        groupPreset = stepPreset.Value.groups.FirstOrDefault(preGroup => preGroup.name == group.Name);
                    }

                    return new Group(idx++, group.Name, group.Type.ToString(),
                                     convertOptions(group.Options, groupPreset, group.Type == OptionGroupType.SelectAll).ToArray());
                });
            };

            Action<InstallerStep[], int> insertGroups = (InstallerStep[] steps, int idx) =>
            {
                InstallStep inStep = lstSteps[idx];
                OptionsPresetStep? stepPreset = null;
                if ((m_Preset.HasValue) && (m_Preset.Value.steps != null))
                {
                    stepPreset = m_Preset.Value.steps.FirstOrDefault(preStep => preStep.name == inStep.Name);
                }

                steps[idx].optionalFileGroups.order = inStep.GroupSortOrder.ToString();
                steps[idx].optionalFileGroups.group = convertGroups(inStep.OptionGroups, stepPreset).ToArray();
            };

            InstallerStep[] uiSteps = convertSteps(lstSteps).ToArray();
            // previously we only sent the groups for the current step but sending all makes it easier to track and save all
            // installer choices made
            for (int i = 0; i < lstSteps.Count; ++i)
            {
              insertGroups(uiSteps, i);
            }
            m_Delegates.ui.UpdateState(uiSteps, stepIdx);
        }

        private int findNextIdx(IList<InstallStep> lstSteps, int currentIdx)
        {
            for (int i = currentIdx + 1; i < lstSteps.Count; ++i) {
                if ((lstSteps[i].VisibilityCondition == null) ||
                    lstSteps[i].VisibilityCondition!.GetIsFulfilled(m_csmState!, m_Delegates))
                {
                    return i;
                }
            }
            return -1;
        }

        private int findPrevIdx(IList<InstallStep> lstSteps, int currentIdx)
        {
            for (int i = currentIdx - 1; i >= 0; --i) {
                if ((lstSteps[i].VisibilityCondition == null) ||
                    lstSteps[i].VisibilityCondition!.GetIsFulfilled(m_csmState!, m_Delegates))
                {
                    return i;
                }
            }
            return 0;
        }

        private OptionsPreset? convertPreset(dynamic input)
        {
            if (input == null)
            {
                return null;
            }

            Func<IEnumerable<dynamic>?, IEnumerable<OptionsPresetChoice>> convertChoices = choiceIn =>
            {
                if (choiceIn == null)
                {
                    return new List<OptionsPresetChoice> { };
                }
                return choiceIn.Select(choice => new OptionsPresetChoice() { name = choice.name, idx = choice.idx });
            };

            Func<IEnumerable<dynamic>?, IEnumerable<OptionsPresetGroup>> convertGroups = groupsIn =>
            {
                if (groupsIn == null)
                {
                    return new List<OptionsPresetGroup> { };
                }
                return groupsIn.Select(group => new OptionsPresetGroup() { name = group.name, choices = convertChoices(group["choices"] as IEnumerable<dynamic>).ToArray() });
            };

            Func<IEnumerable<dynamic>?, IEnumerable<OptionsPresetStep>> convertSteps = stepsIn =>
            {
                if (stepsIn == null)
                {
                    return new List<OptionsPresetStep> { };
                }
                return stepsIn.Select (step => new OptionsPresetStep () { name = step.name, groups = convertGroups(step["groups"] as IEnumerable<dynamic>).ToArray() });
            };

            return new OptionsPreset() { steps = convertSteps(input).ToArray() };
        }

        #endregion
    }
}
