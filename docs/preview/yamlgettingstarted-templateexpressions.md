# YAML getting started - Template expressions

## Template expression syntax

Template expressions enable values to be dynamically resolved during pipeline initialization.

A value that starts and ends with `${{ }}` indicates a template expression.

## Contexts

The following contexts are available within template expressions:

* **variables** - provides access to the built-in system variables. For more about variables, refer [here](https://docs.microsoft.com/en-us/vsts/build-release/concepts/definitions/build/variables).

* **parameters** - provides access to the parameters passed to the file being processed. Template parameters are described further below.

## Functions

All general functions of [task conditions](https://go.microsoft.com/fwlink/?linkid=842996) are available within template expressions.

Additionally the following two functions are available:

* **format** - string format, ex: `format('{0} Build', parameters.os)`

* **coalesce** - coalesce null or empty string, ex: `coalesce(parameters.restoreProjects, parameters.buildProjects)`

## Template parameters

Parameters allow the caller to influence how a template is expanded. Parameters
can be used for simple or complex values. Parameters even allow the template author
to defined pre/post hooks throughout the template.

### Example: Simple parameter

Parameters are expanded using template expressions. A value that starts and ends
with `${{ }}` indicates a template expression.

For example:

```yaml
# File: steps/msbuild.yml

parameters:
  solution: '**/*.sln'

steps:
- task: msbuild@1
  inputs:
    solution: ${{ parameters.solution }}
- task: vstest@2
  inputs:
    solution: ${{ parameters.solution }}
```

```yaml
# File: .vsts-ci.yml

steps:
- template: steps/msbuild.yml
  parameters:
    solution: my.sln
```


### Example: Insert into a sequence

```yaml
# File: phases/build.yml

parameters:
  preBuild: []
  preTest: []
  preSign: []

phases:
- phase: Build
  queue: Hosted VS2017
  steps:
  - script: cred-scan
  - ${{ parameters.preBuild }}
  - task: msbuild@1
  - ${{ parameters.preTest }}
  - task: vstest@2
  - ${{ parameters.preSign }}
  - script: sign
```

```yaml
# File: .vsts.ci.yml

phases:
- template: phases/build.yml
  parameters:
    preBuild:
    - script: echo hello from pre-build
    preTest:
    - script: echo hello from pre-test
```

Note, when an array is inserted into an array, the nested array is flattened.

### Example: Insert into a dictionary

In the example below, the `${{ insert }}` property indicates the value that follows
is expected to be a mapping, and should be inserted into the outer mapping.

```yaml
# Default values
parameters:
  variables: {}

phases:
- phase: build
  variables:
    configuration: debug
    arch: x86
    ${{ insert }}: ${{ parameters.variables }}
  steps:
  - task: msbuild@1
  - task: vstest@2
```

```yaml
phases:
- template: phases/build.yml
  parameters:
    variables:
      TEST_SUITE: L0,L1
```

### Example: Conditionally insert into a sequence

```yaml
# File: steps/build.yml

parameters:
  toolset: msbuild

steps:
# msbuild
- ${{ if eq(parameters.toolset, 'msbuild') }}:
  - task: msbuild@1
  - task: vstest@2

# dotnet
- ${{ if eq(parameters.toolset, 'dotnet') }}:
  - task: dotnet@1
    inputs:
      command: build
  - task: dotnet@1
    inputs:
      command: test
```

```yaml
# File: .vsts-ci.yml

steps:
- template: steps/build.yml
  parameters:
    toolset: dotnet
```

### Example: Conditionally insert into a mapping

```yaml
# File: steps/build.yml

parameters:
  debug: false

steps:
- script: tool
  env:
    ${{ if eq(parameters.debug, 'true') }}:
      TOOL_DEBUG: true
      TOOL_DEBUG_DIR: _dbg
```

```yaml
steps:
- template: steps/build.yml
  parameters:
    debug: true
```

## Escaping

Prepend additional leading `$` characters to escape a value that literally starts and ends with `${{ }}`. For example: `$${{ }}`
