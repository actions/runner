# YAML getting started - Multiple phases

## Phase dependencies

Multiple phases can be defined within a pipeline. The order in which the phases are executed, can be controlled by defining dependencies. The start of one phase, can depend on another phase completing. And a phase can have more than one dependency.

Phase dependencies enables four types of controls.

### Sequential phases

Example phases that execute sequentially.

```yaml
phases:
- phase: Debug
  steps:
  - script: echo hello from the Debug build

- phase: Release
  dependsOn: Debug # After Debug completes
  steps:
  - script: echo hello from the Release build
```

Example where an artifact is published in the first phase, and downloaded in the second phase:

```yaml
phases:
- phase: A
  steps:
  - script: echo hello > $(system.artifactsDirectory)/hello.txt
    displayName: Stage artifact

  - task: PublishBuildArtifacts@1
    displayName: Upload artifact
    inputs:
      pathtoPublish: $(system.artifactsDirectory)
      artifactName: hello
      artifactType: Container

- phase: B
  dependsOn: A
  steps:
  - task: DownloadBuildArtifacts@0
    displayName: Download artifact
    inputs:
      artifactName: hello

  - script: dir /s /b $(system.artifactsDirectory)
    displayName: List artifact (Windows)
    condition: and(succeeded(), eq(variables['agent.os'], 'Windows_NT'))

  - script: find $(system.artifactsDirectory)
    displayName: List artifact (macOS and Linux)
    condition: and(succeeded(), ne(variables['agent.os'], 'Windows_NT'))
```

## Parallel phases

Example phases that execute in parallel (no dependencies).

```yaml
phases:
- phase: Windows
  queue: Hosted VS2017
  steps:
  - script: echo hello from Windows

- phase: macOS
  queue: Hosted macOS Preview
  steps:
  - script: echo hello from macOS

- phase: Linux
  queue: Hosted Linux Preview
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

### Basic phase conditions

You can specify conditions under which phases will run. The following functions can be used to evaluate the result of dependent phases:

* **succeeded()** - Runs if all previous phases in the dependency graph completed with a result of Succeeded or SucceededWithIssues. Specific phase names may be specified as arguments.
* **failed()** - Runs if any previous phase in the dependency graph failed. Specific phases names may be specified as arguments.
* **succeededOrFailed()** - Runs if all previous phases in the dependency graph succeeded or any previous phase failed. Specific phase names may be specified as arguments.
<!-- * **canceled()** - Runs if the orchestration plan has been canceled. 
* **always()** - Runs always. -->

If no condition is explictly specified, a default condition of ```succeeded()``` will be used.

Example - Using the result functions in the expression:

```yaml
phases:
- phase: A
  steps:
  - script: exit 1

- phase: B
  dependsOn: A
  condition: failed()
  steps:
  - script: echo this will run when A fails

- phase: C
  dependsOn:
  - A
  - B
  condition: succeeded('B')
  steps:
  - script: echo this will run when B runs and succeeds
```

### Custom phase condition, with a variable

[Variables](https://docs.microsoft.com/en-us/vsts/build-release/concepts/definitions/build/variables) and all general functions of [task conditions](https://go.microsoft.com/fwlink/?linkid=842996) are also available in phase conditions.

Example - Using a variable in the expression:

```yaml
phases:
- phase: A
  steps:
  - script: echo hello

- phase: B
  dependsOn: A
  condition: and(succeeded(), eq(variables['build.sourceBranch'], 'refs/heads/master'))
  steps:
  - script: echo this only runs for master
```

### Custom phase condition, using an output variable

Output variables from previous phases can also be used within conditions.

Only phases which are referenced as direct dependencies are available for use.

Example - Using an output variable in the expression:

```yaml
phases:
- phase: A
  steps:
  - script: "echo ##vso[task.setvariable variable=skipsubsequent;isOutput=true]false"
    name: printvar

- phase: B
  condition: and(succeeded(), ne(dependencies.A.outputs['printvar.skipsubsequent'], 'true'))
  dependsOn: A
  steps:
  - script: echo hello from B
```

For details about output variables, refer [here](https://github.com/Microsoft/vsts-agent/blob/master/docs/preview/outputvariable.md#for-ad-hoc-script).

## Expression context

Phase-level expressions may use the following context:

* **variables** - all variables which are available in the root orchestration environment, including input variables, definition variables, linked variable groups, etc.
* **dependencies** - a property for each phase exists as the name of the phase.

Structure of the dependencies object:

```yaml
dependencies:
  <PHASE_NAME>:
    result: (Succeeded|SucceededWithIssues|Skipped|Failed|Canceled)
    outputs:
      variable1: value1
      variable2: value2
```
