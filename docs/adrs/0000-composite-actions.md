**Date**: 2021-06-10

**Status**: Accepted

## Context

We released [composite run steps](https://github.com/actions/runner/pull/554) last year which started our journey of reusing steps across different workflow files. To continue that journey, we want to expand composite run steps into composite actions.

We want to support the `uses` steps from workflows in composite actions, including:
  - Container actions
  - javascript actions
  - and even other Composite actions (up to a limit of course!)
  - The pre and post steps these actions can generate

## Guiding Principles

- Composite Actions should function as a single step or action, no matter how many steps it is composed of or how many levels of recursion it has
- A workflow author should not need to understand the inner workings of a composite action in order to use it
- Composite actions should leverage inputs to get values they need, they will not have full access to the `context` objects. The secrets context will **not** be available to composite actions, users will need to pass these values in as an input.
- Other Actions should **just work** inside a composite action, without any code changes

## Decisions

### Composite Recursion Limit

- We will start with supporting a recursion limit of `10` composite actions deep
- We are free to bump this limit in the future, the code will be written to just require updating a variable. If the graph evaluates beyond the recursion limit, the job will fail in the pre-job phase (The `Set up job` step).
- A composite actions interface is its inputs and outputs, nothing else is carried over when invoking recursively.

### Pre/Post Steps in nested Actions

- We do not plan on adding the ability to configure a customizable pre or post step for composite actions at this time. However, we will execute the pre and post steps of any actions referenced in a composite action.
- Composite actions will generate a single pre-step and post-step for the entire composite action, even if there are multiple pre-steps and post-steps in the referenced actions.
  - These steps will execute following the same ordering rules we have today, first to run has their pre step run first and their post step run last.
  - For example, if you had a composite action with two pre steps and two posts steps:

  ```
  - uses: action1
  - uses: composite1
  - uses: action2
  ```

  The order of execution would be:

  ```
  - prestep-action1
  - prestep-composite1
    - prestep-composite1-first-action-referenced
    - prestep-composite1-second-action-referenced
  - prestep-action2
  - the job steps
  - poststep-action2
  - poststep-composite1
    - poststep-composite1-the-second-action-referenced
    - poststep-composite1-first-action-referenced
  - poststep-action1
  ```

#### Set-state

- While the composite action has an individual combined pre/post action, the `set-state` command will not be shared.
- If the `set-state` command is used during a composite step, only the action that originally called `set-state` will have access to the env variable during the post run step.
  - This prevents multiple actions that set the same state from interfering with the execution of another action's post step.

### Resolve Action Endpoint changes

- The resolve actions endpoint will now validate policy to ensure that the given workflow run has access to download that action.
  - It will return a value indicated security was checked. Older server versions would lack this boolean. For composite actions that try to resolve an action, if the server does not respond with this value, we will fail the job as we could violate access policy if we allowed the composite action to continue.
    - Oder GHES/GHAE customers with newer runners will be locked out of composite uses steps until they upgrade their instance.

### Local actions
- TODO describe this more
- We should support pre and post actions for local actions


### If, continue-on-error, timeout-minutes - Not being considered at this time

- `if`, `continue-on-error`, `timeout-minutes` could be supported in composite run/uses steps. These values were not originally supported in our composite run steps implementation.
  - Browsing the community forums and runner repo, there hasn't been a lot of noise asking for these features, so we will hold off on them.
- These values passed as input into the composite action will **not** be carried over as input into the individual steps the composite action runs.

### Defaults - Not being considered at this time

- In actions, we have the idea of [defaults](https://docs.github.com/en/actions/reference/workflow-syntax-for-github-actions#defaultsrun) , which allow you to specify a shell and working directory in one location, rather then on each step.
- However, `shell` is currently required in composite run steps
  - In regular run steps, it is optional, and defaults to a different value based on the OS.
- We want to prioritize the right experience for the consumer, and make the action author continue to explicitly set these values. We can consider improving this experience in the future.

## Consequences

- Workflows are now more reusable across multiple workflow files
- Composite actions implement most of the existing workflow run steps, with room to expand these in the future
- Feature flags will control this rollout