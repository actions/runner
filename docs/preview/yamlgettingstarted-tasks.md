# YAML getting started - Tasks (internal only, public preview soon)

The syntax to reference a task is:

```yaml
steps:
  - task: string@number # For example: Npm@1. The version must indicate the major-version component only.

    name: string # Display name

    refName: string # TODO

    inputs: { string: string } # Map of task inputs. Refer to the task.json. TODO export to YAML.

    enabled: true | false

    continueOnError: true | false

    condition: string # Defaults to succeeded(). TODO FWLINK HERE

    timeoutInMinutes: number # Whole numbers only. Zero indicates no timeout.

    env: { string: string } # Mapping of additional environment variables to set for the scope of the task.
```

A simple build definition may look like this:

```yaml
steps:
  - task: Npm@1
    name: npm install
    inputs:
      command: install
  - task: Npm@1
    name: npm test
    inputs:
      command: test
```
