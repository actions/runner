# YAML getting started - Phase options (internal only, public preview soon)

## Continue on error

When `continueOnError` is set to true and the phase fails, the result will be \"Succeeded with issues\" instead of "Failed\".

<!--
## Enable access token

When `enableAccessToken` is set to true, script tasks (script, powershell, bash) will have access to the job OAuth token. The token can be passed to inputs using the macro `$(System.AccessToken)`, or accessed within a script via the environment variable `SYSTEM_ACCESSTOKEN`.
-->

## Phase target

When a phase is started, it is dispatched as one or more jobs to it's target (agent `queue`, `deployment` group, or `server`). Each target section has settings that are either specific to the target type, or specific to the individual jobs that are dispatched.

When not defined, the target defaults to `queue`.

### Queue target

When only the queue name is specified, the simplified syntax can be used:

```yaml
queue: string
```

Otherwise the full syntax is:

```yaml
queue:
  name: string
  continueOnError: true | false
  parallel: number
  timeoutInMinutes: number
  cancelTimeoutInMinutes: number
  demands: string | [ string ]
  matrix: { string: { string: string } }
```

### Deployment target

Likewise the simplified deployment syntax is:

```yaml
deployment: string # group name
```

Full syntax:

```yaml
deployment:
  group: string
  continueOnError: true | false
  healthOption: string
  percentage: string
  timeoutInMinutes: number
  cancelTimeoutInMinutes: number
```

### Server target

Simplified server syntax:

```yaml
server: true
```

Full syntax:

```yaml
server:
  continueOnError: true | false
  parallel: number
  timeoutInMinutes: number
  cancelTimeoutInMinutes: number
  matrix: { string: { string: string } }
```

### Demands (applies to: queue)

Agent routing demands can be specified, which match against agent capabilities. For example:

```yaml
queue:
  name: myQueue
  demands: agent.os -equals Windows_NT
steps:
- script: echo hello world
```

Or multiple demands:

```yaml
queue:
  name: myQueue
  demands:
  - agent.os -equals Darwin
  - anotherCapability -equals somethingElse
steps:
- script: echo hello world
```

### Job timeout (applies to: queue, deployment, server)

The `timeoutInMinutes` allows a limit to be set for the job execution time. When not specified, the default is 60 minutes.

The `cancelTimeoutInMinutes` allows a limit to be set for the job cancel time. When not specified, the default is 5 minutes.

#### Matrix (applies to: queue, server)

The `matrix` setting enables a phase to be dispatched multiple times, with different variable sets.

For example, a common scenario is to run the same build steps for varying permutations of architecture (x86/x64) and configuration (debug/release).

```yaml
queue:
  parallel: 2 # Consume up to two agents at a time. The default is 1.
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

### Slicing (applies to: queue, server)

When `parallel` is specified and `matrix` is not defined, the setting indicates how many jobs to dispatch. Variables `System.SliceNumber` and `System.SliceCount` are added to each job. The variables can then be used within your scripts to divide work among the jobs.

### Continue on error (applies to: queue, deployment, server)

When `continueOnError` is `true` and the job fails, the result will be \"Succeeded with issues\" instead of "Failed\".

## Variables

Variables can be specified on the phase. The variables can be passed to task inputs using the macro syntax `$(variableName)`, or accessed within a script using the environment variable.

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
