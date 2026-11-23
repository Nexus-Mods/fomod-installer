using System.Collections.Generic;

namespace FomodInstaller.Interface.snapshot
{
    /// <summary>
    /// Discriminator values for <see cref="ConditionSnapshot.Kind"/>.
    /// </summary>
    public static class ConditionKind
    {
        /// <summary>A condition on the state of a plugin (present/absent/active). Re-evaluable after the fact.</summary>
        public const string Plugin = "Plugin";

        /// <summary>A condition on the value of a flag set by another option. Re-evaluable after the fact.</summary>
        public const string Flag = "Flag";

        /// <summary>An And/Or combination of sub-conditions.</summary>
        public const string Composite = "Composite";

        /// <summary>
        /// A condition on the environment (game version, script extender version, mod manager
        /// version). These cannot change as a result of what the user installs, so a consumer
        /// should treat them as stable and skip them.
        /// </summary>
        public const string Environment = "Environment";

        /// <summary>A condition kind this installer does not know. Treat as opaque.</summary>
        public const string Unknown = "Unknown";
    }

    /// <summary>
    /// A structured, self-sufficient description of one ICondition, carrying enough information
    /// for a consumer outside this repository to evaluate it without the installer.
    /// </summary>
    /// <remarks>
    /// This is a single flat type with a Kind discriminator rather than a class hierarchy, so that
    /// Native AOT source generation needs no polymorphic handling and an older consumer that meets
    /// an unknown Kind can still read the rest of the record.
    /// </remarks>
    public class ConditionSnapshot
    {
        /// <summary>One of the ConditionKind values.</summary>
        public string Kind { get; set; }

        /// <summary>For Plugin: the plugin path the condition tests.</summary>
        public string PluginPath { get; set; }

        /// <summary>For Plugin: the required state, Missing/Inactive/Active.</summary>
        public string PluginState { get; set; }

        /// <summary>For Flag: the flag name the condition tests.</summary>
        public string FlagName { get; set; }

        /// <summary>
        /// For Flag: the required value. Null or empty means the condition is fulfilled when the
        /// flag is unset or empty.
        /// </summary>
        public string FlagValue { get; set; }

        /// <summary>For Composite: And or Or.</summary>
        public string Operator { get; set; }

        /// <summary>For Composite: the sub-conditions, in evaluation order.</summary>
        public List<ConditionSnapshot> Conditions { get; set; }

        /// <summary>
        /// For Environment: which environment condition this is, one of GameVersion,
        /// ScriptExtender, ModManager or Version.
        /// </summary>
        public string EnvironmentType { get; set; }

        /// <summary>For Environment: the minimum version required, as declared.</summary>
        public string MinimumVersion { get; set; }

        /// <summary>
        /// For Environment: the script extender identifier (fose, nvse, f4se, skse) when
        /// EnvironmentType is ScriptExtender.
        /// </summary>
        public string Extender { get; set; }

        /// <summary>
        /// The human-readable explanation this condition renders, or null when it renders none.
        /// </summary>
        public string Message { get; set; }
    }
}
