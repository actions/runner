# VSTS YAML deserialization

This document describes the details of the YAML deserialization process. This is not a "getting started" document.

Several expansion mechanisms are available during the deserialization process. The goals are:
1. Enable process reuse (maintainability)
1. Enable a simple getting-started experience

The deserialzation process occurs when a definition is triggered (manual, CI, or scheduled). All expansion mechanisms discussed in this document, occur during the deserialization process. In this sense, all mechanisms discussed here are "static". This document does not discuss dynamic mechanisms - i.e. adding additional jobs, after initial construction.

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

### Level inference

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

## Deserialization overview

The static expansion mechanisms can largely be separated into two categories: 1) mustache preprocessing and 2) templates.

Additionally, templates depend on mustache preprocessing as the mechanism for passing parameters into a template.

At a high level, the deserialization process is:

1. Preprocess file (mustache)
1. Deserialize yaml
1. If structure references a template
 1. Preprocess template (mustache)
 1. Deserialize yaml
 1. If structure references a template, recursively repeat 3.i
 1. Merge structure into caller structure

## Mustache

Each YAML file that is loaded is run through mustache preprocessing.

### Mustache escaping rules

Properties referenced using `{{property}}` will be JSON-string-escaped. Escaping can be omitted by using the triple-brace syntax: `{{{property}}}`.

### Server generated mustache context

When a definition is triggered (manual, CI, or scheduled), information about the event will be available as the mustache context object. The available data will be similar to the \"variables\" that are available server side today when a build is queued.

### User defined mustache context

The server generated context will be overlaid onto optional user defined context. *Yaml front matter* is a common technique to define metadata at the beginning of a document. Using YAML front matter, user defined context can be defined in a separate section, distinguished by a starting line `---` and ending line `---`.

Example YAML front matter:

```yaml
---
matrix:
 - buildConfiguration: debug
   buildPlatform: any cpu
 - buildConfiguration: release
   buildPlatform: any cpu
---
jobs:
  {{#each matrix}}
  - name: build-{{buildConfiguration}}-{{buildPlatform}}}
    - task: VSBuild@1.*
      inputs:
        solution: "**/*.sln"
        configuration: "{{buildConfiguration}}"
        platform: "{{buildPlatform}}"
  {{/each}}
```

## Templates

Templates enable portions of a process to be imported from other files.

### Template parameters

When a template is referenced, the caller may pass parameters to the template. The parameters are overlaid onto any user defined context in the target file (YAML front matter). The overlaid object is used as the mustache context during template deserialization.

Default parameter values can be specified in the template's YAML front matter. Since the caller-defined parameter values are overlaid on top, any parameters that are not specified will not be overridden.

TODO: What about the server generated context? Should that always be available in the mustache context during template deserialization without the caller explicitly passing it in? Should all outer root context?

TODO: EXAMPLES

### Template granularity

Templates may be used to define an entire process, or may be used to pull in smaller pieces.

The following types of templates are supported:
- entire process
- array of phases
- array of jobs
- array of variables
- array of steps

TODO: MORE DETAILS ABOUT HOW ARRAYS ARE PULLED IN, MULTIPLE ARRAYS CAN BE PULLED INTO SINGLE OUTER ARRAY

TODO: EXAMPLES

### Template chaining

Templates may reference other templates, but only at lower level objects in the hierarchy.

For example, a process template can reference a phases template. A process template cannot reference another process template.

### TODO: Discuss overrides and selectors

## Run local

A goal of the agent is to support "run-local" mode for testing YAML configuration. When running local, all agent calls to the VSTS server are stubbed (and server URL is hardcoded to 127.0.0.1).

When running local, the YAML will be converted into a pipeline, and worker processes invoked for each job.

A definition variable `Agent.RunMode`=`Local` is added to each job.

Note, this is not fully implemented yet. Only task steps are supported. Sync-sources and resource-import/export are not supported yet. Each job is run with syncSources=false.

Example:
```
~/vsts-agent/_layout/bin/Agent.Listener --whatif --yaml ~/vsts-agent/docs/preview/yaml/cmdline.yaml
```

### What-if mode

A "what-if" mode is supported for debugging the YAML static expansion and deserialization process. What-if mode dumps the constructed pipeline to the console, and exits.

Example:
```
~/vsts-agent/_layout/bin/Agent.Listener --whatif --yaml ~/vsts-agent/docs/preview/yaml/vsbuild.yaml
```

### Task version resolution and caching

In run-local mode, all referenced tasks must either be pre-cached under \_work/\_tasks, or optionally credentials can be supplied to query and download each referenced task from VSTS/TFS.

VSTS example:
```
~/vsts-agent/_layout/bin/Agent.Listener --url https://contoso.visualstudio.com --auth pat --token <TOKEN> --yaml ~/vsts-agent/docs/preview/yaml/cmdline.yaml
```

TFS example (defaults to integrated):
```
~/vsts-agent/_layout/bin/Agent.Listener --url http://localhost:8080/tfs --yaml ~/vsts-agent/docs/preview/yaml/cmdline.yaml
```

TFS example (negotiate, refer `--help` for all auth options):
```
~/vsts-agent/_layout/bin/Agent.Listener --url http://localhost:8080/tfs --auth negotiate --username <USERNAME> --password <PASSWORD> --yaml ~/vsts-agent/docs/preview/yaml/cmdline.yaml
```

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
