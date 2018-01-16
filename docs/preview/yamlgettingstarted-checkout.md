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

## Checkout/repo options reference

```yaml
clean: true | false
fetchDepth: number
lfs: true | false
```
