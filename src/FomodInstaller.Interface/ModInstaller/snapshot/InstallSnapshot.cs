using System.Collections.Generic;

namespace FomodInstaller.Interface.snapshot
{
    /// <summary>
    /// Discriminator values for <see cref="ResolverSnapshot.Kind"/>.
    /// </summary>
    public static class ResolverKind
    {
        /// <summary>A resolver that always returns the same option type.</summary>
        public const string Static = "Static";

        /// <summary>A resolver that picks the first matching pattern, else a default type.</summary>
        public const string Conditional = "Conditional";
    }

    /// <summary>
    /// One entry of a conditional resolver's pattern list.
    /// </summary>
    public class PatternSnapshot
    {
        /// <summary>The option type this pattern yields when its condition is fulfilled.</summary>
        public string Type { get; set; }

        /// <summary>The condition that must be fulfilled for this pattern to be elected.</summary>
        public ConditionSnapshot Condition { get; set; }
    }

    /// <summary>
    /// A structured description of an IOptionTypeResolver, sufficient to reproduce the option
    /// type the installer would compute for any given state.
    /// </summary>
    /// <remarks>
    /// Evaluation order for <see cref="ResolverKind.Conditional"/>, matching what
    /// ConditionalOptionTypeResolver.ResolveOptionType actually implements: walk
    /// <see cref="Patterns"/> in order and return the Type of the first pattern whose Condition
    /// is fulfilled. If none is fulfilled, return <see cref="DefaultType"/>.
    /// <para>
    /// A condition may be <i>undecidable</i> from the snapshot alone - an
    /// <see cref="ConditionKind.Environment"/> leaf, because the version observed at install time
    /// is not recorded. Undecidability propagates through the tree, and a pattern whose condition
    /// is undecidable makes the rest of the walk undecidable too, because it may have been the
    /// first pattern to match. A consumer that reaches an undecidable pattern must therefore
    /// return the recorded <see cref="OptionSnapshot.Type"/> rather than continuing to the next
    /// pattern or falling through to <see cref="DefaultType"/>.
    /// </para>
    /// </remarks>
    public class ResolverSnapshot
    {
        /// <summary>One of the ResolverKind values.</summary>
        public string Kind { get; set; }

        /// <summary>For Static: the fixed option type.</summary>
        public string Type { get; set; }

        /// <summary>For Conditional: the type returned when no pattern matches.</summary>
        public string DefaultType { get; set; }

        /// <summary>For Conditional: the patterns, in the order they are evaluated.</summary>
        public List<PatternSnapshot> Patterns { get; set; }
    }

    /// <summary>
    /// A flag an option sets when it is enabled.
    /// </summary>
    public class FlagSnapshot
    {
        /// <summary>The flag name.</summary>
        public string Name { get; set; }

        /// <summary>The value the flag is set to when the option is enabled.</summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// The snapshot of a single option: the type it resolved to at install time, the resolver
    /// that produced it, and the flags it sets.
    /// </summary>
    public class OptionSnapshot
    {
        /// <summary>The option name, as declared by the mod author.</summary>
        public string Name { get; set; }

        /// <summary>
        /// The option type the installer resolved for this option at the moment of install.
        /// </summary>
        public string Type { get; set; }

        /// <summary>Whether the option was selected in the install that produced this snapshot.</summary>
        public bool Selected { get; set; }

        /// <summary>The resolver that produced <see cref="Type"/>.</summary>
        public ResolverSnapshot Resolver { get; set; }

        /// <summary>The flags this option sets when enabled.</summary>
        public List<FlagSnapshot> Flags { get; set; }
    }

    /// <summary>
    /// The snapshot of one option group.
    /// </summary>
    public class GroupSnapshot
    {
        /// <summary>The group name.</summary>
        public string Name { get; set; }

        /// <summary>The group type, e.g. SelectExactlyOne.</summary>
        public string Type { get; set; }

        /// <summary>The options in the group, in display order.</summary>
        public List<OptionSnapshot> Options { get; set; }
    }

    /// <summary>
    /// The snapshot of one install step.
    /// </summary>
    public class StepSnapshot
    {
        /// <summary>The step index, matching the index used by the UI protocol.</summary>
        public int Id { get; set; }

        /// <summary>The step name.</summary>
        public string Name { get; set; }

        /// <summary>Whether the step was visible given the state at install time.</summary>
        public bool Visible { get; set; }

        /// <summary>
        /// The condition that governs the step's visibility, or null when the step is always
        /// visible.
        /// </summary>
        public ConditionSnapshot VisibilityCondition { get; set; }

        /// <summary>The groups in this step.</summary>
        public List<GroupSnapshot> Groups { get; set; }
    }

    /// <summary>
    /// The complete option-condition snapshot for one install.
    /// </summary>
    /// <remarks>
    /// This is what makes it possible to tell, long after an install finished, that an option
    /// which was unusable then has since become usable - without re-parsing the mod's
    /// ModuleConfig.xml and without a second implementation of the parser's semantics.
    /// </remarks>
    public class InstallSnapshot
    {
        /// <summary>The steps, in install order.</summary>
        public List<StepSnapshot> Steps { get; set; }

        /// <summary>
        /// The flag values in effect when the install completed, as name/value pairs. A consumer
        /// replaying flag conditions can start from these rather than from nothing.
        /// </summary>
        public List<FlagSnapshot> Flags { get; set; }
    }
}
