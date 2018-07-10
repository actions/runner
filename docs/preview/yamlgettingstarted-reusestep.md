# YAML getting started - Step reuse

Templates enable steps to be defined once, and used from multiple places.

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
