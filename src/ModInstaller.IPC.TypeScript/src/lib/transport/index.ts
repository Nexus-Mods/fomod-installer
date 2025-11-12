/**
 * Transport module for FOMOD IPC communication
 * Provides pluggable transport mechanisms (Named Pipes, TCP)
 * with automatic fallback
 */

export { ITransport, TransportType, TransportError } from './ITransport';
export { TCPTransport } from './TCPTransport';
export { NamedPipeTransport } from './NamedPipeTransport';
