# YAML getting started - Scripts (internal only, public preview soon)

## Simple script

Run a command line script using cmd.exe on Windows and bash on macOS and Linux.

```yaml
steps:
- script: echo hello world
  name: Simple script
```

The script contents are embedded in a temporary file and cleaned up after your script runs. On Windows a .cmd file is created. On macOS and Linux a .sh file is created.

## Multi-line script

Because the contents you specify are embedded in a script, you can write multiple lines.

```yaml
steps:
- script: |
    echo hello from a...
    echo ...multi-line script
  name: Multi-line script
```

## Working directory

You can specify a different working directory where your script is invoked. Otherwise the default is $(system.defaultWorkingDirectory).

```yaml
steps:
- script: echo agent.homeDirectory is %CD%
  name: Working directory
  workingDirectory: $(agent.homeDirectory)
  condition: and(succeeded(), eq(variables['agent.os'], 'windows_nt'))
- script: echo agent.homeDirectory is $PWD
  name: Working directory
  workingDirectory: $(agent.homeDirectory)
  condition: and(succeeded(), in(variables['agent.os'], 'darwin', 'linux'))
```

## Fail on STDERR

By default, text written to stderr does not fail your task. Programs commonly write progress
or other non-error information to stderr. Use the failOnStderr property to fail your task if
text is written to stderr.

```yaml
steps:
- script: echo hello from stderr 1>&2
  name: Fail on stderr
  failOnStderr: true
```

## Environment variables

Use env to map secrets variables into the environment for your script. Unless explicitly mapped,
secret variables are not propagated to the environment for ad hoc scripts.

```yaml
steps:
# First, create a secret variable. Normally these would be persisted securely by the definition.
- script: "echo ##vso[task.setvariable variable=MySecret;isSecret=true]My secret value"
  name: Create secret variable
  condition: and(succeeded(), eq(variables['agent.os'], 'windows_nt'))
- script: "echo \"##vso[task.setvariable variable=MySecret;isSecret=true]My secret value\""
  name: Create secret variable
  condition: and(succeeded(), in(variables['agent.os'], 'darwin', 'linux'))

# Next, map the secret into an environment variable and print it. Note, secrets are masked in the log
# and appear as '********'.
- script: echo The password is %MyPassword%
  name: Print secret variable
  env:
    MyPassword: $(MySecret)
  condition: and(succeeded(), eq(variables['agent.os'], 'windows_nt'))
- script: echo The password is $MyPassword
  name: Print secret variable
  env:
    MyPassword: $(MySecret)
  condition: and(succeeded(), in(variables['agent.os'], 'darwin', 'linux'))
```

## Control inputs

All task built-in inputs can be set as well (`continueOnError`, etc). Refer to the [tasks documentation](yamlgettingstarted-tasks.md) for details.
