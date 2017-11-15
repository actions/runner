# YAML getting started - PowerShell scripts

## Simple script

Run a PowerShell script on Windows, macOS, and Linux. `powershell` must be in your PATH.

```yaml
steps:
- powershell: Write-Host "Hello from PowerShell v$($PSVersionTable.PSVersion.Major)"
  displayName: Simple script
```

The script contents are embedded in a temporary .ps1 file and cleaned up after your script runs.

## Multi-line script

Because the contents you specify are embedded in a script, you can write multiple lines.

```yaml
steps:
- powershell: |
    Write-Host "Hello from a..."
    Write-Host "...multi-line PowerShell script"
  displayName: Multi-line script
```

## Error action preference

Unless specified, the task defaults the error action preference to Stop. The line
`$ErrorActionPreference = '<VALUE>'` is prepended to the top of your script.

When the error action preference is set to `stop`, errors will be treated as terminating and
powershell will return a non-zero exit code (task result will be Failed).

Valid values are: `stop`, `continue`, `silentlyContinue`

```yaml
steps:
- powershell: |
    Write-Error 'Uh oh, an error occurred'
    Write-Host 'Trying again...'
  displayName: Error action preference
  errorActionPreference: continue
```

## Fail on STDERR

If true, the task will fail if any errors are written to the error pipeline, or if any data
is written to the Standard Error stream. Otherwise the task will rely on the exit code to
determine failure.

```yaml
steps:
- powershell: Write-Error 'Uh oh, an error occurred'
  displayName: Fail on stderr
  errorActionPreference: Continue
  failOnStderr: true
```

## Ignore $LASTEXITCODE

If false, the line `if ((Test-Path -LiteralPath variable:\LASTEXITCODE)) { exit $LASTEXITCODE }`
is appended to the end of your script. This will cause the last exit code from an external command
to be propagated as the exit code of powershell. Otherwise the line is not appended to the end of your script.

```yaml
steps:
- powershell: git nosuchcommand
  displayName: Ignore last exit code
  ignoreLASTEXITCODE: true
```

## Working directory

You can specify a different working directory where your script is invoked. Otherwise the default is $(system.defaultWorkingDirectory).

```yaml
steps:
- powershell: |
    Write-Host "agent.homeDirectory is:"
    Get-Location
  displayName: Working directory
  workingDirectory: $(agent.homeDirectory)
```

## Environment variables

Use env to map secrets variables into the environment for your script. Unless explicitly mapped,
secret variables are not propagated to the environment for ad hoc scripts.

```yaml
steps:
# First, create a secret variable. Normally these would be persisted securely by the definition.
- powershell: "Write-Host '##vso[task.setvariable variable=MySecret;isSecret=true]My secret value'"
  displayName: Create secret variable

# Next, map the secret into an environment variable and print it. Note, secrets are masked in the log
# and appear as '********'.
- powershell: Write-Host "The password is $env:MyPassword"
  displayName: Print secret variable
  env:
    MyPassword: $(MySecret)
```

## Control inputs

All task built-in inputs can be set as well (`continueOnError`, etc). Refer to the [tasks documentation](yamlgettingstarted-tasks.md) for details.
