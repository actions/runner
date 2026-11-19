# ADR 1438: Support Conditionals In Composite Actions

**Date**: 2021-10-13

**Status**: Accepted

## Context

We recently shipped composite actions, which allows you to reuse individual steps inside an action. 
However, one of the [most requested features](https://github.com/actions/runner/issues/834) has been a way to support the `if` keyword.

### Goals
- We want to keep consistent with current behavior
- We want to support conditionals via the `if` keyword
- Our built in functions like `success` should be implementable without calling them, for example you can do `job.status == success` rather than `success()` currently.

### How does composite currently work?

Currently, we have limited conditional support in composite actions for `pre` and `post` steps. 
These are based on the `job status`, and support keywords like `always()`, `failed()`, `success()` and `cancelled()`. 
However, generic or main steps do **not** support conditionals.

By default, in a regular workflow, a step runs on the `success()` condition. Which looks at the **job** **status**, sees if it is successful and runs.

By default, in a composite action, main steps run until a single step fails in that composite action, then the composite action is halted early. It does **not** care about the job status.
Pre, and post steps in composite actions use the job status to determine if they should run.

### How do we go forward?

Well, if we think about what composite actions are currently doing when invoking main steps, they are checking if the current composite action is successful. 
Lets formalize that concept into a "real" idea.

- We will add an `action_status` field to the github context to mimic the [job's context status](https://docs.github.com/en/actions/learn-github-actions/contexts#job-context).
  - We have an existing concept that does this `action_path` which is only set for composite actions on the github context.
- In a composite action during a main step, the `success()` function will check if `action_status == success`, rather than `job_status == success`. Failure will work the same way. 
  - Pre and post steps in composite actions will not change, they will continue to check the job status.


### Nested Scenario
For nested composite actions, we will follow the existing behavior, you only care about your current composite action, not any parents. 
For example, lets imagine a scenario with a simple nested composite action

```
- Job
  - Regular Step
  - Composite Action
    - runs: exit 1
    - if: always()
      uses: A child composite action
        - if: success()
          runs: echo "this should print"
        - runs: echo "this should also print"
    - if: success()
      runs: echo "this will not print as the current composite action has failed already"
      
```
The child composite actions steps should run in this example, the child composite action has not yet failed, so it should run all steps until a step fails. This is consistent with how a composite action currently works in production if the main job fails but a composite action is invoked with `if:always()` or `if: failure()`

### Other options explored
We could add the `current_step_status` to the job context rather than `__status` to the steps context, however this comes with two major downsides:
- We need to support the field for every type of step, because its non trivial to remove a field from the job context once it has been added (its readonly)
  - For all actions besides composite it would only every be `success`
  - Its weird to have a `current_step` value on the job context
- We also explored a `__status` on the steps context.
  - The `__` is required to prevent us from colliding with a step with id: status
  - This felt wrong because the naming was not smooth, and did not fit into current conventions.

### Consequences
- github context has a new field for the status of the current composite action.
- We support conditional's in composite actions
- We keep the existing behavior for all users, but allow them to expand that functionality.
