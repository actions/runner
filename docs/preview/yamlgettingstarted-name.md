# YAML getting started - Pipeline instance name

The pipeline instance name can be specified using the `name` property.

For example:

```yaml
name: $(BuildDefinitionName)_$(Date:yyyyMMdd)$(Rev:.rr)
steps:
- script: echo hello world
```

[Refer here](https://docs.microsoft.com/en-us/vsts/build-release/concepts/definitions/build/options#build-number-format) for details about the pipeline instance name.
