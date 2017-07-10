# YAML getting started - Mustache text templating (internal only, public preview soon)

The YAML are run through a mustache-text-template preprocessor prior to deserialization.

## Server generated mustache context

When a definition is triggered (manual, CI, or scheduled), information about the event is available to the entry YAML file as the mustache context object. See following sample data:

```yaml
TODO: Sample variables at queue time.
```

For a full list of values available in your environment, you can dump the context:

```yaml
steps:
  - powershell: |
      @'
      {{#each @root}}
      "{{@key}}": "{{this}}"
      {{/each}}
      '@
    condition: and(succeeded(), eq(variables['agent.os'], 'Windows_NT'))
  - bash: |
      echo @'
      {{#each @root}}
      "{{@key}}": "{{this}}"
      {{/each}}
      '@
    condition: and(succeeded(), in(variables['agent.os'], 'Darwin', 'Linux'))
```

## User defined mustache context

The server generated context will be overlaid onto optional user-defined context. User-defined context can be defined as YAML front matter at the beginning of any document (must be at the top). The user-defined context is delimited by a starting and ending line `---`.

Example YAML front matter:

```yaml
---
matrix:
 - buildConfiguration: debug
   buildPlatform: any cpu
 - buildConfiguration: release
   buildPlatform: any cpu
---
jobs:
  {{#each matrix}}
  - name: build-{{buildConfiguration}}-{{buildPlatform}}}
    - task: VSBuild@1.*
      inputs:
        solution: "**/*.sln"
        configuration: "{{buildConfiguration}}"
        platform: "{{buildPlatform}}"
  {{/each}}
```

## Mustache escaping rules

Properties referenced using `{{property}}` will be JSON-string-escaped. Escaping can be omitted by using a triple-stash: `{{{property}}}`.

Literal `{{` and `}}` can be escaped as `\{{` and `\}}`.
