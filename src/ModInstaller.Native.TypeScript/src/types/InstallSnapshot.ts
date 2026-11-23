/**
 * Structured option-condition snapshot.
 *
 * This is what makes it possible to tell, long after an install finished, that an option
 * which was unusable then has since become usable - without re-parsing the mod's
 * ModuleConfig.xml and without a second implementation of the parser's semantics.
 */

export type OptionType =
  | 'Required'
  | 'Optional'
  | 'Recommended'
  | 'NotUsable'
  | 'CouldBeUsable';

export type ConditionKind = 'Plugin' | 'Flag' | 'Composite' | 'Environment' | 'Unknown';

export type PluginState = 'Missing' | 'Inactive' | 'Active';

export type ConditionOperator = 'And' | 'Or';

export type ResolverKind = 'Static' | 'Conditional';

export type EnvironmentType = 'GameVersion' | 'ScriptExtender' | 'ModManager' | 'Version';

/**
 * One node of a condition tree.
 *
 * A single flat shape with a `kind` discriminator rather than a union of distinct types, so
 * that an older consumer meeting an unknown `kind` can still read the rest of the record.
 */
export interface ConditionSnapshot {
  kind: ConditionKind;

  /** For `Plugin`: the plugin path the condition tests. */
  pluginPath?: string | null;

  /** For `Plugin`: the required state. */
  pluginState?: PluginState | null;

  /** For `Flag`: the flag name the condition tests. */
  flagName?: string | null;

  /**
   * For `Flag`: the required value. Null or empty means the condition is fulfilled when the
   * flag is unset or empty.
   */
  flagValue?: string | null;

  /** For `Composite`: the operator combining the sub-conditions. */
  operator?: ConditionOperator | null;

  /** For `Composite`: the sub-conditions, in evaluation order. */
  conditions?: ConditionSnapshot[] | null;

  /** For `Environment`: which environment condition this is. */
  environmentType?: EnvironmentType | null;

  /** For `Environment`: the minimum version required, as declared. */
  minimumVersion?: string | null;

  /** For `Environment`: the script extender identifier (fose, nvse, f4se, skse). */
  extender?: string | null;

  /** The human-readable explanation this condition renders, when it renders one. */
  message?: string | null;
}

/** One entry of a conditional resolver's pattern list. */
export interface PatternSnapshot {
  /** The option type this pattern yields when its condition is fulfilled. */
  type: OptionType;
  /** The condition that must be fulfilled for this pattern to be elected. */
  condition: ConditionSnapshot;
}

/**
 * The resolver that produced an option's type.
 *
 * Evaluation order for `Conditional`, matching the installer: walk `patterns` in order and
 * return the `type` of the first pattern whose `condition` is fulfilled. If none is
 * fulfilled, return `defaultType`.
 */
export interface ResolverSnapshot {
  kind: ResolverKind;
  /** For `Static`: the fixed option type. */
  type?: OptionType | null;
  /** For `Conditional`: the type returned when no pattern matches. */
  defaultType?: OptionType | null;
  /** For `Conditional`: the patterns, in the order they are evaluated. */
  patterns?: PatternSnapshot[] | null;
}

/** A flag an option sets when it is enabled. */
export interface FlagSnapshot {
  name: string;
  value: string;
}

export interface OptionSnapshot {
  name: string;
  /** The option type the installer resolved for this option at the moment of install. */
  type?: OptionType | null;
  /** Whether the option was selected in the install that produced this snapshot. */
  selected: boolean;
  resolver: ResolverSnapshot;
  /** The flags this option sets when enabled. */
  flags: FlagSnapshot[];
}

export interface GroupSnapshot {
  name: string;
  type: string;
  options: OptionSnapshot[];
}

export interface StepSnapshot {
  id: number;
  name: string;
  visible: boolean;
  /** The condition governing the step's visibility, or null when it is always visible. */
  visibilityCondition?: ConditionSnapshot | null;
  groups: GroupSnapshot[];
}

export interface InstallSnapshot {
  steps: StepSnapshot[];
  /** The flag values in effect when the install completed. */
  flags: FlagSnapshot[];
}
