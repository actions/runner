# YAML getting started - YAML schema

## Structural overview

At a high level, the structure of the YAML document is:

```
└───phases
    ├───phase
    │   ├───queue|deployment|server
    │   ├───variables
    │   └───steps
    │       ├───step # e.g. script: echo hello world
    │       └───[...]
    │
    └───[...]
```

## Level inference

In the spirit of simplicity, the YAML file is not required to define the full structural hierarchy.

For example, a very simple pipeline may only define steps (one phase implied):

```yaml
steps:
- script: echo hello world from script 1
- script: echo hello world from script 2
```

In short, at the top of the file, properties for a single phase can be specified without defining an array of phases.

<!-- Commenting-out template schema for now
The inference rules apply to templates as well. For details, see the schema reference section below.
-->

## Schema reference

All YAML definitions start with \"pipeline\" schema.

### Pipeline schema

#### pipeline

```yaml
# pipeline properties
name: string
phases: [ phase ]

# phase properties - not allowed when "phases" is defined
dependsOn: string | [ string ]
condition: string
continueOnError: true | false
queue: string | queueTarget
deployment: string | deploymentTarget
server: true | serverTarget
variables: { string: string }
steps: [ script | powershell | bash | task | checkout ]
```

<!-- #### repoResource

```yaml
repo: string # e.g. repo: self
clean: true | false
fetchDepth: number
lfs: true | false
``` -->

<!-- Commenting-out template schema for now
#### processTemplateReference

```yaml
name: string # relative path to process template
parameters: { string: any }
phases: [ # phase specific step overrides
  {
    name: string
    jobs: [ # phase and job specific step overrides
      {
        name: string
        steps: { string: [ script | powershell | bash | task ] }
      }
    ]
    steps: { string: [ script | powershell | bash | task ] }
  }
]
jobs: [ # job specific step overrides
  {
    name: string
    steps: { string: [ script | powershell | bash | task ] }
  }
]
steps: { string: [ script | powershell | bash | task ] } # step overrides
```

#### processTemplate

```yaml
resources: [ resource ]
phases: [ phase | phasesTemplateReference ]
jobs: [ job | jobsTemplateReference ]
steps: [ script | powershell | bash | task | stepsPhase | stepsTemplateReference ]
```
-->

### Phase schema

#### phase

```yaml
displayName: string
name: string
dependsOn: string | [ string ]
condition: string
continueOnError: true | false
queue: string | queueTarget
deployment: string | deploymentTarget
server: true | serverTarget
variables: { string: string }
steps: [ script | powershell | bash | task | checkout ]
```

#### queueTarget

```yaml
name: string
continueOnError: true | false
parallel: number
timeoutInMinutes: number
cancelTimeoutInMinutes: number
demands: string | [ string ]
matrix: { string: { string: string } }
```

#### deploymentTarget

```yaml
group: string
continueOnError: true | false
healthOption: string
percentage: string
timeoutInMinutes: number
cancelTimeoutInMinutes: number
tags: string | [ string ]
```

#### serverTarget

```yaml
continueOnError: true | false
parallel: number
timeoutInMinutes: number
cancelTimeoutInMinutes: number
matrix: { string: { string: string } }
```

<!-- Commenting-out template schema for now
#### phasesTemplateReference

```yaml
template: string # relative path
parameters: { string: any }
phases: [ # phase specific step overrides
  {
    name: string
    jobs: [ # phase and job specific step overrides
      {
        name: string
        steps: { string: [ script | powershell | bash | task ] }
      }
    ]
    steps: { string: [ script | powershell | bash | task ] }
  }
]
jobs: [ # job specific step overrides
  {
    name: string
    steps: { string: [ script | powershell | bash | task ] }
  }
]
steps: { string: [ script | powershell | bash | task ] } # step overrides
```

#### phasesTemplate

```yaml
phases: [ phase ]
jobs: [ job | jobsTemplateReference ]
steps: [ script | powershell | bash | task | stepsPhase | stepsTemplateReference ]
```
-->

<!-- Commenting-out variable object syntax for now
#### variable

```yaml
name: string
value: string
verbatim: bool # instructs agent not to uppercase/etc when setting env var
```
-->

<!-- Commenting-out template schema for now
#### variablesTemplateReference

```yaml
template: string # relative path
parameters: { string: any }
```

#### variablesTemplate

```yaml
variables: [ variable ]
```
-->

### Step schema

#### script

```yaml
script: string
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

#### powershell

```yaml
powershell: string
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

#### bash

```yaml
bash: string
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

#### task

```yaml
task: string # task reference, e.g. "VSBuild@1"
displayName: string
name: string
condition: string
continueOnError: true | false
enabled: true | false
timeoutInMinutes: number
inputs: { string: string }
env: { string: string }
```

#### checkout

```yaml
checkout: self # self represents the repo associated with the entry .yml file
clean: true | false
fetchDepth: number
lfs: true | false
```

OR

```yaml
checkout: none # skips checkout
```

<!-- Commenting-out step group for now
#### stepGroup

```yaml
phase: string # name
steps: [ script | powershell | bash | task ]
```
-->

<!-- Commenting-out template schema for now since it's main value comes with templates and docker
#### stepsTemplateReference

```yaml
template: string # relative path
parameters: { string: any }
steps: { string: [ script | powershell | bash | task ] } # step overrides
```

#### stepsTemplate

```yaml
steps: [ script | powershell | bash | task | stepsPhase ]
```
-->
