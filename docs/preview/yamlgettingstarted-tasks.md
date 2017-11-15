# YAML getting started - Tasks

## Syntax

The syntax to reference a task is:

```yaml
steps:
- task: string@number # The task name and version. For example: Npm@1. The version must indicate the
                      # major-version number only.

  displayName: string

  name: string

  inputs: { string: string } # Map of task inputs. Refer to the task.json. TODO export to YAML.

  enabled: true | false

  continueOnError: true | false

  condition: string # Defaults to succeeded(). https://go.microsoft.com/fwlink/?linkid=842996

  timeoutInMinutes: number # Whole numbers only. Zero indicates no timeout.

  env: { string: string } # Mapping of additional environment variables to set for the scope of the task.
```

## Example

A simple build definition may look like this:

```yaml
steps:
- task: Npm@1
  displayName: npm install
  inputs:
    command: install
- task: Npm@1
  displayName: npm test
  inputs:
    command: test
```

## Resiliency

For resiliency when using extension tasks, instead of the task name you can use
`<CONTRIBUTION_IDENTIFIER>.<NAME>` to avoid collisions on the name. Otherwise
if the name collides, in-the-box tasks take precedence.

Alternatively, the task ID (a GUID) can be used instead of the task name.

The detailed task metadata (contribution identifier and GUID) can be found from:
`https://<YOUR_ACCOUNT>.visualstudio.com/_apis/distributedtask/tasks`

## Export to YAML

Coming soon.
