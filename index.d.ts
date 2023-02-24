/// <reference types="node" />

import { ChildProcess } from 'child_process';

declare module "fomod-installer" {
  /**
   * start the peer server
   * @param usePipe if set, use a pipe for communication, otherwise a network socket is used
   * @param id the id of the connection. in case of a pipe, this is the name of the pipe, otherwise it's the port of the socket
   * @param onExit callback when the process ends
   * @param onStdOut callback for console output (stdout and stderr)
   * @param containerName name of the container. Only relevant on windows 8+. If set, the process will run in an app container
   */
  export function createIPC(usePipe: boolean, id: string, onExit: (code: number) => void, onStdOut: (msg: string) => void, containerName: string, lowIntegrityProcess: boolean): Promise<number>;
}

