TODO: Change file name to represent the correct PR number (PR is not created yet for this ADR)

# ADR 0549: Composite Run Steps

**Date**: 2020-06-17

**Status**: Proposed

**Relevant PR**: https://github.com/actions/runner/pull/549

## Context
Customers want to be able to compose actions from actions (ex: https://github.com/actions/runner/issues/438)

An important step towards meeting this goal is to build in functionality for actions where users can simply execute any number of steps. 

## Decision
**In this ADR, we only support running multiple steps in an Action.** In doing so, we build in support for mapping and flowing the inputs, outputs, and env variables (ex: All nested steps should have access to its parents' input variables and nested steps can overwrite the input variables).

### Steps
Example `user/composite/action.yml`
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
    - uses: user/composite@v1
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
Example `user/composite/action.yml`:
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
    uses: user/composite@v1
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
Example `user/composite/action.yml`:
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
    uses: user/composite@v1
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
Example `user/composite/action.yml`:
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
    env:
      NAME2: test3
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
NAME2 test3
Server development
Server production
```

We plan to use environment variables for Composite Actions similar to the parent/child relationship between nested function calls in programming languages like Python in terms of [lexical scoping](https://inst.eecs.berkeley.edu/~cs61a/fa19/assets/slides/29-Tail_Calls_full.pdf). In Python, let's say you have `functionA` that has local variables called `a` and `b` in this function frame. Let's say we have a `functionB` whose parent frame is `functionA` and has local variable `a` (aka `functionB` is called and defined in `functionA`). `functionB` will have access to its parent input variables that are not overwritten in the local scope (`a`) as well as its own local variable `b`. [Visual Example](http://www.pythontutor.com/visualize.html#code=def%20functionA%28%29%3A%0A%20%20%20%20a%20%3D%201%0A%20%20%20%20b%20%3D%202%0A%20%20%20%20def%20functionB%28%29%3A%0A%20%20%20%20%20%20%20%20b%20%3D%203%0A%20%20%20%20%20%20%20%20print%28%22a%22,%20a%29%0A%20%20%20%20%20%20%20%20print%28%22b%22,%20b%29%0A%20%20%20%20%20%20%20%20return%20b%0A%20%20%20%20return%20functionB%28%29%0A%0A%0A%0AfunctionA%28%29&cumulative=false&curInstr=14&heapPrimitives=nevernest&mode=display&origin=opt-frontend.js&py=3&rawInputLstJSON=%5B%5D&textReferences=false) 

Similar to the above logic, the environment variables will flow from the parent node to its children node. More concretely, whatever workflow/action calls a composite action, that composite action has access to whatever environment variables its caller workflow/action has. Note that the composite action can append its own environment variables or overwrite its parent's environment variables. 


### If Condition

Example `user/composite/action.yml`:
```
steps:
  - run: exit 1
  - uses: user/composite@v1  # <--- this will run, as it's marked as always runing
    if: always()
```

Example `workflow.yml`:
```
steps:
  - run: echo "just succeeding"
  - run: echo "I will run, as my current scope is succeeding"
    if: success()
  - run: exit 1
  - run: echo "I will not run, as my current scope is now failing"
```

**TODO: This if condition implementation is up to discussion.
Discussions: https://github.com/actions/runner/pull/554#discussion_r443661891, ...** 

See the paragraph below for a rudimentary approach (thank you to @cybojenix for the idea, example, and explanation for this approach):

The `if` statement in the parent (in the example above, this is the `workflow.yml`) shows whether or not we should run the composite action. So, our composite action will run since the `if` condition for running the composite action is `always()`.

**Note that the if condition on the parent does not propogate to the rest of its children though.**

In the child action (in this example, this is the `action.yml`), it starts with a clean slate (in other words, no imposing if conditions). Similar to the logic in the paragraph above, `echo "I will run, as my current scope is succeeding"` will run since the `if` condition checks if the previous steps **within this composite action** has not failed. `run: echo "I will not run, as my current scope is now failing"` will not run since the previous step resulted in an error and by default, the if expression is set to `success()` if the if condition is not set for a step.


#### Exposing Parent's If Condition to Children Via a Variable
It would be nice to have a way to access information from a parent's if condition. We could have a parent variable that is contained in the context similar to other context variables `github`, `strategy`, etc.:

Example `user/composite/action.yml`
steps:
  - run: echo "preparing the slack bot..."  # <--- This will run, as nothing has failed within the composite yet
  - run: slack.post("All builds passing, ready for a deploy")  # <-- this will not run, as the parent fails
    if: ${{ parent.success() }}
  - run: slack.post("A failure has happened, fix things now", alert=true)  # <--- This will run, as the parent fails
    if: ${{ parent.failure() }}

Example `workflow.yml`
steps:
  - run: exit 1
  - uses: user/composite@v1
    if: always()

    
### Timeout-minutes
Example `user/composite/action.yml`:
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
For now, if `continue-on-error` is set to `true` for any of the composite action steps, the composite action job proceeds to the next step and ignores that failure. 

Note, that since the composite action is not a workflow, it does not have jobs and thus it is not within scope at the moment to support something like `strategy` with `continue-on-error` as seen in this [example](https://help.github.com/en/actions/reference/workflow-syntax-for-github-actions#jobsjob_idcontinue-on-error).

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
