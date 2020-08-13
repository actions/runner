# ADR 0549: Composite Run Steps

**Date**: 2020-06-17

**Status**: Accepted

## Context

Customers want to be able to compose actions from actions (ex: https://github.com/actions/runner/issues/438)

An important step towards meeting this goal is to build in functionality for actions where users can simply execute any number of steps. 

### Guiding Principles

We don't want the workflow author to need to know how the internal workings of the action work. Users shouldn't know the internal workings of the composite action (for example, `default.shell` and `default.workingDir` should not be inherited from the workflow file to the action file). When deciding how to design certain parts of composite run steps, we want to think one logical step from the consumer.

A composite action is treated as **one** individual job step (this is known as encapsulation).

## Decision

**In this ADR, we only support running multiple run steps in an Action.** In doing so, we build in support for mapping and flowing the inputs, outputs, and env variables (ex: All nested steps should have access to its parents' input variables and nested steps can overwrite the input variables).

### Composite Run Steps Features
This feature supports at the top action level:
- name
- description
- inputs
- runs
- outputs

This feature supports at the run step level:
- name
- id
- run
- env
- shell
- working-directory

This feature **does not support** at the run step level:
- timeout-minutes
- secrets
- conditionals (needs, if, etc.)
- continue-on-error

### Steps

Example `workflow.yml`

```yaml
jobs:
  build:
    runs-on: self-hosted
    steps:
    - id: step1
      uses: actions/setup-python@v1
    - id: step2
      uses: actions/setup-node@v2
    - uses: actions/checkout@v2
    - uses: user/composite@v1
    - name: workflow step 1
      run: echo hello world 3
    - name: workflow step 2
      run: echo hello world 4
```

Example `user/composite/action.yml`

```yaml
runs:
  using: "composite"
  steps:
    - run: pip install -r requirements.txt
      shell: bash
    - run: npm install
      shell: bash
```

Example Output

```yaml
[npm installation output]
[pip requirements output]
echo hello world 3
echo hello world 4
```

We add a token called "composite" which allows our Runner code to process composite actions. By invoking "using: composite", our Runner code then processes the "steps" attribute, converts this template code to a list of steps, and finally runs each run step sequentially. If any step fails and there are no `if` conditions defined, the whole composite action job fails. 

### Defaults

We will not support "defaults" in a composite action. 

### Shell and Working-directory

For each run step in a composite action, the action author can set the `shell` and `working-directory` attributes for that step. The shell attribute is **required** for each run step because the action author does not know what the workflow author is using for the operating system so we need to explicitly prevent unknown behavior by making sure that each run step has an explicit shell **set by the action author.** On the other hand, `working-directory` is optional. Moreover, the composite action author can map in values from the `inputs` for it's `shell` and `working-directory` attributes at the step level for an action. 

For example,

`action.yml`


```yaml
inputs:
  shell_1:
    description: 'Your name'
    default: 'pwsh'
steps:
  - run: echo 1
    shell: ${{ inputs.shell_1 }}
```

Note, the workflow file and action file are treated as separate entities. **So, the workflow `defaults` will never change the `shell` and `working-directory` value in the run steps in a composite action.** Note, `defaults` in a workflow only apply to run steps not "uses" steps (steps that use an action).

### Running Local Scripts

Example 'workflow.yml':
```yaml
jobs:
  build:
    runs-on: self-hosted
    steps:
    - uses: user/composite@v1
```

Example `user/composite/action.yml`:

```yaml
runs:
  using: "composite"
  steps: 
    - run: chmod +x ${{ github.action_path }}/test/script2.sh
      shell: bash
    - run: chmod +x $GITHUB_ACTION_PATH/script.sh
      shell: bash
    - run: ${{ github.action_path }}/test/script2.sh
      shell: bash
    - run: $GITHUB_ACTION_PATH/script.sh
      shell: bash
```
Where `user/composite` has the file structure:
```
.
+-- action.yml
+-- script.sh
+-- test
|   +-- script2.sh
```


Users will be able to run scripts located in their action folder by first prepending the relative path and script name with `$GITHUB_ACTION_PATH` or `github.action_path` which contains the path in which the composite action is downloaded to and where those "files" live. Note, you'll have to use `chmod` before running each script if you do not git check in your script files into your github repo with the executable bit turned on.

### Inputs

Example `workflow.yml`:

```yaml
steps: 
  - id: foo
    uses: user/composite@v1
    with:
      your_name: "Octocat"
```

Example `user/composite/action.yml`:

```yaml
inputs:
  your_name:
    description: 'Your name'
    default: 'Ethan'
runs:
  using: "composite"
  steps: 
    - run: echo hello ${{ inputs.your_name }}
      shell: bash
```

Example Output:

```
hello Octocat
```

Each input variable in the composite action is only viewable in its own scope.

### Outputs

Example `workflow.yml`:

```yaml
...
steps: 
  - id: foo
    uses: user/composite@v1
  - run: echo random-number ${{ steps.foo.outputs.random-number }} 
    shell: bash
```

Example `user/composite/action.yml`:

```yaml
outputs:
  random-number: 
    description: "Random number"
    value: ${{ steps.random-number-generator.outputs.random-id }}
runs:
  using: "composite"
  steps: 
    - id: random-number-generator
      run: echo "::set-output name=random-id::$(echo $RANDOM)"
      shell: bash
```

Example Output:

```
::set-output name=my-output::43243
random-number 43243
```

Each of the output variables from the composite action is viewable from the workflow file that uses the composite action. In other words, every child action output(s) is viewable only by its parent using dot notation (ex `steps.foo.outputs.random-number`).

Moreover, the output ids are only accessible within the scope where it was defined. Note that in the example above, in our `workflow.yml` file, it should not have access to output id (i.e. `random-id`). The reason why we are doing this is because we don't want to require the workflow author to know the internal workings of the composite action.

### Context

Similar to the workflow file, the composite action has access to the [same context objects](https://help.github.com/en/actions/reference/context-and-expression-syntax-for-github-actions#contexts) (ex: `github`, `env`, `strategy`). 

### Environment

In the Composite Action, you'll only be able to use `::set-env::` to set environment variables just like you could with other actions.

### Secrets

**We will not support "Secrets" in a composite action for now. This functionality will be focused on in a future ADR.**

We'll pass the secrets from the composite action's parents (ex: the workflow file) to the composite action. Secrets can be created in the composite action with the secrets context. In the actions yaml, we'll automatically mask the secret. 


### If Condition

** If and needs conditions will not be supported in the composite run steps feature. It will be supported later on in a new feature. **

Old reasoning:

Example `workflow.yml`:

```yaml
steps:
  - run: exit 1
  - uses: user/composite@v1  # <--- this will run, as it's marked as always runing
    if: always()
```

Example `user/composite/action.yml`:

```yaml
runs:
  using: "composite"
  steps:
    - run: echo "just succeeding"
      shell: bash
    - run: echo "I will run, as my current scope is succeeding"
      shell: bash
      if: success()
    - run: exit 1
      shell: bash
    - run: echo "I will not run, as my current scope is now failing"
      shell: bash
```

**We will not support "if Condition" in a composite action for now. This functionality will be focused on in a future ADR.**

See the paragraph below for a rudimentary approach (thank you to @cybojenix for the idea, example, and explanation for this approach):

The `if` statement in the parent (in the example above, this is the `workflow.yml`) shows whether or not we should run the composite action. So, our composite action will run since the `if` condition for running the composite action is `always()`.

**Note that the if condition on the parent does not propagate to the rest of its children though.**

In the child action (in this example, this is the `action.yml`), it starts with a clean slate (in other words, no imposing if conditions). Similar to the logic in the paragraph above, `echo "I will run, as my current scope is succeeding"` will run since the `if` condition checks if the previous steps **within this composite action** has not failed. `run: echo "I will not run, as my current scope is now failing"` will not run since the previous step resulted in an error and by default, the if expression is set to `success()` if the if condition is not set for a step.


What if a step has `cancelled()`? We do the opposite of our approach above if `cancelled()` is used for any of our composite run steps. We will cancel any step that has this condition if the workflow is cancelled at all.
    
### Timeout-minutes

Example `workflow.yml`:

```yaml
steps: 
  - id: bar
    uses: user/test@v1
    timeout-minutes: 50
```

Example `user/composite/action.yml`:

```yaml
runs:
  using: "composite"
  steps: 
    - id: foo1
      run: echo test 1
      timeout-minutes: 10
      shell: bash
    - id: foo2
      run: echo test 2
      shell: bash
    - id: foo3
      run: echo test 3
      timeout-minutes: 10
      shell: bash
```

**We will not support "timeout-minutes" in a composite action for now. This functionality will be focused on in a future ADR.**

A composite action in its entirety is a job. You can set both timeout-minutes for the whole composite action or its steps as long as the the sum of the `timeout-minutes` for each composite action step that has the attribute `timeout-minutes` is less than or equals to `timeout-minutes` for the composite action. There is no default timeout-minutes for each composite action step. 

If the time taken for any of the steps in combination or individually exceed the whole composite action `timeout-minutes` attribute, the whole job will fail (1). If an individual step exceeds its own `timeout-minutes` attribute but the total time that has been used including this step is below the overall composite action `timeout-minutes`, the individual step will fail but the rest of the steps will run based on their own `timeout-minutes` attribute (they will still abide by condition (1) though).

For reference, in the example above, if the composite step `foo1` takes 11 minutes to run, that step will fail but the rest of the steps, `foo1` and `foo2`, will proceed as long as their total runtime with the previous failed `foo1` action is less than the composite action's `timeout-minutes` (50 minutes). If the composite step `foo2` takes 51 minutes to run, it will cause the whole composite action job to fail. I

The rationale behind this is that users can configure their steps with the `if` condition to conditionally set how steps rely on each other. Due to the additional capabilities that are offered with combining `timeout-minutes` and/or `if`, we wanted the `timeout-minutes` condition to be as dumb as possible and not effect other steps. 

[Usage limits still apply](https://help.github.com/en/actions/reference/workflow-syntax-for-github-actions?query=if%28%29#usage-limits)


### Continue-on-error

Example `workflow.yml`:

```yaml
steps: 
  - run: exit 1
  - id: bar
    uses: user/test@v1
    continue-on-error: false
  - id: foo
    run: echo "Hello World" <------- This step will not run
```

Example `user/composite/action.yml`:

```yaml
runs:
  using: "composite"
  steps: 
    - run: exit 1
      continue-on-error: true
      shell: bash
    - run: echo "Hello World 2" <----- This step will run
      shell: bash
```

**We will not support "continue-on-error" in a composite action for now. This functionality will be focused on in a future ADR.**

If any of the steps fail in the composite action and the `continue-on-error` is set to `false` for the whole composite action step in the workflow file, then the steps below it will run. On the flip side, if `continue-on-error` is set to `true` for the whole composite action step in the workflow file, the next job step will run.

For the composite action steps, it follows the same logic as above. In this example, `"Hello World 2"` will be outputted because the previous step has `continue-on-error` set to `true` although that previous step errored. 

### Visualizing Composite Action in the GitHub Actions UI
We want all the composite action's steps to be condensed into the original composite action node. 

Here is a visual represenation of the [first example](#Steps)

```yaml
| composite_action_node |
    | echo hello world 1 | 
    | echo hello world 2 |
| echo hello world 3 | 
| echo hello world 4 |

```


## Consequences

This ADR lays the framework for eventually supporting nested Composite Actions within Composite Actions. This ADR allows for users to run multiple run steps within a GitHub Composite Action with the support of inputs, outputs, environment, and context for use in any steps as well as the if, timeout-minutes, and the continue-on-error attributes for each Composite Action step. 
