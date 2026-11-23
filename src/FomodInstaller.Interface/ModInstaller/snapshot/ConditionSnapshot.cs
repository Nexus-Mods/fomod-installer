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
        /// version).
        /// </summary>
        /// <remarks>
        /// These are <b>not</b> re-evaluable from the snapshot. The requirement is recorded
        /// (<see cref="ConditionSnapshot.MinimumVersion"/>) but the version actually observed at
        /// install time is not, so a consumer cannot tell whether the requirement was met.
        /// <para>
        /// Treat this kind as <i>undecidable</i>, not as unfulfilled. Undecidability propagates:
        /// an enclosing <see cref="Composite"/> is undecidable unless a sibling already decides it,
        /// and a pattern whose condition is undecidable makes the whole resolution undecidable
        /// because it may have been the first pattern to match. A consumer that reaches that point
        /// must fall back to the recorded <see cref="OptionSnapshot.Type"/> instead of guessing.
        /// </para>
        /// <para>
        /// Assuming the condition failed is the one answer that is certainly wrong: it reports an
        /// option the installer found usable as having become unusable, which is the false alarm
        /// this snapshot exists to avoid.
        /// </para>
        /// </remarks>
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
