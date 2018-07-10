# YAML getting started - Phase reuse

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

For details about parameter syntax, refer [here](yamlgettingstarted-templateexpressions.md).
