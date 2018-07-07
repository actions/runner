# YAML getting started - Queues

The agent queue can be specified in the YAML file. For example,

```yaml
queue: Hosted VS2017
steps:
- script: echo hello world
```

And different phases can target different queues.

```yaml
phases:

- phase: Windows
  queue: Hosted VS2017
  steps:
  - script: echo hello from Windows

- phase: Linux
  queue: Hosted Linux Preview
  steps:
  - script: echo hello from Linux
```

For details about phase and queue settings, refer [here](yamlgettingstarted-phase.md).

For details about queue authorization, refer [here](yamlgettingstarted-authz.md).
