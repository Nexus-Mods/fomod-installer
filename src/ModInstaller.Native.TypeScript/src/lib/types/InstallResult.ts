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
}