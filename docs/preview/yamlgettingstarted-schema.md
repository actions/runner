# YAML getting started - YAML schema (internal only, public preview soon)

## Structural overview

The YAML document describes an entire process.

At a high level, the structure of a process is:

```
├───resources (endpoints, etc)
└───phases
    ├───phase
    │   ├───target # e.g. agent queue
    │   └───jobs
    │       ├───job # e.g. a build
    │       │   ├───variables
    │       │   └───steps
    │       │       ├───step # e.g. run msbuild
    │       │       └───[...]
    │       └───[...]
    └───[...]
```

## Level inference

In the spirit of simplicity, the YAML file is not required to define the full structural hierarchy.

For example, a very simple process may only define steps (one job implied, one phase implied):

```yaml
steps:
  - task: VSBuild@1.*
    inputs:
      solution: "**/*.sln"
      configuration: "Debug"
      platform: "Any CPU"
```

In short, the rules are:
- Where a process is defined, properties for a single phase can be specified without defining `phases -> phase`.
- Where a process is defined, properties for a single job can be specified without defining `phases -> phase -> jobs -> job`.
- Where a phase is defined, properties for a single job can be specified without defining `jobs -> job`.

The inference rules apply to templates as well. For details, see the schema reference section below.

## Schema reference

All YAML definitions start with an entry \"process\" file.

### Process structures

#### process

```yaml
# general properties
name: string

# process properties
resources: [ resource ]
template:  processTemplateReference
phases: [ phase | phasesTemplateReference ]

# phase properties - not allowed when higher level template or phases is defined
target: phaseTarget
jobs: [ job | jobsTemplateReference ]

# job properties - not allowed when higher level template, phases, or jobs is defined
timeout: string # e.g. "0.01:30:00" (1 hour and 30 minutes)
variables: { string: string }
steps: [ import | export | task | stepsPhase | stepsTemplateReference ]
```

#### resource

```yaml
name: string
type: string
data: { string: any }
```

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
        steps: { string: [ import | export | task ] }
      }
    ]
    steps: { string: [ import | export | task ] }
  }
]
jobs: [ # job specific step overrides
  {
    name: string
    steps: { string: [ import | export | task ] }
  }
]
steps: { string: [ import | export | task ] } # step overrides
```

#### processTemplate

```yaml
resources: [ resource ]
phases: [ phase | phasesTemplateReference ]
jobs: [ job | jobsTemplateReference ]
steps: [ import | export | task | stepsPhase | stepsTemplateReference ]
```

### Phase structures

#### phase

```yaml
# phase properties
phase: string # name
target: phaseTarget
jobs: [ job | jobsTemplateReference ]

# job properties
timeout: string # e.g. "0.01:30:00" (1 hour and 30 minutes)
variables: { string: string }
steps: [ import | export | task | stepsPhase | stepsTemplateReference ]
```

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
        steps: { string: [ import | export | task ] }
      }
    ]
    steps: { string: [ import | export | task ] }
  }
]
jobs: [ # job specific step overrides
  {
    name: string
    steps: { string: [ import | export | task ] }
  }
]
steps: { string: [ import | export | task ] } # step overrides
```

#### phasesTemplate

```yaml
phases: [ phase ]
jobs: [ job | jobsTemplateReference ]
steps: [ import | export | task | stepsPhase | stepsTemplateReference ]
```

### Job structures

#### job

```yaml
job: string # name
timeoutInMinutes: number
variables: [ variable | variablesTemplateReference ]
steps: [ import | export | task | stepsPhase | stepsTemplateReference ]
```

#### jobsTemplateReference

```yaml
template: string # relative path
parameters: { string: any }
jobs: [ # job specific step overrides
  {
    name: string
    steps: { string: [ import | export | task ] }
  }
]
steps: { string: [ import | export | task ] } # step overrides
```

#### jobsTemplate

```yaml
jobs: [ job ]
steps: [ import | export | task | stepsPhase | stepsTemplateReference ]
```

#### variable

```yaml
name: string
value: string
verbatim: bool # instructs agent not to uppercase/etc when setting env var
```

#### variablesTemplateReference

```yaml
template: string # relative path
parameters: { string: any }
```

#### variablesTemplate

```yaml
variables: [ variable ]
```

### Step structures

#### script

```yaml
script: string
name: string # display name
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
script: string
name: string # display name
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
script: string
name: string # display name
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
name: string  # display name
condition: string
continueOnError: true | false
enabled: true | false
timeoutInMinutes: number
inputs: { string: string }
env: { string: string }
```

#### stepsPhase

```yaml
phase: string # name
steps: [ import | export | task ]
```

#### stepsTemplateReference

```yaml
template: string # relative path
parameters: { string: any }
steps: { string: [ import | export | task ] } # step overrides
```

#### stepsTemplate

```yaml
steps: [ script | powershell | bash | task | stepsPhase ]
```
