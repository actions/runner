# YAML getting started - Matrix execution

### Matrix

A `matrix` execution enables a phase to be dispatched multiple times, with different variable sets.

For example, a common scenario is to run the same build steps for varying permutations of architecture (x86/x64) and configuration (debug/release).

In the following example, different variables `buildArch` and `buildConfig` are added for each job that is dispatched.

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

When combined with a matrix, the `parallel` property indicates the maximum number of jobs to run concurrently.
