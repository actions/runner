# YAML getting started - Bash scripts (internal only, public preview soon)

## Simple script

Run a Bash script on macOS, Linux, and Windows. `bash` must be in your PATH.

```yaml
steps:
- bash: echo hello world
  displayName: Simple script
```

The script contents are embedded in a temporary .sh file and cleaned up after your script runs.

## Multi-line script

Because the contents you specify are embedded in a script, you can write multiple lines.

```yaml
steps:
- bash: |
    echo hello from a...
    echo ...multi-line script
  displayName: Multi-line script
```

## Working directory

You can specify a different working directory where your script is invoked. Otherwise the default is $(system.defaultWorkingDirectory).

```yaml
steps:
- bash: echo agent.homeDirectory is $PWD
  displayName: Working directory
  workingDirectory: $(agent.homeDirectory)
```

## Fail on STDERR

By default, text written to stderr does not fail your task. Programs commonly write progress
or other non-error information to stderr. Use the failOnStderr property to fail your task if
text is written to stderr.

```yaml
steps:
- bash: echo hello from stderr 1>&2
  displayName: Fail on stderr
  failOnStderr: true
```

## Environment variables

Use env to map secrets variables into the environment for your script. Unless explicitly mapped,
secret variables are not propagated to the environment for ad hoc scripts.

```yaml
steps:
# First, create a secret variable. Normally these would be persisted securely by the definition.
- bash: "echo \"##vso[task.setvariable variable=MySecret;isSecret=true]My secret value\""
  displayName: Create secret variable

# Next, map the secret into an environment variable and print it. Note, secrets are masked in the log
# and appear as '********'.
- bash: echo The password is $MyPassword
  displayName: Print secret variable
  env:
    MyPassword: $(MySecret)
```

## Control inputs

All task built-in inputs can be set as well (`continueOnError`, etc). Refer to the [tasks documentation](yamlgettingstarted-tasks.md) for details.
