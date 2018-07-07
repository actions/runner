# YAML getting started - Setting variables from a script

A new variable can be defined within a step, and can be accessed from a downstream step.

Example for macOS and Linux:

```yaml
queue: Hosted Linux Preview

steps:

# Create a variable
- script: |
    echo '##vso[task.setvariable variable=myVariable]abc123'

# Print the variable
- script: |
    echo my variable is $(myVariable)
```

Example for Windows:

```yaml
queue: Hosted VS2017

steps:

# Create a variable
- script: |
    echo ##vso[task.setvariable variable=myVariable]abc123

# Print the variable
- script: |
    echo my variable is $(myVariable)
```
