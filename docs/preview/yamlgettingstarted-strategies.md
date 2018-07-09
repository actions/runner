# YAML getting started - Phase strategies

### Matrix

The `matrix` strategy enables a phase to be dispatched multiple times, with different variable sets.

For example, a common scenario is to run the same build steps for varying permutations of architecture (x86/x64) and configuration (debug/release).

```yaml
queue:
  parallel: 2 # Limit to two agents at a time
  matrix:
    x64_debug:
      buildArch: x64
      buildConfig: debug
    x64_release:
      buildArch: x64
      buildConfig: release
    x86_release:
      buildArch: x86
      buildConfig: release
steps:
- script: build arch=$(buildArch) config=$(buildConfig)
```

### Slice

The `parallel` setting indicates how many jobs to dispatch. Variables `system.sliceNumber` and `system.sliceCount` are added to each job. The variables can then be used within your scripts to divide work among the jobs.

```yaml
queue:
  parallel: 5
steps:
- script: test slice=$(system.sliceNumber) sliceCount=$(system.sliceCount)
```
