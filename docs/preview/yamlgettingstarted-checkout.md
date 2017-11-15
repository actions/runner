# YAML getting started - Checkout options

## Checkout step

The `checkout` step can be used to control the checkout options. The well-known \"self\" repo is the repo associated with the .yml entry file.

For example, the clean setting can be specified on the checkout step:

```yaml
steps:
- checkout: self
  clean: true
- script: echo hello world
```

## Skip checkout

To skip the checkout step, a `checkout: none` step must be added. When the list of steps does not contain a checkout step at all, a `checkout: self` step is implicitly added by the system.

```yaml
steps:
- checkout: none
- script: echo hello world
```

## Multi-phase checkout options

The checkout options can also be specified in the resources section of the .yml file. The values specified in the resource section are treated as the defaults, and can be overridden by individual checkout steps.

For example:

```yaml
resources:
- repo: self
  clean: true
  lfs: true
phases:
- phase: A
  steps:
  # implicit checkout step; inherits checkout options from the resources section
  - script: echo hello world from phase A
- phase: B
  steps:
  - checkout: self # explicit checkout step, inherits options from the resources section
  - script: echo hello world from phase B
- phase: C
  steps:
  - checkout: self # explicit checkout step
    clean: false # overrides clean, inherits other options from the resources section
  - script: echo hello world from phase C
```

## Checkout/repo options reference

```yaml
clean: true | false
fetchDepth: number
lfs: true | false
```
