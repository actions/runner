# YAML getting started - Custom variables

## Custom variables

Custom variables can be defined within the pipeline.

For example:

```yaml
# Set variables once
variables:
  configuration: debug
  platform: x64

steps:

# Build solution 1
- task: MSBuild@1
  inputs:
    solution: solution1.sln
    configuration: $(configuration) # Use the variable
    platform: $(platform)

# Build solution 2
- task: MSBuild@1
  inputs:
    solution: solution2.sln
    configuration: $(configuration) # Use the variable
    platform: $(platform)
```

## Defined in the web

Variables can also be defined centrally from the definition editor in the web, on the `Variables` tab.