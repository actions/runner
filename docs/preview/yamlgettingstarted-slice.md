# YAML getting started - Slice execution

### Slice

A slice execution dispatches a phase N times, to enable dividing work among the jobs.

The `parallel` setting indicates how many jobs to dispatch.

Variables `system.sliceNumber` and `system.sliceCount` are added to each job. The variables can then be used within your scripts to divide work among the jobs.

```yaml
queue:
  parallel: 5
steps:
- script: test slice=$(system.sliceNumber) sliceCount=$(system.sliceCount)
```
