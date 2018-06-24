# YAML getting started - Variables

## Pipeline variables

Many predefined variables are available for use within pipelines.

For example, the variable `system.defaultWorkingDirectory` is the default working directory for scripts. And `agent.homeDirectory` is the directory where the agent is installed.

## Macro syntax

Within pipline inputs, variables can be referenced using macro syntax.

For example:

```yaml
steps:

# macOS and Linux
- script: ls
  workingDirectory: $(agent.homeDirectory)
  condition: and(succeeded(), in(variables['agent.os'], 'darwin', 'linux'))

# Windows
- script: dir
  workingDirectory: $(agent.homeDirectory)
  condition: and(succeeded(), eq(variables['agent.os'], 'windows_nt'))
```

## Process environment block

Within running scripts, variables are available as process environment variables.

When pipeline variables are set as process environment variables for scripts, the variable name is transformed to upper case and the characters \".\" and \" " are replaced with \"_\".

For example:

```yaml
steps:

# macOS and Linux
- script: |
    cd $AGENT_HOMEDIRECTORY
    ls
  condition: and(succeeded(), in(variables['agent.os'], 'darwin', 'linux'))

# Windows
- script: |
    cd %AGENT_HOMEDIRECTORY%
    dir
  condition: and(succeeded(), eq(variables['agent.os'], 'windows_nt'))
```

For a full list of variables, you can dump the environment variables from a script.

```yaml
steps:

# macOS and Linux
- script: printenv | sort
  condition: and(succeeded(), in(variables['agent.os'], 'darwin', 'linux'))

# Windows
- script: set
  condition: and(succeeded(), eq(variables['agent.os'], 'windows_nt'))
```
