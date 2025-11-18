export * from './ModInstaller';
export * from './FileSystem';
export * from './Logger';
export * from './SupportedResult';
export * from './InstallResult';

import { IFileSystemExtension } from './FileSystem';
import { ILoggerExtension } from './Logger';
import { IModInstallerExtension } from './ModInstaller';

export type OrderType = 'AlphaAsc' | 'AlphaDesc' | 'Explicit';
export type GroupType = 'SelectAtLeastOne' | 'SelectAtMostOne' | 'SelectExactlyOne' | 'SelectAll' | 'SelectAny';
export type PluginType = 'Required' | 'Optional' | 'Recommended' | 'NotUsable' | 'CouldBeUsable';

export interface IPlugin {
    id: number;
    selected: boolean;
    preset: boolean;
    name: string;
    description: string;
    image: string;
    type: PluginType;
    conditionMsg?: string;
}

export interface IGroup {
    id: number;
    name: string;
    type: GroupType;
    options: IPlugin[];
}

export interface IGroupList {
    group: IGroup[];
    order: OrderType;
}

export interface IInstallStep {
    id: number;
    name: string;
    visible: boolean;
    optionalFileGroups?: IGroupList;
}

export interface IHeaderImage {
    path: string;
    showFade: boolean;
    height: number;
}

export type SelectCallback = (stepId: number, groupId: number, optionId: number[]) => void;
export type ContinueCallback = (forward: boolean, currentStepId: number) => void;
export type CancelCallback = () => void;

export interface IExtension extends IModInstallerExtension, IFileSystemExtension {
    allocWithOwnership(length: number): Buffer | null;
    allocWithoutOwnership(length: number): Buffer | null;
    allocAliveCount(): number;
}