TODO: Change file name to represent the correct PR number (PR is not created yet for this ADR)

# ADR 0549: Composite Run Steps

**Date**: 2020-06-17

**Status**: Proposed

**Relevant PR**: https://github.com/actions/runner/pull/549

## Context
Customers want to be able to compose actions from actions (ex: https://github.com/actions/runner/issues/438)

An important step towards meeting this goal is to build in functionality for actions where users can simply execute any number of steps. 

## Decision
In this ADR, we support running multiple steps in an Action. In doing so, we build in support for mapping and flowing the inputs, outputs, and env variables (ex: All nested steps should have access to its parents' input variables and nested steps can overwrite the input variables).

### Steps
Example `user/test/composite-action.yml`
```
...
using: 'composite' 
steps:
  - run: pip install -r requirements.txt
  - run: npm install
```
Example `workflow.yml`
```
...
jobs:
  build:
    runs-on: self-hosted
    steps:
    - id: job1
      uses: actions/setup-python@v1
    - id: job2
      uses: actions/setup-node@v2
    - uses: actions/checkout@v2
      needs: [job1, job2]
    - uses: user/test@v1
    - name: workflow step 1
      run: echo hello world 3
    - name: workflow step 2
      run: echo hello world 4
```
Example Output
```
[npm installation output]
[pip requirements output]
echo hello world 3
echo hello world 4
```
We add a token called "composite" which allows our Runner code to process composite actions. By invoking "using: composite", our Runner code then processes the "steps" attribute, converts this template code to a list of steps, and finally runs each run step sequentially. If any step fails and there are no `if` conditions defined, the whole composite action job fails. 

### Inputs
Example `user/test/composite-action.yml`:
```
using: 'composite' 
inputs:
  your_name:
    description: 'Your name'
    default: 'Ethan'
steps: 
  - run: echo hello ${{ inputs.your_name }}
```

Example `workflow.yml`:
```
...
steps: 
  - id: foo
    uses: user/test@v1
    with:
      your_name: "Octocat"
  - run: echo hello ${{ steps.foo.inputs.your_name }} 2
```

Example Output:
```
hello Octocat
Error
```

Each input variable in the composite action is only viewable in its own scope (unlike environment variables). As seen in the last line in the example output, in the workflow file, it will not have access to the action's `inputs` attribute.

### Outputs
Example `user/test/composite-action.yml`:
```
using: 'composite' 
inputs:
  your_name:
    description: 'Your name'
    default: 'Ethan'
outputs:
  bar: ${{ steps.my-step.my-output}}
steps: 
  - id: my-step
    run: |
      echo ::set-output name=my-output::my-value
      echo hello ${{ inputs.your_name }} 
```

Example `workflow.yml`:
```
...
steps: 
  - id: foo
    uses: user/test@v1
    with:
      your_name: "Octocat"
  - run: echo oh ${{ steps.foo.outputs.bar }} 
```

Example Output:
```
hello Octocat
oh hello Octocat
```
Each of the output variables from the composite action is viewable from the workflow file that uses the composite action. In other words, every child action output(s) is viewable only by its parent using dot notation (ex `steps.foo.outputs.bar`).

Moreover, the output ids are only accessible within the scope where it was defined. Note that in the example above, in our `workflow.yml` file, it should not have access to output id (i.e. `my-output`). For example, in the `workflow.yml` file, you can't run `foo.steps.my-step.my-output`.

### Context
Similar to the workflow file, the composite action has access to the [same context objects](https://help.github.com/en/actions/reference/context-and-expression-syntax-for-github-actions#contexts) (ex: `github`, `env`, `strategy`). 

### Environment
Example `user/test/composite-action.yml`:
```
using: 'composite' 
env:
  NAME2: test2
  SERVER: development
steps: 
  - id: my-step
    run: |
      echo NAME2 $NAME2
      echo Server $SERVER 
```

Example `workflow.yml`:
```
env:
  NAME1: test1
  SERVER: production
steps: 
  - id: foo
    uses: user/test@v1
  - run: echo Server $SERVER
```

Example Output:
```
NAME2 test2
Server development
Server production
```

We plan to use environment variables for Composite Actions similar to the parent/child relationship between nested function calls in programming languages like Python in terms of [lexical scoping](https://inst.eecs.berkeley.edu/~cs61a/fa19/assets/slides/29-Tail_Calls_full.pdf). In Python, let's say you have `functionA` that has local variables called `a` and `b` in this function frame. Let's say we have a `functionB` whose parent frame is `functionA` and has local variable `a` (aka `functionB` is called and defined in `functionA`). `functionB` will have access to its parent input variables that are not overwritten in the local scope (`a`) as well as its own local variable `b`. [Visual Example](http://www.pythontutor.com/visualize.html#code=def%20functionA%28%29%3A%0A%20%20%20%20a%20%3D%201%0A%20%20%20%20b%20%3D%202%0A%20%20%20%20def%20functionB%28%29%3A%0A%20%20%20%20%20%20%20%20b%20%3D%203%0A%20%20%20%20%20%20%20%20print%28%22a%22,%20a%29%0A%20%20%20%20%20%20%20%20print%28%22b%22,%20b%29%0A%20%20%20%20%20%20%20%20return%20b%0A%20%20%20%20return%20functionB%28%29%0A%0A%0A%0AfunctionA%28%29&cumulative=false&curInstr=14&heapPrimitives=nevernest&mode=display&origin=opt-frontend.js&py=3&rawInputLstJSON=%5B%5D&textReferences=false) 

Similar to the above logic, the environment variables will flow from the parent node to its children node. More concretely, whatever workflow/action calls a composite action, that composite action has access to whatever environment variables its caller workflow/action has. Note that the composite action can append its own environment variables or overwrite its parent's environment variables. 


### If Condition

Example `user/test/composite-action.yml`:
```
using: 'composite' 
steps: 
  - id: foo2
    run: ERROR
  - id: foo3
    run: echo test 2
    if: success()
  - id: foo4
    run: echo test 3
```

Example `workflow.yml`:
```
steps: 
  - id: foo
    uses: user/test@v1
    if: always()
  - run: Server: ${{ env.SERVER }} 
```

**TODO: This if condition implementation is up to discussion.** 

See the paragraph below for a rudimentary approach:

The immediate if condition holds the most importance and only effects the current if condition. If there is no if condition for a step, it defaults to its parent's if condition (if that parent's if condition is not defined, it takes the grandparent's if condition, and so on)

In the example above, the `always()` condition in the `foo` step affects the composite action steps `foo2` and `foo3`. Since `foo3` has an if condition defined, it overrides its parent condition so `foo3` will have an if condition `success()`. Let's say that the `foo2` step fails, then `foo3` will not run since a past step failed and its if condition is `succcess()` but `foo4` will run because its if condition is inherited from `foo` which is `always()`.   
    
### Timeout-minutes
Example `user/test/composite-action.yml`:
```
using: 'composite' 
steps: 
  - id: foo1
    run: echo test 1
    timeout-minutes: 10
  - id: foo2
    run: echo test 2
  - id: foo3
    run: echo test 3
    timeout-minutes: 10
```

Example `workflow.yml`:
```
steps: 
  - id: bar
    uses: user/test@v1
    timeout-minutes: 50
```
**TODO: This timeout-minutes condition implementation is up to discussion.** 

A composite action in its entirety is a job. You can set both timeout-minutes for the whole composite action or its steps as long as the the sum of the `timeout-minutes` for each composite action step that has the attribute `timeout-minutes` is less than or equals to `timeout-minutes` for the composite action. There is no default timeout-minutes for each composite action step. 

If the time taken for any of the steps in combination or individually exceed the whole composite action `timeout-minutes` attribute, the whole job will fail. If an individual step exceeds its own `timeout-minutes` attribute but the total time that has been used including this step is below the overall composite action `timeout-minutes`, the individual step will fail but the rest of the steps will run. 

For reference, in the example above, if the composite step `foo1` takes 11 minutes to run, that step will fail but the rest of the steps, `foo1` and `foo2`, will proceed as long as their total runtime with the previous failed `foo1` action is less than the composite action's `timeout-minutes` (50 minutes). If the composite step `foo2` takes 51 minutes to run, it will cause the whole composite action job to fail. I

The rationale behind this is that users can configure their steps with the `if` condition to conditionally set how steps rely on each other. Due to the additional capabilities that are offered with combining `timeout-minutes` and/or `if`, we wanted the `timeout-minutes` condition to be as dumb as possible and not effect other steps. 

[Usage limits still apply](https://help.github.com/en/actions/reference/workflow-syntax-for-github-actions?query=if%28%29#usage-limits)


### Continue-on-error

**TODO: This continue-on-error condition implementation is up to discussion.** 
For now, if `continue-on-error` is set to `true` for any of the composite action steps, the composite action job proceeds with the next step and ignores that failure. 

### Visualizing Composite Action in the GitHub Actions UI
We want all the composite action's steps to be condensed into the original composite action node. 

Here is a visual represenation of the [first example](#Steps)
```
| composite_action_node |
    | echo hello world 1 | 
    | echo hello world 2 |
| echo hello world 3 | 
| echo hello world 4 |

```


## Conclusion
This ADR lays the framework for eventually supporting nested Composite Actions within Composite Actions. This ADR allows for users to run multiple run steps within a GitHub Composite Action with the support of inputs, outputs, environment, and context for use in any steps as well as the if, timeout-minutes, and the continue-on-error attributes for each Composite Action step. 
