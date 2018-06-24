# YAML getting started - Scripts

## Simple script

Run a command line script using Bash on macOS and Linux and Command Prompt on Windows.

```yaml
steps:
- script: echo hello world
  displayName: Simple script
```

The script contents are embedded in a temporary file and cleaned up after your script runs. On macOS and Linux a .sh file is created. On Windows a .cmd file is created.

## Multi-line script

The following syntax can be used to specify a multi-line script:

```yaml
steps:
- script: |
    echo hello from a...
    echo ...multi-line script
  displayName: Multi-line script
```

## Working directory

You can specify a different working directory where your script is invoked. Otherwise the default is `$(system.defaultWorkingDirectory)`.

```yaml
steps:

# Script for macOS and Linux
- script: echo agent.homeDirectory is $PWD
  displayName: Working directory
  workingDirectory: $(agent.homeDirectory)
  condition: and(succeeded(), in(variables['agent.os'], 'darwin', 'linux'))

# Script for Windows
- script: echo agent.homeDirectory is %CD%
  displayName: Working directory
  workingDirectory: $(agent.homeDirectory)
  condition: and(succeeded(), eq(variables['agent.os'], 'windows_nt'))
```

## Fail on STDERR

By default, text written to stderr does not fail your task. Programs commonly write progress
or other non-error information to stderr. Use the failOnStderr property to fail your task if
text is written to stderr.

```yaml
steps:
- script: echo hello from stderr 1>&2
  displayName: Fail on stderr
  failOnStderr: true
```

## Environment variables

Use `env` to map secrets variables into the process environment block for your script. Otherwise secret variables are not mapped for ad hoc scripts.

```yaml
steps:

########################################
# macOS and Linux
########################################

# First, create a secret variable. Normally these would be persisted securely by the definition.
- script: "echo \"##vso[task.setvariable variable=MySecret;isSecret=true]My secret value\""
  displayName: Create secret variable
  condition: and(succeeded(), in(variables['agent.os'], 'darwin', 'linux'))

# Next, map the secret into an environment variable and print it. Note, secrets are masked in the log
# and appear as '********'.
- script: echo The password is $MyPassword
  displayName: Print secret variable
  env:
    MyPassword: $(MySecret)
  condition: and(succeeded(), in(variables['agent.os'], 'darwin', 'linux'))

########################################
# Windows
########################################

# First, create a secret variable. Normally these would be persisted securely by the definition.
- script: "echo ##vso[task.setvariable variable=MySecret;isSecret=true]My secret value"
  displayName: Create secret variable
  condition: and(succeeded(), eq(variables['agent.os'], 'windows_nt'))

# Next, map the secret into an environment variable and print it. Note, secrets are masked in the log
# and appear as '********'.
- script: echo The password is %MyPassword%
  displayName: Print secret variable
  env:
    MyPassword: $(MySecret)
  condition: and(succeeded(), eq(variables['agent.os'], 'windows_nt'))

```

## Control inputs

All task built-in inputs can be set as well (`continueOnError`, etc). Refer to the [tasks documentation](yamlgettingstarted-tasks.md) for details.
