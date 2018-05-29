# YAML getting started - Templates

Templates enable steps to be defined once, and used from multiple places.

## Step reuse

In the following example, the same steps are used across multiple phases.

```yaml
# File: steps/build.yml

steps:
- script: npm install
- script: npm test
```

```yaml
# File: .vsts-ci.yml

phases:
- phase: macOS
  queue: Hosted macOS Preview
  steps:
  - template: steps/build.yml # Template reference

- phase: Linux
  queue: Hosted Linux Preview
  steps:
  - template: steps/build.yml # Template reference

- phase: Windows
  queue: Hosted VS2017
  steps:
  - template: steps/build.yml # Template reference
  - script: sign              # Extra step on Windows only
```

For advanced template syntax, see the sections below describing template parameters.

## Phase reuse

In the following example, a phase template is used to build across multiple platforms

```yaml
# File: phases/build.yml

parameters:
  name: ''
  queue: ''
  sign: false

phases:
- phase: ${{ parameters.name }}
  queue: ${{ parameters.queue }}
  steps:
  - script: npm install
  - script: npm test
  - ${{ if eq(parameters.sign, 'true') }}:
    - script: sign
```

```yaml
# File: .vsts-ci.yml

phases:
- template: phases/build.yml  # Template reference
  parameters:
    name: macOS
    queue: Hosted macOS Preview

- template: phases/build.yml  # Template reference
  parameters:
    name: Linux
    queue: Hosted Linux Preview

- template: phases/build.yml  # Template reference
  parameters:
    name: Windows
    queue: Hosted VS2017
    sign: true  # Extra step on Windows only
```

For more details, see the sections below describing template parameters.

## Reuse from other repositories

Repository resources may be defined in the resources section for use as template source. 
For more details on repository resource support and properties see [repositories](yamlgettingstarted-resources.md#Repositories). 

For example, a team which manages a separate repository for templates may define a definition
in one repository which utilizes a common template for an organization. Much like the task
specification uses `task@version` the external template reference uses `filePath@repository`. If
no source specification is present the current repository, or the repository hosting the file 
currently being processed, is used as the template source.

```yaml
# File: steps/msbuild.yml (this is located in the templates repo, defined below in the entry file)

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

resources:
  repositories:
  - repository: templates
    type: github
    endpoint: my-github-endpoint
    name: contoso/build-templates
    ref: refs/tags/lkg
    
steps:
# This file will be pulled from the contoso/build-templates repository
- template: steps/msbuild.yml@templates
  parameters:
    solution: my.sln
    
# This file will be pulled from the same repository as .vsts-ci.yml    
- template: steps/mstest.yml
```

When the file `.vsts-ci.yml` is processed the repository resources will be loaded. In the case of
a git-based repository the `ref` property is used to resolve to a specific commit. Once the version
has been determined it is saved onto the repository resource as a `version` property and is used
for the duration of the pipeline.

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
