# YAML getting started - Build number format (deploying end of Nov)

The build number format can be specified as a name property at the root of the document.

Example:

```yaml
name: $(BuildDefinitionName)_$(Date:yyyyMMdd)$(Rev:.rr)
steps:
- script: echo hello world
```

[Refer here](https://docs.microsoft.com/en-us/vsts/build-release/concepts/definitions/build/options#build-number-format) for details about build number format.
