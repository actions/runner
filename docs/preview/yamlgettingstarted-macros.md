# YAML getting started - Macro syntax

Within pipeline inputs, variables can be referenced using macro syntax.

Example for macOS and Linux:

```yaml
queue: Hosted Linux Preview
steps:
- script: ls
  workingDirectory: $(agent.homeDirectory)
```

Example for Windows:

```yaml
queue: Hosted VS2017
steps:
- script: dir
  workingDirectory: $(agent.homeDirectory)
```
