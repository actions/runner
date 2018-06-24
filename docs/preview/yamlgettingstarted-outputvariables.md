# YAML getting started - Output variables

Output variables can be used to set a variable in one phase, and then use the variable in a downstream phase.

## Mapping an output variable

Output variables must be explicitly be mapped into downstream phases.

They are prefixed with the name of the step that set the variable.

Note, outputs can only be referenced from phases which listed as direct dependencies.

```yaml
phases:

# Set an output variable from phase A
- phase: A
  steps: 
  - script: "echo ##vso[task.setvariable variable=myOutputVar;isOutput=true]this is the value"
    name: setvar
  - script: echo $(setvar.myOutputVar)
    name: echovar

# Map the variable into phase B
- phase: B
  dependsOn: A
  variables:
    myVarFromPhaseA: $[ dependencies.A.outputs['setvar.myOutputVar'] ]
  steps:
  - script: "echo $(myVarFromPhaseA)"
    name: echovar
```

## Mapping an output variable from a matrix

```yaml
phases:

# Set an output variable from a phase with a matrix
- phase: A
  queue:
    parallel: 2
    matrix:
      debug:
        configuration: debug
        platform: x64
      release:
        configuration: release
        platform: x64
  steps:
  - script: "echo ##vso[task.setvariable variable=myOutputVar;isOutput=true]this is the $(configuration) value"
    name: setvar
  - script: echo $(setvar.myOutputVar)
    name: echovar

# Map the variable from the debug job
- phase: B
  dependsOn: A
  variables:
    myVarFromPhaseADebug: $[ dependencies.A.outputs['debug.setvar.myOutputVar'] ]
  steps:
  - script: "echo $(myVarFromPhaseADebug)"
    name: echovar
```

## Mapping an output variable from a slice

```yaml
phases:

# Set an output variable from a phase with slicing
- phase: A
  queue:
    parallel: 2 # Two slices
  steps:
  - script: "echo ##vso[task.setvariable variable=myOutputVar;isOutput=true]this is the slice $(system.jobPositionInPhase) value"
    name: setvar
  - script: echo $(setvar.myOutputVar)
    name: echovar

# Map the variable from the job for the first slice
- phase: B
  dependsOn: A
  variables:
    myVarFromPhaseA1: $[ dependencies.A.outputs['job1.setvar.myOutputVar'] ]
  steps:
  - script: "echo $(myVarFromPhaseA1)"
    name: echovar
```
