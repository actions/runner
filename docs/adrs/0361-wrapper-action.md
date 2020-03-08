# ADR 361: Wrapper Action

**Date**: 2020-03-06

**Status**: Pending

## Context

In addition to action's regular execution, action author may wants their action has a chance to participate in:
- Job initialize  
  My Action will collect machine resource usage (CPU/RAM/Disk) during a workflow job execution, we need to start perf recorder at the begin of the job.
- Job cleanup  
  My Action will dirty local workspace or machine environment during execution, we need to cleanup these changes at the end of the job.  
  Ex: `actions/checkout@v2` will write `github.token` into local `.git/config` during execution, it has post job cleanup defined to undo the changes.

## Decision

### Add `pre` and `post` execution to action

Node Action Example:

```yaml
 name: 'My action with pre'
 description: 'My action with pre'
 runs:
   using: 'node12'
   pre: 'setup.js'
   pre-if: 'success()' // Optional
   main: 'index.js'

 name: 'My action with post'
 description: 'My action with post'
 runs:
   using: 'node12'
   main: 'index.js'
   post: 'cleanup.js'
   post-if: 'success()' // Optional
```

Container Action Example:

```yaml
 name: 'My action with pre'
 description: 'My action with pre'
 runs:
   using: 'docker'
   image: 'mycontainer:latest'
   pre-entrypoint: 'setup.sh'
   pre-if: 'success()' // Optional
   entrypoint: 'entrypoint.sh'

 name: 'My action with post'
 description: 'My action with post'
 runs:
   using: 'docker'
   image: 'mycontainer:latest'
   entrypoint: 'entrypoint.sh'
   post-entrypoint: 'cleanup.sh'
   post-if: 'success()' // Optional
```

Both `pre` and `post` will has default `pre-if/post-if` sets to `always()`.  
`pre` executes in order of how the steps are defined and `post` runs in the opposite order.
`pre` will always be added to job steps list during job setup.  
> Action referenced from local repository (`./my-action`) won't get `pre` setup correctly since the repository haven't checkout during job initialize.  
> We can't use GitHub api to download the repository since there is a about 3 mins delay between `git push` and the new commit available to download using GitHub api.

`post` will be added to steps list lazily when the action's main execution passed `if` condition check and about to run.
> Currently `post` works for both repository action (`org/repo@v1`) and local action (`./my-action`)

When you use an action in the job, it can be a stand alone action which doesn't need `pre` and `post`, or it can be an action that serves steps before it which might need `pre` to initialize or it can be an action that serves steps after it which might need `post` to cleanup.
It doesn't really make sense to me that an action needs to have `pre`, `main` and `post` all together.   
Plus, since `post` is added to steps list lazily, when some actions has both `pre` and `post`, it might cause action which only has `post` doesn't run its `post` in the right order.
