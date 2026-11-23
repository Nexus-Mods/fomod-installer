import { InstallSnapshot } from './InstallSnapshot';

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
   * that does not know it ignores it. Null or absent for installs with no FOMOD script, and
   * for script types that do not produce an option tree (e.g. C# scripts).
   */
  optionSnapshot?: InstallSnapshot | null;
}
