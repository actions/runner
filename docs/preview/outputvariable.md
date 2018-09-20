
# Output Variables

## Overview

The feature is to allow a given task to publish a set of variables back to server, so the variables can be used in downstream jobs. All output variables from the same task instance have its own namespace, so they don’t overlap each other within the job.

## Changes for task author and ad-hoc script owner

There will be 2 different ways to produce output variables.

### For task author:

Declare a new section in the task.json to indicate which variables will get published.
```	JSON
"OutputVariables": [{
    "name" : "MY_OUTPUTVARIABLE_1",
    "description" : "This is the description of my output variable."
}, 
{
    "name" : "MY_OUTPUTVARIABLE_2",
    "description" : "Description can contains markdown [vsts-tasks](https://github.com/microsoft/vsts-tasks)"
}]
```
Task author doesn't have to change their task script if the script already uses `##vso[task.setvariable]` to set variables.

Agent will, based on the `OutputVariables` section from task.json, determine which variables need to be published and patch timeline record with the output variables.

The `OutputVariables` section will also be used as intellisense hint in the definition editor.

### For Ad-hoc script:

Use `##vso` command to publish output variables. We will add a new parameter `isOutput=true/false` to the set variable command for setting output variables.

```
##vso[task.setvariable variable=foo;isSecret=false;isOutput=true;]value
```

In definition editor, downstream tasks won't get variable name intellisense for output variables that were published by an ad-hoc script.

## Server side changes:

The output variables will be stored as part of timeline record for each task instance, just like task issues. In this way, we can distinguish the same output variable published by 2 instances of same task within a job. For example, the definition has 2 `AcquireToken` tasks which publish an output variable called `AuthToken`.

We will also introduce `ref name` to each task instance and each job, the `ref name` will be used as namespace for the output variables, so the downstream task can use the `ref name` to pick the right variable. 

```
Example: 
$(JobBuild1.XCode1.Variable1) in downstream jobs. 
$(XCode1.Variable1) in downstream tasks within the same job. 
```

### Ref name

A new `ref name` will be generated when a new task is added to the definition.

The generated `ref name` looks like this:
```
{TaskName}_{Number}
```

Customer can change it, but we will give a warning that they need to change all downstream references. 

### TODO 

I am not sure how are we going to generate Job ref name, since we don’t have job chaining at this point. 

It should be something like:
```
{DefinitionName}_{JobName}
```

### Definition editor intellisense

We need to add intellisense for typing variables in definition editor, since all output variables are defined in task.json.

## Agent changes

Today, the timeline record update is best effort on each update call, and continue retrying on failure till the end of the job. When the job finishes, any remaining errors won’t affect the job result. 

We need to change this since output variables are critical to downstream tasks/jobs, we don’t want a downstream job to fail because of some missing output variable, the failure will be expensive on big fanin fanout scenario. 

We should fail the job if it fails to produce output variables when job finishes.

## Compatibility

Since task author needs to change their task.json in order to use the output variables feature, we suggest they should bump the major version in this case. since definition has major version locking, all existing definitions should continue working without any problems. 

When definition owner decides to consume the new major version, they will fix the downstream task’s input to use the right name of the output variables anyway. Because of this, we should not have any compatibility problems to worry about.

## Examples

1. Consuming output variables within the same job
```
    Job_1
    |
    |__TaskA_1
    |
    |__TaskA_2
    |
    |__TaskB_1
    |
    |__TaskB_2
```

- `TaskA_1` and `TaskA_2` are two instances of same task `TaskA`.
- `TaskA` will, based on its inputs, acquire a token from some service and produce an output variable `AuthToken`. 
- `TaskA_1`, `TaskA_2` are also the ref names for the tasks.

- `TaskB_1` and `TaskB_2` are two instances of same task `TaskB`.
- `TaskB` takes credential as input and makes a rest call to some endpoint.
- `TaskB_1`, `TaskB_2` are also the ref names for the tasks.

When consuming an output variable in `TaskB_1` and `TaskB_2`, you must provide the `ref name` as namespace. Agent won’t set `$(AuthToken)` as long as `$(AuthToken)` is an output variable. 

```
In the above example:
    TaskB_1’s input will be $(TaskA_1.AuthToken).
    TaskB_2’s input will be $(TaskA_2.AuthToken). 
```

2. Consuming output variables in a downstream job
```
    Job_1                       Job_2
    |                           |
    |__TaskA_1                  |__TaskB_1
    |                           |
    |__TaskA_2                  |__TaskB_2
```

- Same `TaskA` and `TaskB`, but this time `TaskB_1` and `TaskB_2` are in a downstream job. 
```
In this example:
TaskB_1’s input will be $(Job_1.TaskA_1.AuthToken).
TaskB_2’s input will be $(Job_1.TaskA_2.AuthToken). 
```
