/// <reference types="node" />

import { ChildProcess } from 'child_process';

declare module "fomod-installer" {
  /**
   * start the peer server
   * @param usePipe if set, use a pipe for communication, otherwise a network socket is used
   * @param id the id of the connection. in case of a pipe, this is the name of the pipe, otherwise it's the port of the socket
   */
  export function createIPC(usePipe: boolean, id: string): Promise<ChildProcess>;
}

