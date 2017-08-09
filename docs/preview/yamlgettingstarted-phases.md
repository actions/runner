# YAML getting started - Multiple phases (internal only, public preview soon)

## Phase dependencies

Multiple phases can be defined in a process. The order in which phases are started, can be controlled by defining dependencies. The start of one phase, can depend on another phase completing. And phases can have more than one dependency.

Phase dependencies enables four types of controls.

### Sequential phases

Example phases that build sequentially.

```yaml
phases:
- name: Debug
  steps:
  - script: echo hello from the Debug build
- name: Release
  dependsOn: Debug
  steps:
  - script: echo hello from the Release build
```

## Parallel phases

Example phases that build in parallel (no dependencies).

```yaml
phases:
- name: Windows
  target:
    demands: agent.os -eq Windows_NT
  steps:
  - script: echo hello from Windows
- name: macOS
  target:
    demands: agent.os -eq Darwin
  steps:
  - script: echo hello from macOS
- name: Linux
  target:
    demands: agent.os -eq Linux
  steps:
  - script: echo hello from Linux
```

## Fan out

Example fan out

```yaml
phases:
- name: InitialPhase
  steps:
  - script: echo hello from initial phase
- name: SubsequentA
  dependsOn: InitialPhase
  steps:
  - script: echo hello from subsequent A
- name: SubsequentB
  dependsOn: InitialPhase
  steps:
  - script: echo hello from subsequent B
```

## Fan in

```yaml
phases:
- name: InitialA
  steps:
  - script: echo hello from initial A
- name: InitialB
  steps:
  - script: echo hello from initial B
- name: Subsequent
  dependsOn:
  - InitialA
  - InitialB
  steps:
  - script: echo hello from subsequent
```

## Phase conditions

TODO
