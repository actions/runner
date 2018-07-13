# YAML getting started - Schema reference

## Structural overview

At a high level, the structure of the YAML document is:

```
└───phases
    ├───phase
    │   ├───queue|server
    │   ├───variables
    │   └───steps
    │       ├───step # e.g. script: echo hello world
    │       └───[...]
    │
    └───[...]
```

## Level inference

In the spirit of simplicity, the YAML file is not required to define the schema hierarchy.

For example, a very simple pipeline may only define steps (one phase implied):

```yaml
steps:
- script: echo hello world from script 1
- script: echo hello world from script 2
```

In short, at the top of the file, properties for a single phase can be specified without defining an array of phases.

## Schema reference

All YAML definitions start with the \"pipeline\" schema.

### pipeline

```yaml
name: string
resources:
  containers: [ container ]
  repositories: [ repository ]
variables: { string: string } | variable
phases: [ phase | templateReference ]
```

### phase

```yaml
- phase: string # name
  displayName: string
  dependsOn: string | [ string ]
  condition: string
  continueOnError: true | false
  queue: string | queue
  server: true | server
  variables: { string: string } | [ variable ]
  steps: [ script | bash | powershell | checkout | task | templateReference ]
```

### script

```yaml
- script: string
  displayName: string
  name: string
  workingDirectory: string
  failOnStderr: true | false
  condition: string
  continueOnError: true | false
  enabled: true | false
  timeoutInMinutes: number
  env: { string: string }
```

### bash

```yaml
- bash: string
  displayName: string
  name: string
  workingDirectory: string
  failOnStderr: true | false
  condition: string
  continueOnError: true | false
  enabled: true | false
  timeoutInMinutes: number
  env: { string: string }
```

### powershell

```yaml
- powershell: string
  displayName: string
  name: string
  errorActionPreference: stop | continue | silentlyContinue
  failOnStderr: true | false
  ignoreLASTEXITCODE: true | false
  workingDirectory: string
  condition: string
  continueOnError: true | false
  enabled: true | false
  timeoutInMinutes: number
  env: { string: string }
```

### checkout

```yaml
- checkout: self # self represents the repo associated with the entry .yml file
  clean: true | false
  fetchDepth: number
  lfs: true | false
```

OR

```yaml
- checkout: none # skips checkout
```

### task

```yaml
- task: string # task reference, e.g. "VSBuild@1"
  displayName: string
  name: string
  condition: string
  continueOnError: true | false
  enabled: true | false
  timeoutInMinutes: number
  inputs: { string: string }
  env: { string: string }
```

### queue

```yaml
name: string
demands: string | [ string ] # Supported by private pools
timeoutInMinutes: number
cancelTimeoutInMinutes: number
parallel: number
matrix: { string: { string: string } }
```

### server

```yaml
timeoutInMinutes: number
cancelTimeoutInMinutes: number
parallel: number
matrix: { string: { string: string } }
```

### templateReference

```yaml
- template: string
  parameters: { string: any }
```
