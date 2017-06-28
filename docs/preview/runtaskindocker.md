
# Run Job Inside Container (PREVIEW for Linux Agent)

## Overview

The feature is to allow a given build/release job executed inside a container on a Linux build/release agent.

## Benefits

### Run build/release on more platforms

Today, the agent is only supported on Windows, OSX, Ubuntu 14, Ubuntu16 and RedHat7, which means your build/release job are also restricted by this. With this feature, the restriction is only scope to the machine that host the agent, you can use any docker image you want for any platform as long as the platform support our task execution engine, ex. Node.js.

### All other benefits of using container

## Job execution flow changes

### Today

```
Everything happened in agent Host Machine                    

Job
 | 
 | Come from VSTS/TFS
 |
 --> Agent.Listener
            | 
            | Launch worker to run the job   
            |
            --> Agent.Worker
                     |
                     | Launch different handler for each task
                     |
                     --> Task Execution Engine (Node, PowerShell, etc.)
```

### Use container execution
```
Agent Host Machine
Job
 | 
 | Come from VSTS/TFS
 |
 --> Agent.Listener
            | 
            | Launch worker to run the job   
            |
            --> Agent.Worker
                     |
                     |                  Container
         --------------------------------------------------------------------------------    
         |           |                                                                  |  
         |           | Launch different handler for each task inside container          |
         |           |                                                                  |
         |           --> Docker EXEC Task Execution Engine (Node, PowerShell, etc.)     |
         |                                                                              |
         --------------------------------------------------------------------------------
```

### How to try out

Install Docker into your agent host machine. [Instructions](https://docs.docker.com/engine/installation)

Make sure you can [Manage Docker as a non-root user](https://docs.docker.com/engine/installation/linux/linux-postinstall/) on your agent host machine, since agent won't call any `Docker` commands with `sudo`.

Add definition variable `_PREVIEW_VSTS_DOCKER_IMAGE` to your build/release definition to point to a docker image.  

```
Ex:
    _PREVIEW_VSTS_DOCKER_IMAGE = ubuntu:16.04
```

That's it, you can queue build as normal.

### Request for Feedback

We log every `Docker` commands we ran during a build/release job, if you find any command we ran is wrong or any improvement we can make, feel free to create an issue and let us know.