TODO: Change file name to represent the correct PR number (PR is not created yet for this ADR)

# ADR 0549: Composite Run Steps

**Date**: 2020-06-17

**Status**: In Development

## Context
Customers want to be able to compose actions from actions (ex: https://github.com/actions/runner/issues/438)

An important step towards meeting this goal is to build in functionality for actions where users can simply execute any number of steps. 

## Decision
In this ADR, we support running multiple steps in an Action. In doing so, we build in support for mapping and flowing the inputs, outputs, and env variables (ex: All nested steps should have access to its parents' input variables and nested steps can overwrite the input variables).

### Steps
We add a token called "composite" which allows our Runner code to process composite actions. By invoking "using: composite", our Runner code then processes the "steps" attribute, converts this template code to a list of steps, and finally runs each run step 
sequentially. 
Example `user/test/action.yml`
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
    - uses: user/test@v5
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
Example `action.yml`:
```
using: 'composite' 
inputs:
  your_name:
    description: 'Your name'
    default: 'Ethan'
steps: 
  - run: echo hello ${{ inputs.your_name }}
```
// inputs:
//   - name: your_name
//     default: John Doe
// steps:
//   - run: echo hello ${{ inputs.your_name }}

#### Mapping Inputs
We plan to correctly set the scope of each input variable by loading inputs from the parent action. Then, if there are any local 

Along those same lines, we don't want the workflow file to have access to the inner inputs. 

to have access to the ste

### Outputs
// outputs:
//   ...
// example usage (scoped namespaces)
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
