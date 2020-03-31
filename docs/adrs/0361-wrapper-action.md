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
   post-entrypoint: 'cleanup.sh'
   post-if: 'success()' // Optional
```

Both `pre` and `post` will has default `pre-if/post-if` sets to `always()`.  
Setting `pre` to `always()` will make sure no matter what condition evaluate result the `main` gets at runtime, the `pre` has always run already.   
`pre` executes in order of how the steps are defined.  
`pre` will always be added to job steps list during job setup.  
> Action referenced from local repository (`./my-action`) won't get `pre` setup correctly since the repository haven't checkout during job initialize.  
> We can't use GitHub api to download the repository since there is a about 3 mins delay between `git push` and the new commit available to download using GitHub api.

`post` will be pushed into a `poststeps` stack lazily when the action's `pre` or `main` execution passed `if` condition check and about to run, you can't have an action that only contains a `post`, we will pop and run each `post` after all `pre` and `main` finished.
> Currently `post` works for both repository action (`org/repo@v1`) and local action (`./my-action`)

Valid action:
- only has `main`
- has `pre` and `main`
- has `main` and `post`
- has `pre`, `main` and `post`

Invalid action:
- only has `pre`
- only has `post`
- has `pre` and `post`

Potential downside of introducing `pre`:

- Extra magic wrt step order. Users should control the step order. Especially when we introduce templates.  
- Eliminates the possibility to lazily download the action tarball, since `pre` always run by default, we have to download the tarball to check whether action defined a `pre`  
- `pre` doesn't work with local action, we suggested customer use local action for testing their action changes, ex CI for their action, to avoid delay between `git push` and GitHub repo tarball download api.  
- Condition on the `pre` can't be controlled using dynamic step outputs. `pre` executes too early.
