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
  - run: echo hello ${{ steps.foo.inputs.your_name }} 2
```

Example Output:
```
hello Octocat
Error
```
We plan to use inputs for Composite Actions similar to how parameters are used in programming languages like Python in terms of [lexical scoping](https://inst.eecs.berkeley.edu/~cs61a/fa19/assets/slides/29-Tail_Calls_full.pdf). In Python, let's say you have "Function A", parameters in this function are stored as local variables in this function frame. Let's say we have a "Function B" that a parent frame "Function A" (aka "Function B" is called in "Function A") will have access to those input variables unless its overwritten locally in the body of this child function.

Similar to how Python treats its parameters, for our case, each input variable in the composite action is viewable in its own scope as well as its descendants' scope (when we have nested composite functions). On the flip side, a child action cannot view its ancestors' inputs. Similarly, as seen in the last line in the example output, in the workflow file, it will not have access to the action's `inputs` attribute.

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
Each of the output variables from the composite action is viewable from the workflow file that uses the composite action. In other words, every child action output(s) is viewable only by its immediete parent.

Moreover, the output ids are only accessible within the scope where it was defined. In the example above, note that in our `workflow.yml` file, it should not have access to output id (i.e. `my-output`). For example, in the `workflow.yml` file, you can't run `foo.steps.my-step.my-output`.

### Context
Similar to the workflow file, the composite action has access to the [same context objects](https://help.github.com/en/actions/reference/context-and-expression-syntax-for-github-actions#contexts) (ex: `github`, `env`, `strategy`). 

### Environment
The environment variables will flow from the parent node to its children node. More concretely, whatever workflow/action calls a composite action, that composite action has access to whatever environment variables its caller workflow/action has. Nevertheless, the composite action can append its own environment variables or overwrite its parent's environment variables. 



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
