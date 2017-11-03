# YAML getting started - Multiple phases (internal only, public preview soon)

## Phase dependencies

Multiple phases can be defined in a process. The order in which phases are started, can be controlled by defining dependencies. The start of one phase, can depend on another phase completing. And phases can have more than one dependency.

Phase dependencies enables four types of controls.

### Sequential phases

Example phases that build sequentially.

```yaml
phases:
- phase: Debug
  steps:
  - script: echo hello from the Debug build
- phase: Release
  dependsOn: Debug
  steps:
  - script: echo hello from the Release build
```

## Parallel phases

Example phases that build in parallel (no dependencies).

```yaml
phases:
- phase: Windows
  target:
    demands: agent.os -eq Windows_NT
  steps:
  - script: echo hello from Windows
- phase: macOS
  target:
    demands: agent.os -eq Darwin
  steps:
  - script: echo hello from macOS
- phase: Linux
  target:
    demands: agent.os -eq Linux
  steps:
  - script: echo hello from Linux
```

## Fan out

Example fan out

```yaml
phases:
- phase: InitialPhase
  steps:
  - script: echo hello from initial phase
- phase: SubsequentA
  dependsOn: InitialPhase
  steps:
  - script: echo hello from subsequent A
- phase: SubsequentB
  dependsOn: InitialPhase
  steps:
  - script: echo hello from subsequent B
```

## Fan in

```yaml
phases:
- phase: InitialA
  steps:
  - script: echo hello from initial A
- phase: InitialB
  steps:
  - script: echo hello from initial B
- phase: Subsequent
  dependsOn:
  - InitialA
  - InitialB
  steps:
  - script: echo hello from subsequent
```

## Phase conditions

You can specify conditions under which the phase will run. All general functions of [task conditions](https://go.microsoft.com/fwlink/?linkid=842996) are available in phase conditions. Phase conditions may make use of the following context:

* **variables** - all variables which are available in the root orchestration environment, including input variables, definition variables, linked variable groups, etc.
* **dependencies** - a property for each phase exists as the name of the phase. For instance, using the **Fan in** example from above, the phase Subsequent would have the following dependencies (the output variables are a Dictionary(string, string) and the result can have one of the listed values):

```yaml
dependencies:
  InitialA:
    result: (Succeeded|SucceededWithIssues|Skipped|Failed|Canceled)
    outputs:
      variable1: value1
      variable2: value2
  InitialB:
    result: (Succeeded|SucceededWithIssues|Skipped|Failed|Canceled)
    outputs:
      variable1: value1
      variable2: value2
```

The following functions are available:

* **succeeded()** - Runs if all previous phases in the dependency graph completed with a result of Succeeded or SucceededWithIssues. Specific phase names may be specified as arguments.
* **failed()** - Runs if any previous phase in the dependency graph failed. Specific phases names may be specified as arguments.
* **succeededOrFailed()** - Runs if all previous phases in the dependency graph succeeded or any previous phase failed. Specific phase names may be specified as arguments.
* **canceled()** - Runs if the orchestration plan has been canceled. 
* **always()** - Runs always.

If no condition is explictly specified, a default condition of ```succeeded()``` will be used.

An example condition may look like the following, assuming ```InitialA``` provides an output named ```skipsubsequent```. Only phases which are referenced as direct dependencies are available for context in conditions.

```yaml
phases:
- phase: InitialA
  steps:
  - script: echo hello from initial A
- phase: InitialB
  steps:
  - script: echo hello from initial B
- phase: Subsequent
  condition: or(succeeded(), not(eq(dependencies.InitialA.outputs.cmdline.skipsubsequent, 'true')))
  dependsOn:
  - InitialA
  - InitialB
  steps:
  - script: echo hello from subsequent
```

For details about output variables, refer [here](https://github.com/Microsoft/vsts-agent/blob/master/docs/preview/outputvariable.md#for-ad-hoc-script).
