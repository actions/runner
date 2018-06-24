# YAML getting started - Tasks

## Syntax

The syntax to reference a task is:

```yaml
steps:
- task: string@number # The task name and version. For example: Npm@1. The version must indicate the
                      # major-version number only.

  displayName: string

  name: string

  inputs: { string: string } # Map of task inputs. Refer to the task.json or use the View Yaml
                             # functionality on a designer definition.

  enabled: true | false

  continueOnError: true | false # On failure, flips the result to SucceededWithIssues

  condition: string # Defaults to succeeded(). https://go.microsoft.com/fwlink/?linkid=842996

  timeoutInMinutes: number # Whole numbers only. Zero indicates no timeout.

  env: { string: string } # Mapping of additional environment variables to set for the scope of the task.
```

## Example

A simple build definition may look like this:

```yaml
steps:

# npm install
- task: Npm@1
  displayName: npm install
  inputs:
    command: install

# npm test
- task: Npm@1
  displayName: npm publish
  inputs:
    command: publish
```

## Resiliency

For resiliency when using extension tasks, instead of the task name you can use
`<CONTRIBUTION_IDENTIFIER>.<NAME>` to avoid collisions on the name. Otherwise
if the name collides, in-the-box tasks take precedence.

Alternatively, the task ID (a GUID) can be used instead of the task name.

The contribution identifier and GUID can be found from:
`https://<YOUR_ACCOUNT>.visualstudio.com/_apis/distributedtask/tasks`

## Export to YAML

Designer definitions have a `View Yaml` link that will export the selected configuration to YAML.
