/// <reference types="node" />

import { ChildProcess } from 'child_process';

declare module "fomod-installer" {
  export function createIPC(port: number | string): Promise<ChildProcess>;
}

