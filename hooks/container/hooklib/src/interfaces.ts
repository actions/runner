export enum Command {
  PrepareJob = 'prepare_job',
  CleanupJob = 'cleanup_job',
  RunContainerStep = 'run_container_step',
  RunScriptStep = 'run_script_step'
}

export interface HookData {
  command: Command
  responseFile: string
  args?: PrepareJobArgs | RunContainerStepArgs | RunScriptStepArgs
  state?: object
}

export interface PrepareJobArgs {
  container?: JobContainerInfo
  services?: ServiceContainerInfo[]
}

export type RunContainerStepArgs = StepContainerInfo

export interface RunScriptStepArgs {
  entrypoint: string
  entrypointArgs: string[]
  environmentVariables?: {[key: string]: string}
  prependPath?: string[]
  workingDirectory: string
}

export interface ContainerInfo {
  entrypoint?: string
  entrypointArgs?: string[]
  createOptions?: string
  environmentVariables?: {[key: string]: string}
  userMountVolumes?: Mount[]
  registry?: Registry
  portMappings?: string[]
}

export interface ServiceContainerInfo extends ContainerInfo {
  contextName: string
  image: string
}

export interface JobContainerInfo extends ContainerInfo {
  image: string
  workingDirectory: string
  systemMountVolumes: Mount[]
}

export interface StepContainerInfo extends ContainerInfo {
  prependPath?: string[]
  workingDirectory: string
  dockerfile?: string
  image?: string
  systemMountVolumes: Mount[]
}

export interface Mount {
  sourceVolumePath: string
  targetVolumePath: string
  readOnly: boolean
}

export interface Registry {
  username?: string
  password?: string
  serverUrl: string
}

export enum Protocol {
  TCP = 'tcp',
  UDP = 'udp'
}

export interface PrepareJobResponse {
  state?: object
  context?: ContainerContext
  services?: {[key: string]: ContainerContext}
  alpine: boolean
}

export interface ContainerContext {
  id?: string
  network?: string
  ports?: {[key: string]: string}
}
