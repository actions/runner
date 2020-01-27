# ADR 0278: Env Context

**Date**: 2019-09-30

**Status**: Accepted

## Context

User wants to reference workflow variables defined in workflow yaml file for action's input, displayName and condition.  

## Decision

### Add `env` context in the runner

Runner will create and populate the `env` context for every job execution using following logic:
1. On job start, create `env` context with any environment variables in the job message, these are env defined in customer's YAML file's job/workflow level `env` section.
2. Update `env` context when customer use `::set-env::` to set env at the runner level.
3. Update `env` context with step's `env` block before each step runs.

The `env` context is only available in the runner, customer can't use the `env` context in any server evaluation part, just like the `runner` context

Example yaml:
```yaml

env:
  env1: 10
  env2: 20
  env3: 30
jobs:
  build:
    env:
      env1: 100
      env2: 200
    runs-on: ubuntu-latest
    steps:
      - run: |
          echo ${{ env.env1 }}  // 1000
          echo $env1            // 1000
          echo $env2            // 200
          echo $env3            // 30
        if: env.env2 == 200     // true
        name: ${{ env.env1 }}_${{ env.env2 }}   //1000_200
        env:
          env1: 1000
```

### Don't populate the `env` context with environment variables from runner machine. 

With job container and container action, the `env` context may not have the right value customer want and will cause confusion.  
Ex:
```yaml
build:
  runs-on: ubuntu-latest   <- $USER=runner in hosted machine
  container: ubuntu:16.04   <- $USER=root in container
  steps:
  - run: echo ${{env.USER}}    <- what should customer expect this output?  runner/root
  - uses: docker://ubuntu:18.04
    with:
      args: echo ${{env.USER}}  <- what should customer expect this output? runner/root
```
