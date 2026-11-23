using System;
using System.Collections.Generic;
using System.Linq;
using FomodInstaller.Interface;
using FomodInstaller.Interface.snapshot;

namespace FomodInstaller.Scripting.XmlScript
{
    /// <summary>
    /// Builds an InstallSnapshot from the option tree an XmlScriptExecutor has already parsed and
    /// evaluated. This serializes objects that already exist; it adds no new evaluation semantics
    /// and does not influence the install in any way.
    /// </summary>
    public static class InstallSnapshotBuilder
    {
        /// <summary>
        /// Builds the snapshot for a completed install.
        /// </summary>
        /// <param name="lstSteps">The install steps, after the executor has finished selecting.</param>
        /// <param name="csmState">The condition state at the end of the install.</param>
        /// <param name="coreDelegates">The Core delegates component.</param>
        /// <param name="selectedOptions">The options the install selected.</param>
        public static InstallSnapshot Build(IList<InstallStep> lstSteps,
                                            ConditionStateManager csmState,
                                            CoreDelegates coreDelegates,
                                            ISet<Option> selectedOptions)
        {
            var snapshot = new InstallSnapshot
            {
                Steps = new List<StepSnapshot>(),
                Flags = csmState.FlagValues
                    .Select(kvp => new FlagSnapshot { Name = kvp.Key, Value = kvp.Value })
                    .ToList(),
            };

            int stepId = 0;
            foreach (InstallStep step in lstSteps)
            {
                var stepSnapshot = new StepSnapshot
                {
                    Id = stepId++,
                    Name = step.Name,
                    Visible = step.GetIsVisible(csmState, coreDelegates),
                    VisibilityCondition = ToConditionSnapshot(step.VisibilityCondition, csmState, coreDelegates),
                    Groups = new List<GroupSnapshot>(),
                };

                foreach (OptionGroup group in step.OptionGroups)
                {
                    var groupSnapshot = new GroupSnapshot
                    {
                        Name = group.Name,
                        Type = group.Type.ToString(),
                        Options = new List<OptionSnapshot>(),
                    };

                    foreach (Option option in group.Options)
                    {
                        groupSnapshot.Options.Add(new OptionSnapshot
                        {
                            Name = option.Name,
                            Type = ResolveType(option, csmState, coreDelegates),
                            Selected = selectedOptions != null && selectedOptions.Contains(option),
                            Resolver = ToResolverSnapshot(option.OptionTypeResolver, csmState, coreDelegates),
                            Flags = (option.Flags ?? new List<ConditionalFlag>())
                                .Select(flag => new FlagSnapshot { Name = flag.Name, Value = flag.ConditionalValue })
                                .ToList(),
                        });
                    }

                    stepSnapshot.Groups.Add(groupSnapshot);
                }

                snapshot.Steps.Add(stepSnapshot);
            }

            return snapshot;
        }

        private static string ResolveType(Option option, ConditionStateManager csmState, CoreDelegates coreDelegates)
        {
            try
            {
                return option.GetOptionType(csmState, coreDelegates).ToString();
            }
            catch
            {
                // A resolver that cannot be evaluated must not fail the install; record the
                // absence rather than inventing a type.
                return null;
            }
        }

        /// <summary>
        /// Serializes a resolver, preserving the evaluation order a consumer must reproduce.
        /// </summary>
        public static ResolverSnapshot ToResolverSnapshot(IOptionTypeResolver resolver,
                                                          ConditionStateManager csmState,
                                                          CoreDelegates coreDelegates)
        {
            switch (resolver)
            {
                case StaticOptionTypeResolver staticResolver:
                    return new ResolverSnapshot
                    {
                        Kind = ResolverKind.Static,
                        Type = staticResolver.Type.ToString(),
                    };

                case ConditionalOptionTypeResolver conditionalResolver:
                    return new ResolverSnapshot
                    {
                        Kind = ResolverKind.Conditional,
                        DefaultType = conditionalResolver.DefaultType.ToString(),
                        Patterns = conditionalResolver.ConditionalTypePatterns
                            .Select(pattern => new PatternSnapshot
                            {
                                Type = pattern.Type.ToString(),
                                Condition = ToConditionSnapshot(pattern.Condition, csmState, coreDelegates),
                            })
                            .ToList(),
                    };

                default:
                    // An unknown resolver kind. Record that we could not describe it rather than
                    // silently claiming a static type.
                    return new ResolverSnapshot { Kind = ResolverKind.Static, Type = null };
            }
        }

