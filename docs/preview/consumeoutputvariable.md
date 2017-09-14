
# How to Set/Publish Output Variables in Task

## Overview

The feature is to allow a given task to publish a set of variables back to server that scope to the current task. All output variables from the same task instance has its own namespace, so they don’t overlap each other within the job. [Full Design Doc](./outputvariable.md)

## Step to consume this feature

### Minimum agent version

You need demand minimum agent version to 2.119.1 in your Task.json, since 2.119.1 agent is the first version agent that has the ability to set and publish output variables.  

### Bump task major version

Since output variable involved a new concept of reference name for task instance in Build/Release definition, and publish output variable normally will change the way you consume the variable in downstream tasks. So, we recommend to bump your task's major version, so definition owner has a chance to provide a meaningful reference name for task they have and change how downstream tasks consume the output variables in definition editor.

### Define output variable in task.json

There is no required changes to your task's implementation for using this feature, the only thing you need to do is update your task.json.
Here is an example:
```	JSON
"OutputVariables": [
    {
        "name": "MY_OUTPUTVARIABLE_1",
        "description": "This is the description of my output variable."
    }, 
    {
        "name": "MY_OUTPUTVARIABLE_2",
        "description": "Description can contains markdown [vsts-tasks](https://github.com/microsoft/vsts-tasks)"
    }
]
```

The agent will base on the `OutputVariables` section in your task.json set and publish the variable along with the reference name of the task instance.

## Example

I have a task called `DeployVM`, the task will take a image as input, deploy a VM use that image, and set a variable `VMPublicIP` point to the public IP of the VM using `##vso[task.setvariable]` command, the task current version `1.2.0`  

In my Build/Release definition, I add a `DeployVM` task, then a `CmdLine` task to ping the `VMPublicIP` to check whether the VM is up. However, I can not have multiple `DeployVM` tasks in a single definition, since they set the same variable `VMPublicIP`, that variable will get overwrite over and over again.

I want to use the output variable feature for solving this problem.  
Here is what I will do:

1. Add `"minimumAgentVersion" : "2.119.1"` to task.json
1. Change task version to `2.0.0` a new major version
3. Add following to task.json
```JSON
    "OutputVariables": [{
        "name": "VMPublicIP",
        "description": "This is the public IP of the deployed VM."
    }]
```
4. Publish the new version task.
5. Now I can do something like this for my definition to deploy multiple VM using different image and ping them.
```
Definition
|
|
|__ DeployVM task 2.0 (this step takes Ubuntu16 image as input. I set the reference name for this step to be "DeployUbuntu16")
|
|
|__ DeployVM task 2.0 (this step takes Windows10 image as input. I set the reference name for this step to be "DeployWindows10")
|
|
|__ CmdLine task with input "ping $(DeployUbuntu16.VMPublicIP)"
|
|
|__ CmdLine task with input "ping $(DeployWindows10.VMPublicIP)"
```