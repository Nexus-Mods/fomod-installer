/**
 * Result types for the FOMOD installer IPC transport.
 *
 * The IPC transport returns the installer's result as a plain JSON object; these declarations
 * describe its shape.
 *
 * IMPORTANT - casing differs from the native package. The IPC server serialises with
 * Newtonsoft.Json and no naming policy (ModInstaller.IPC/Server.cs), so the payload is
 * PascalCase: `optionSnapshot` is lower-cased only because it is a dictionary key added in
 * ModInstaller.Adaptor.Dynamic/Installer.cs, while every field beneath it keeps its .NET name
 * (`Steps`, `Resolver`, `PluginPath`, ...). The native package camel-cases instead. Do not
 * copy declarations between the two packages.
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
 * A single flat shape with a `Kind` discriminator rather than a union of distinct types, so
 * that an older consumer meeting an unknown `Kind` can still read the rest of the record.
 */
export interface ConditionSnapshot {
  Kind: ConditionKind;

  /** For `Plugin`: the plugin path the condition tests. */
  PluginPath?: string | null;

  /** For `Plugin`: the required state. */
  PluginState?: PluginState | null;

  /** For `Flag`: the flag name the condition tests. */
  FlagName?: string | null;

  /**
   * For `Flag`: the required value. Null or empty means the condition is fulfilled when the
   * flag is unset or empty.
   */
  FlagValue?: string | null;

  /** For `Composite`: the operator combining the sub-conditions. */
  Operator?: ConditionOperator | null;

  /** For `Composite`: the sub-conditions, in evaluation order. */
  Conditions?: ConditionSnapshot[] | null;

  /** For `Environment`: which environment condition this is. */
  EnvironmentType?: EnvironmentType | null;

  /**
   * For `Environment`: the minimum version required, as declared. The version actually
   * observed at install time is NOT recorded, so an Environment leaf is undecidable - see
   * ResolverSnapshot.
   */
  MinimumVersion?: string | null;

  /** For `Environment`: the script extender identifier (fose, nvse, f4se, skse). */
  Extender?: string | null;

  /** The human-readable explanation this condition renders, when it renders one. */
  Message?: string | null;
}

/** One entry of a conditional resolver's pattern list. */
export interface PatternSnapshot {
  /** The option type this pattern yields when its condition is fulfilled. */
  Type: OptionType;
  /** The condition that must be fulfilled for this pattern to be elected. */
  Condition: ConditionSnapshot;
}

/**
 * The resolver that produced an option's type.
 *
 * Evaluation order for `Conditional`, matching the installer: walk `Patterns` in order and
 * return the `Type` of the first pattern whose `Condition` is fulfilled. If none is fulfilled,
 * return `DefaultType`.
 *
 * A condition may be undecidable from the snapshot alone - an `Environment` leaf, because the
 * version observed at install time is not recorded. Undecidability propagates through the tree,
 * and a pattern whose condition is undecidable makes the rest of the walk undecidable too,
 * because it may have been the first pattern to match. A consumer that reaches an undecidable
 * pattern must return the recorded `OptionSnapshot.Type` rather than continuing to the next
 * pattern or falling through to `DefaultType`.
 */
export interface ResolverSnapshot {
  Kind: ResolverKind;
  /** For `Static`: the fixed option type. */
  Type?: OptionType | null;
  /** For `Conditional`: the type returned when no pattern matches. */
  DefaultType?: OptionType | null;
  /** For `Conditional`: the patterns, in the order they are evaluated. */
  Patterns?: PatternSnapshot[] | null;
}

/** A flag an option sets when it is enabled. */
export interface FlagSnapshot {
  Name: string;
  Value: string;
}

export interface OptionSnapshot {
  Name: string;
  /** The option type the installer resolved for this option at the moment of install. */
  Type?: OptionType | null;
  /** Whether the option was selected in the install that produced this snapshot. */
  Selected: boolean;
  Resolver: ResolverSnapshot;
  /** The flags this option sets when enabled. */
  Flags: FlagSnapshot[];
}

export interface GroupSnapshot {
  Name: string;
  Type: string;
  Options: OptionSnapshot[];
}

export interface StepSnapshot {
  Id: number;
  Name: string;
  Visible: boolean;
  /** The condition governing the step's visibility, or null when it is always visible. */
  VisibilityCondition?: ConditionSnapshot | null;
  Groups: GroupSnapshot[];
}

export interface InstallSnapshot {
  Steps: StepSnapshot[];
  /** The flag values in effect when the install completed. */
  Flags: FlagSnapshot[];
}

export interface InstallInstruction {
  type: string;
  source: string;
  destination: string;
  section: string;
  key: string;
  value: string;
  data: Uint8Array;
  priority: string;
}

export interface InstallResult {
  message: string;
  instructions: InstallInstruction[];

  /**
   * A structured snapshot of every option the installer considered: the type each one
   * resolved to, the resolver that produced it, and the flags it sets.
   *
   * Additive and optional. A result from an older installer omits it entirely; a consumer
   * that does not know it ignores it.
   *
   * Note the lower-case key over a PascalCase payload - see the file header.
   */
  optionSnapshot?: InstallSnapshot | null;
}