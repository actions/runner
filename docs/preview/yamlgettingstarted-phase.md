# YAML getting started - Phase options (internal only, public preview soon)

## Continue on error

When `continueOnError` is set to true and the phase fails, the result will be \"Succeeded with issues\" instead of "Failed\".

## Enable access token

When `enableAccessToken` is set to true, script tasks (script, powershell, bash) will have access to the job OAuth token. The token can be passed to inputs using the macro `$(System.AccessToken)`, or accessed within a script via the environment variable `SYSTEM_ACCESSTOKEN`.

## Phase target

The phase `target` defines where the phase runs (an agent queue, a deployment group, or server side).

### Queue

The default phase target is a queue (the queue associated with the web definition).

Agent routing demands can also be specified on the target. For example:

```yaml
target:
  queue: myQueue
  demands: agent.os -eq Windows_NT
steps:
- script: echo hello world
```

Or multiple demands:

```yaml
target:
  queue: myQueue
  demands:
  - agent.os -eq Darwin
  - anotherCapability -eq somethingElse
steps:
- script: echo hello world
```

### Deployment group

```yaml
target:
  deploymentGroup: myDeploymentGroup
  healthOption: percentage
  percentage: 50
  tags: WebRole
steps:
- script: echo hello world
```

Or multiple tags:

```yaml
target:
  deploymentGroup: myDeploymentGroup
  tags:
  - WebRole
  - WorkerRole
steps:
- script: echo hello world
```

### Server

```yaml
target: server
steps:
- task: MyServerSideTask@1
```

## Phase execution

When a phase is started, it is dispatched as one or more jobs to it's target (agent queue, deployment group, or server). The phase `execution` section exposes settings specific to the individual jobs.

### Job timeout

The `timeoutInMinutes` allows a limit to be set for the job execution time. When not specified, the default is 60 minutes.

### Matrix

The `matrix` setting enables a phase to be queued multiple times, with different variable sets.

For example, a common scenario is to run the same build steps for varying permutations of architecture (x86/x64) and configuration (debug/release).

```yaml
execution:
  maxConcurrency: 2 # Don't consume more than 2 agents at a time. The default is 1.
  matrix:
    x64_debug:
      buildArch: x64
      buildConfig: debug
    x64_release:
      buildArch: x64
      buildConfig: release
    x86_release:
      buildArch: x86
      buildConfig: release
```

### Slicing

TODO

### Continue on error

When `continueOnError` is set to true and the job fails, the result will be \"Succeeded with issues\" instead of "Failed\".

## Variables

Variables can be specified on the phase. The variables can be passed to task inputs using the macro syntax `$(variableName)`, or can be accessed within a script using the environment variable.

```yaml
variables:
  mySimpleVar: simple var value
  "my.dotted.var": dotted var value
  "my var with spaces": var with spaces value

steps:
- script: echo Input macro = $(mySimpleVar). Env var = %MYSIMPLEVAR%
  condition: eq(variables['agent.os'], 'Windows_NT')

- script: echo Input macro = $(mySimpleVar). Env var = $MYSIMPLEVAR
  condition: in(variables['agent.os'], 'Darwin', 'Linux')

- bash: echo Input macro = $(my.dotted.var). Env var = $MY_DOTTED_VAR

- powershell: Write-Host "Input macro = $(my var with spaces). Env var = $env:MY_VAR_WITH_SPACES"
```