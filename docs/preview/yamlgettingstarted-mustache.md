# YAML getting started - Mustache text templating (internal only, public preview soon)

The YAML files are run through a mustache-text-template processor prior to deserialization.

Mustache uses `{{jsonSelector}}` syntax to reference items from the context.

## Mustache escaping rules

Values referenced using `{{jsonSelector}}` will be JSON-string-escaped. Escaping can be
omitted by using a triple-stash: `{{{jsonSelector}}}`.

Literal `{{` and `}}` can be escaped as `\{{` and `\}}`.

## Server generated mustache context

When a definition is triggered (manual, CI, or scheduled), information about the event is
available to the entry YAML file as the mustache context. See following sample data:

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

The server generated context is overlaid onto optional user-defined context. User-defined
context can be defined in a YAML front matter section at the very beginning of any document.
The user-defined context is delimited by a starting and ending line `---`.

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

## Selector syntax

The JSON selectors use `.` and `/` to distinguish between segments.

The selector syntax supports the following keywords:

| Keyword  | Description |
| -------- | ----------- |
| `@root`  | Selects the root context |
| `..`     | Selects the parent context |
| `this`   | Current context |
| `.`      | Current context |
| `@index` | Index of the current context |
| `@key`   | Key of the current context |
| `@first` | True if the current index is the first item |
| `@last`  | True if the current index is the last item |


## Mustache helper functions

Several mustache helper functions are available. Arguments to the helper functions are
resolved as either literals or context selectors.

In the following example, the value resolved by the selector `./build.reason` is compared
against the literal string `'pullrequest'`. The function result (`true` or `false`) is
used to control whether the task is enabled.

```yaml
steps:
  - task: PublishBuildArtifacts@1
    enabled: {{notEquals ./'build.reason' 'pullrequest' true}}
    inputs:
      pathToPublish: $(build.binariesDirectory)
      artifactName: binaries
      artifactType: container
```

### Block expressions

Block expressions can be used to conditionally evaluate nested content.

Example:

```yaml
steps:
  # Only publish when not a pull request build
  {{#notEquals ./'build.reason' 'pullrequest' true}}
  - task: PublishBuildArtifacts@1
    inputs:
      pathToPublish: $(build.binariesDirectory)
      artifactName: binaries
      artifactType: container
  {{/notEquals}}
```

Block expressions start with `#` and must be paired with a closing expression. Closing expressions
start with `/` and must be followed by either the helper name or the full expression.

### Inverted block expressions

Alternatively, block expressions can start with `^` which inverts the condition.

Example:

```yaml
steps:
  # Only publish artifacts when not a pull request build
  {{^equals ./'build.reason' 'pullrequest' true}}
  - task: PublishBuildArtifacts@1
    inputs:
      pathToPublish: $(build.binariesDirectory)
      artifactName: binaries
      artifactType: container
  {{/equals}}
```

### equals function

Signature: `(left: string, right: string, ignoreCase?: bool = false): bool`

Performs string equals comparison.

### notEquals function

Signature: `(left: string, right: string, ignoreCase?: bool = false): bool`

Performs string not equals comparison.

### contains function

Signature: `(left: string, right: string, ignoreCase?: bool = false): bool`

Performs string contains.

### with function

Signature: `(selector: string): void`

Evaluates the inner block using the context specified by the selector.

### if function

Signature: `(selector: string): bool`

Returns true if the selector result is truthy. See truthy table below.

Note, when used with a block expression, the `if` function is equivalent to specifying a selector only. Example:

```yaml
{{#if jsonSelector}}
  # Do something
{{/if}}

{{#jsonSelector}}
  # Equivalent to "if" expression above
{{/jsonSelector}}
```

### else function

Signature: `(selector: string): bool`

Paired with an `if` or `unless` expression. Evaluates the inner block if the corresponding `if` or `unless` is false.

```yaml
{{#if equals ./'build.reason' 'pullrequest' true}}
  # do this when pull request build
{{#else}}
  # do this when not a pull request build
{{/if}}
```

### unless function

Signature: `(selector: string): bool`

Returns true if the selector is falsy. See truthy table below.

Note, when used with a block expression, the `unless` function is equivalent to specifying an inverted block expression with a selector only. Example:

```yaml
{{#unless jsonSelector}}
  # Do something
{{/unless}}

{{^jsonSelector}}
  # Equivalent to "unless" expression above
{{/jsonSelector}}
```

### each function

Signature: `(selector: string): void`

Evaluates the inner block once for every item in an array or object.

### lookup function

Signature: `(selector: string, type: string): string`

Retrieves the index or key of an item. The argument `type` must be `@index` or `@key`.

## Truthy table

| Type    | Result |
| ------- | ------ |
| Array   | True if not empty; otherwise false |
| Null    | False |
| Number  | True if not zero; otherwise false |
| Object  | True |
| String  | True if not empty; otherwise false |