        /// <summary>
        /// Serializes a condition tree, preserving structure. Environment conditions are recorded
        /// as their own kind because they cannot change after the install.
        /// </summary>
        public static ConditionSnapshot ToConditionSnapshot(ICondition condition,
                                                            ConditionStateManager csmState,
                                                            CoreDelegates coreDelegates)
        {
            if (condition == null)
                return null;

            switch (condition)
            {
                case PluginCondition pluginCondition:
                    return new ConditionSnapshot
                    {
                        Kind = ConditionKind.Plugin,
                        PluginPath = pluginCondition.PluginPath,
                        PluginState = pluginCondition.State.ToString(),
                    };

                case FlagCondition flagCondition:
                    return new ConditionSnapshot
                    {
                        Kind = ConditionKind.Flag,
                        FlagName = flagCondition.FlagName,
                        FlagValue = flagCondition.Value,
                    };

                case CompositeCondition compositeCondition:
                    return new ConditionSnapshot
                    {
                        Kind = ConditionKind.Composite,
                        Operator = compositeCondition.Operator.ToString(),
                        Conditions = compositeCondition.Conditions
                            .Select(sub => ToConditionSnapshot(sub, csmState, coreDelegates))
                            .ToList(),
                    };

                // Order matters: the concrete environment conditions derive from VersionCondition,
                // so they must be matched first.
                case SEVersionCondition seVersionCondition:
                    return EnvironmentSnapshot("ScriptExtender", seVersionCondition, condition,
                                              csmState, coreDelegates,
                                              extender: seVersionCondition.Extender);

                case GameVersionCondition gameVersionCondition:
                    return EnvironmentSnapshot("GameVersion", gameVersionCondition, condition,
                                              csmState, coreDelegates);

                case ModManagerCondition modManagerCondition:
                    return EnvironmentSnapshot("ModManager", modManagerCondition, condition,
                                              csmState, coreDelegates);

                case VersionCondition versionCondition:
                    return EnvironmentSnapshot("Version", versionCondition, condition,
                                              csmState, coreDelegates);

                default:
                    return new ConditionSnapshot
                    {
                        Kind = ConditionKind.Unknown,
                        Message = SafeMessage(condition, csmState, coreDelegates),
                    };
            }
        }

        private static ConditionSnapshot EnvironmentSnapshot(string environmentType,
                                                             VersionCondition versionCondition,
                                                             ICondition condition,
                                                             ConditionStateManager csmState,
                                                             CoreDelegates coreDelegates,
                                                             string extender = null)
        {
            return new ConditionSnapshot
            {
                Kind = ConditionKind.Environment,
                EnvironmentType = environmentType,
                MinimumVersion = versionCondition.MinimumVersion?.ToString(),
                Extender = extender,
                Message = SafeMessage(condition, csmState, coreDelegates),
            };
        }

        /// <summary>
        /// Renders a condition's message, tolerating conditions whose message rendering throws
        /// when the environment it needs is absent (e.g. SEVersionCondition with no extender
        /// version string).
        /// </summary>
        private static string SafeMessage(ICondition condition,
                                          ConditionStateManager csmState,
                                          CoreDelegates coreDelegates)
        {
            try
            {
                return condition.GetMessage(csmState, coreDelegates, false);
            }
            catch
            {
                return null;
            }
        }
    }
}
