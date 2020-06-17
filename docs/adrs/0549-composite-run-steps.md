TODO: Change file name to represent the correct PR number (PR is not created yet for this ADR)

# ADR 0549: Composite Run Steps

**Date**: 2020-06-17

**Status**: Proposed

## Context
Customers want to be able to compose actions from actions (ex: https://github.com/actions/runner/issues/438)

An important step towards meeting this goal is to build in functionality for actions where users can simply execute any number of steps. 

## Decision
In this ADR, we support running multiple steps in an Action. In doing so, we build in support for mapping and flowing the inputs, outputs, and env variables (ex: All nested steps should have access to its parents' input variables and nested steps can overwrite the input variables).

### Steps
We add a token called "composite" which allows our Runner code to process composite actions. By invoking "using: composite", our Runner code then processes the "steps" attribute, converts this template code to a list of steps, and finally runs each run step 
sequentially. 
Example `user/test/composite-action.yml`
```
...
using: 'composite' 
steps:
  - run: echo hello world 1
  - run: echo hello world 2
```
Example `workflow.yml`
```
...
jobs:
  build:
    runs-on: self-hosted
    steps:
    - uses: actions/checkout@v2
    - uses: user/test@v1
    - name: workflow step 1
      run: echo hello world 3
    - name: workflow step 2
      run: echo hello world 4
```
Example Output
```
echo hello world 1
echo hello world 2
echo hello world 3
echo hello world 4
```
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
  - run: echo hello ${{ steps.inputs.your_name }} 2
```

Example Output:
```
hello Octocat
ERROR
```
Each input variable in the composite action is only viewable to those in the same scope as composite action or is a child of the composite action such as the composite steps (and eventually nested actions).

As seen in the last output message, in the workflow file, you cannot access the inputs of the composite action.

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
  - run: echo woahhhhhh ${{ steps.foo.outputs.bar }} 
```

Example Output:
```
hello Octocat
woahhhhhh hello Octocat
```
Each of the output variables from the composite action is viewable from the workflow file that uses the composite action. 

In the example above, note that in our `workflow.yml` file, it should not have access to all of the input variables (i.e. `your_name`) nor should it have access to `my-step`. For example, in the `workflow.yml` file, you can't run `my-step.outputs.bar`

### Context
Similar to the workflow file, the composite action has access to the [same context objects](https://help.github.com/en/actions/reference/context-and-expression-syntax-for-github-actions#contexts) (ex: `github`, `env`, `strategy`). 

### If condition?
TODO: Figure out: What does it mean if the composite step is "always()" but inner step is not? What should the behavior be:
steps:
  - uses: my-composite-action@v1
    if: cancel()
    
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
