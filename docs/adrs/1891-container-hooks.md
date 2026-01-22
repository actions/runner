# ADR 0000: Container Hooks

**Date**: 2022-05-12

**Status**: Accepted

# Background

[Job Hooks](https://github.com/actions/runner/blob/main/docs/adrs/1751-runner-job-hooks.md) have given users the ability to customize how their self hosted runners run a job.
Users also want the ability to customize how they run containers during the scope of the job, rather then being locked into the docker implementation we have in the runner. They may want to use podman, kubernetes, or even change the docker commands we run. 
We should give them that option, and publish examples how they can create their own hooks.

# Guiding Principles
- **Extensibility** is the focus, we need to make sure we are flexible enough to cover current and future scenarios, even at the cost of making it harder to utilize these hooks
- Args should map **directly** to yaml values provided by the user. 
  - For example, the current runner overrides `HOME`, we can do that in the hook, but we shouldn't pass that hook as an ENV with the other env's the user has set, as that is not user input, it is how the runner invokes containers

## Interface
- You will set the variable `ACTIONS_RUNNER_CONTAINER_HOOKS=/Users/foo/runner/hooks.js` which is the entrypoint to your hook handler.
  - There is no partial opt in, you must handle every hook 
- We will pass a command and some args via `stdin`
- An exit code of 0 is a success, every other exit code is a failure
- We will support the same runner commands we support in [Job Hooks](https://github.com/actions/runner/blob/main/docs/adrs/1751-runner-job-hooks.md)
- On timeout, we will send a sigint to your process. If you fail to terminate within a reasonable amount of time, we will send a sigkill, and eventually kill the process tree.

An example input looks like
```json
{
  "command": "job_cleanup",
  "responseFile": "/users/thboop/runner/_work/{guid}.json",
  "args": {},
  "state": 
  {
      "id": "82e8219701fe096a35941d869cf8d71af1d943b5d3bdd718850fb87ac3042480"
  }
}
```

`command` is the command we expect you to invoke
`responseFile` is the file you need to write your output to, if the command has output
`args` are the specific arguments the command needs
`state` is a json blog you can pass around to maintain your state, this is covered in more details below.

### Writing responses to a file
All text written to stdout or stderr should appear in the job or step logs. With that in mind, we support a few ways to actually return data:
1. Wrapping the json in some unique tag and processing it like we do commands
2. Writing to a file

For 1, users typically view logging information as a safe action, so we worry someone accidentally logging unsantized information and causing unexpected or un-secure behavior. We eventually plan to move off of stdout/stderr style commands in favor of a runner cli. 
Investing in this area doesn't make a lot of sense at this time.

While writing to a file to communicate isn't the most ideal pattern, its an existing pattern in the runner and serves us well, so lets reuse it.

### Output
Your output must be correctly formatted json. An example output looks like:

```
{
  "state": {},
  "context"
  {
    "container" : 
    {
      "id": "82e8219701fe096a35941d869cf8d71af1d943b5d3bdd718850fb87ac3042480"
      "network": "github_network_53269bd575974817b43f4733536b200c"
    }
    "services": {
      "redis": {
        "id": "60972d9aa486605e66b0dad4abb638dc3d9116f566579e418166eedb8abb9105",
        "ports": {
          "8080": "8080"
        },
      "network": "github_network_53269bd575974817b43f4733536b200c"
    }
  }
  "alpine: true,
}
```

`state` is a unique field any command can return. If it is not empty, we will store the state for you and pass it into all future commands. You can overwrite it by having the next hook invoked return a unique state.

Other fields are dependent upon the command being run.

### Versioning
We will not version these hooks at launch. If needed, we can always major version split these hooks in the future. We will ship in Beta to allow for breaking changes for a few months.

### The Job Context
The [job context](https://docs.github.com/en/actions/learn-github-actions/contexts#example-contents-of-the-job-context) currently has a variety of fields that correspond to containers. We should consider allowing hooks to populate new fields in the job context. That is out of scope for this original release however.

## Hooks
Hooks are to be implemented at a very high level, and map to actions the runner does, rather then specific docker actions like `docker build` or `docker create`. By mapping to runner actions, we create a very extensible framework that is flexible enough to solve any user concerns in the future. By providing first party implementations, we give users easy starting points to customize specific hooks (like `docker build`) without having to write full blown solutions.

The other would be to provide hooks that mirror every docker call we make, and expose more hooks to help support k8s users, with the expectation that users may have to no-op on multiple hooks if they don't correspond to our use case.

Why we don't want to go that way
- It feels clunky, users need to understand which hooks they need to implement and which they can ignore, which isn't a great UX
- It doesn't scale well, I don't want to build a solution where we may need to add more hooks, by mapping to runner actions, updating hooks is a painful experience for users
- Its overwhelming, its easier to tell users to build 4 hooks and track data themselves, rather then 16 hooks where the runner needs certain information and then needs to provide that information back into each hook. If we expose `Container Create`, you need to return the container you created, then we do `container run` which uses that container. If we just give you an image and say create and run this container, you don't need to store the container id in the runner, and it maps better to k8s scenarios where we don't really have container ids.

### Prepare_job hook
The `prepare_job` hook is called when a job is started. We pass in any job or service containers the job has. We expect that you:
- Prune anything from previous jobs if needed
- Create a network if needed
- Pull the job and service containers
- Start the job container
- Start the service containers
- Write to the response file some information we need
  - Required: if the container is alpine, otherwise x64
  - Optional: any context fields you want to set on the job context, otherwise they will be unavailable for users to use
- Return 0 when the health checks have succeeded and the job/service containers are started

This hook will **always** be called if you have container hooks enabled, even if no service or job containers exist in the job. This allows you to fail the job or implement a default job container if you want to and no job container has been provided.


<details>
<summary>Example Input</summary>
<br>
  
```
{
  "command": "prepare_job",
  "responseFile": "/users/thboop/runner/_work/{guid}.json",
  "state": {},
  "args": 
  {
    "jobContainer": {
      "image": "node:14.16",
      "workingDirectory": "/__w/thboop-test2/thboop-test2",
      "createOptions": "--cpus 1",
      "environmentVariables": {
        "NODE_ENV": "development"
      },
      "userMountVolumes:[
        {
          "sourceVolumePath": "my_docker_volume",
          "targetVolumePath": "/volume_mount",
          "readOnly": false
        },
      ],
      "mountVolumes": [
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work",
          "targetVolumePath": "/__w",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/externals",
          "targetVolumePath": "/__e",
          "readOnly": true
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_temp",
          "targetVolumePath": "/__w/_temp",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_actions",
          "targetVolumePath": "/__w/_actions",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_tool",
          "targetVolumePath": "/__w/_tool",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_temp/_github_home",
          "targetVolumePath": "/github/home",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_temp/_github_workflow",
          "targetVolumePath": "/github/workflow",
          "readOnly": false
        }
      ],
      "registry": {
        "username": "foo",
        "password": "bar",
        "serverUrl": "https://index.docker.io/v1"
      },
      "portMappings": [ "8080:80/tcp", "8080:80/udp" ]
    },
    "services": [
      {
        "contextName": "redis",
        "image": "redis",
        "createOptions": "--cpus 1",
        "environmentVariables": {},
        "mountVolumes": [],
        "portMappings": [ "8080:80/tcp", "8080:80/udp" ]
        "registry": {
          "username": "foo",
          "password": "bar",
          "serverUrl": "https://index.docker.io/v1"
        }
      }
    ]
  }
}
```

</details>

<details>
<summary>Field Descriptions</summary>
<br>

```
Arg Fields:

jobContainer: **Optional** An Object containing information about the specified job container
  "image": **Required** A string containing the docker image
  "workingDirectory": **Required** A string containing the absolute path of the working directory
  "createOptions": **Optional** The optional create options specified in the [YAML](https://docs.github.com/en/actions/using-jobs/running-jobs-in-a-container#example-running-a-job-within-a-container)
  "environmentVariables": **Optional** A map of key value env's to set
  "userMountVolumes: ** Optional** an array of user mount volumes set in the [YAML](https://docs.github.com/en/actions/using-jobs/running-jobs-in-a-container#example-running-a-job-within-a-container)
    "sourceVolumePath": **Required** The source path to the volume to be mounted into the docker container
    "targetVolumePath": **Required** The target path to the volume to be mounted into the docker container
    "readOnly": false **Required** whether or not the mount should be read only
  "mountVolumes": **Required** an array of mounts to mount into the container, same fields as above
    "sourceVolumePath": **Required** The source path to the volume to be mounted into the docker container
    "targetVolumePath": **Required** The target path to the volume to be mounted into the docker container
    "readOnly": false **Required** whether or not the mount should be read only
  "registry" **Optional** docker registry credentials to use when using a private container registry
    "username": **Optional** the username
    "password": **Optional** the password
    "serverUrl": **Optional** the registry url
  "portMappings": **Optional** an array of source:target ports to map into the container
 
"services": an array of service containers to spin up
  "contextName": **Required** the name of the service in the Job context
  "image": **Required** A string containing the docker image
  "createOptions": **Optional** The optional create options specified in the [YAML](https://docs.github.com/en/actions/using-jobs/running-jobs-in-a-container#example-running-a-job-within-a-container)
  "environmentVariables": **Optional** A map of key value env's to set
  "mountVolumes": **Required** an array of mounts to mount into the container, same fields as above
    "sourceVolumePath": **Required** The source path to the volume to be mounted into the docker container
    "targetVolumePath": **Required** The target path to the volume to be mounted into the docker container
    "readOnly": false **Required** whether or not the mount should be read only
  "registry" **Optional** docker registry credentials to use when using a private container registry
    "username": **Optional** the username
    "password": **Optional** the password
    "serverUrl": **Optional** the registry url
  "portMappings": **Optional** an array of source:target ports to map into the container
```

</details>

<details>
<summary>Example Output</summary>
<br>

```
{
  "state": 
  {
    "network": "github_network_53269bd575974817b43f4733536b200c",
    "jobContainer" : "82e8219701fe096a35941d869cf8d71af1d943b5d3bdd718850fb87ac3042480",
    "serviceContainers": 
    {
      "redis": "60972d9aa486605e66b0dad4abb638dc3d9116f566579e418166eedb8abb9105"
    }
  },
  "context"
  {
    "container" : 
    {
      "id": "82e8219701fe096a35941d869cf8d71af1d943b5d3bdd718850fb87ac3042480"
      "network": "github_network_53269bd575974817b43f4733536b200c"
    }
    "services": {
      "redis": {
        "id": "60972d9aa486605e66b0dad4abb638dc3d9116f566579e418166eedb8abb9105",
        "ports": {
          "8080": "8080"
        },
      "network": "github_network_53269bd575974817b43f4733536b200c"
    }
  }
  "alpine: true,
}
```

</details>


### Cleanup Job
The `cleanup_job` hook is called at the end of a job and expects you to:
- Stop any running service or job containers (or the equivalent pod)
- Stop the network (if one exists)
- Delete any job or service containers (or the equivalent pod)
- Delete the network (if one exists)
- Cleanup anything else that was created for the run

Its input looks like

<details>
<summary>Example Input</summary>
<br>

```
  "command": "cleanup_job",
  "responseFile": null,
  "state":
  {
    "network": "github_network_53269bd575974817b43f4733536b200c",
    "jobContainer" : "82e8219701fe096a35941d869cf8d71af1d943b5d3bdd718850fb87ac3042480",
    "serviceContainers": 
    {
      "redis": "60972d9aa486605e66b0dad4abb638dc3d9116f566579e418166eedb8abb9105"
    }
  }
  "args": {}
```
  
</details>

No args are provided.
  
No output is expected.


### Run Container Step
The `run_container_step` is called once per container action in your job and expects you to:
- Pull or build the required container (or fail if you cannot)
- Run the container action and return the exit code of the container
- Stream any step logs output to stdout and stderr
- Cleanup the container after it executes

<details>
<summary>Example Input for Image</summary>
<br>

```
  "command": "run_container_step",
  "responseFile": null,
  "state":
  {
    "network": "github_network_53269bd575974817b43f4733536b200c",
    "jobContainer" : "82e8219701fe096a35941d869cf8d71af1d943b5d3bdd718850fb87ac3042480",
    "serviceContainers": 
    {
      "redis": "60972d9aa486605e66b0dad4abb638dc3d9116f566579e418166eedb8abb9105"
    }
  }
  "args":      
  {
      "image": "node:14.16",
      "dockerfile": null,
      "entryPointArgs": ["-f", "/dev/null"],
      "entryPoint": "tail",
      "workingDirectory": "/__w/thboop-test2/thboop-test2",
      "createOptions": "--cpus 1",
      "environmentVariables": {
        "NODE_ENV": "development"
      },
      "prependPath":["/foo/bar", "bar/foo"]
      "userMountVolumes:[
        {
          "sourceVolumePath": "my_docker_volume",
          "targetVolumePath": "/volume_mount",
          "readOnly": false
        },
      ],
      "mountVolumes": [
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work",
          "targetVolumePath": "/__w",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/externals",
          "targetVolumePath": "/__e",
          "readOnly": true
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_temp",
          "targetVolumePath": "/__w/_temp",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_actions",
          "targetVolumePath": "/__w/_actions",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_tool",
          "targetVolumePath": "/__w/_tool",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_temp/_github_home",
          "targetVolumePath": "/github/home",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_temp/_github_workflow",
          "targetVolumePath": "/github/workflow",
          "readOnly": false
        }
      ],
      "registry": null,
      "portMappings": { "80": "801" }
    },
```
  
</details>


<details>
<summary>Example Input for dockerfile</summary>
<br>

```
  "command": "run_container_step",
  "responseFile": null,
  "state":
  {
    "network": "github_network_53269bd575974817b43f4733536b200c",
    "jobContainer" : "82e8219701fe096a35941d869cf8d71af1d943b5d3bdd718850fb87ac3042480",
    "services": 
    {
      "redis": "60972d9aa486605e66b0dad4abb638dc3d9116f566579e418166eedb8abb9105"
    }
  }
  "args":      
  {
      "image": null,
      "dockerfile": /__w/_actions/foo/dockerfile,
      "entryPointArgs": ["hello world"],
      "entryPoint": "echo",
      "workingDirectory": "/__w/thboop-test2/thboop-test2",
      "createOptions": "--cpus 1",
      "environmentVariables": {
        "NODE_ENV": "development"
      },
      "prependPath":["/foo/bar", "bar/foo"]
      "userMountVolumes:[
        {
          "sourceVolumePath": "my_docker_volume",
          "targetVolumePath": "/volume_mount",
          "readOnly": false
        },
      ],      
      "mountVolumes": [
        {
          "sourceVolumePath": "my_docker_volume",
          "targetVolumePath": "/volume_mount",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work",
          "targetVolumePath": "/__w",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/externals",
          "targetVolumePath": "/__e",
          "readOnly": true
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_temp",
          "targetVolumePath": "/__w/_temp",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_actions",
          "targetVolumePath": "/__w/_actions",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_tool",
          "targetVolumePath": "/__w/_tool",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_temp/_github_home",
          "targetVolumePath": "/github/home",
          "readOnly": false
        },
        {
          "sourceVolumePath": "/home/thomas/git/runner/_layout/_work/_temp/_github_workflow",
          "targetVolumePath": "/github/workflow",
          "readOnly": false
        }
      ],
      "registry": null,
      "portMappings": [ "8080:80/tcp", "8080:80/udp" ]
    },
  }
```
  
</details>


<details>
<summary>Field Descriptions</summary>
<br>

```
Arg Fields:


"image": **Optional** A string containing the docker image. Otherwise a dockerfile must be provided
"dockerfile": **Optional** A string containing the path to the dockerfile, otherwise an image must be provided
"entryPointArgs": **Optional** A list containing the entry point args
"entryPoint": **Optional** The container entry point to use if the default image entrypoint should be overwritten
"workingDirectory": **Required** A string containing the absolute path of the working directory
"createOptions": **Optional** The optional create options specified in the [YAML](https://docs.github.com/en/actions/using-jobs/running-jobs-in-a-container#example-running-a-job-within-a-container)
"environmentVariables": **Optional** A map of key value env's to set
"prependPath": **Optional** an array of additional paths to prepend to the $PATH variable
"userMountVolumes: ** Optional** an array of user mount volumes set in the [YAML](https://docs.github.com/en/actions/using-jobs/running-jobs-in-a-container#example-running-a-job-within-a-container)
  "sourceVolumePath": **Required** The source path to the volume to be mounted into the docker container
  "targetVolumePath": **Required** The target path to the volume to be mounted into the docker container
  "readOnly": false **Required** whether or not the mount should be read only
"mountVolumes": **Required** an array of mounts to mount into the container, same fields as above
  "sourceVolumePath": **Required** The source path to the volume to be mounted into the docker container
  "targetVolumePath": **Required** The target path to the volume to be mounted into the docker container
  "readOnly": false **Required** whether or not the mount should be read only
"registry" **Optional** docker registry credentials to use when using a private container registry
  "username": **Optional** the username
  "password": **Optional** the password
  "serverUrl": **Optional** the registry url
"portMappings": **Optional** an array of source:target ports to map into the container
```
  
</details>

No output is expected

Currently we build all container actions at the start of the job. By doing it during the hook, we move this to just in time building for hooks. We could expose a hook to build/pull a container action, and have those called at the start of a job, but doing so would require hook authors to track the build containers in the state, which could be painful.

### Run Script Step
The `run_script_step` expects you to:
- Invoke the provided script inside the job container and return the exit code
- Stream any step log output to stdout and stderr

<details>
<summary>Example Input</summary>
<br>


```
  "command": "run_script_step",
  "responseFile": null,
  "state":
  {
    "network": "github_network_53269bd575974817b43f4733536b200c",
    "jobContainer" : "82e8219701fe096a35941d869cf8d71af1d943b5d3bdd718850fb87ac3042480",
    "serviceContainers": 
    {
      "redis": "60972d9aa486605e66b0dad4abb638dc3d9116f566579e418166eedb8abb9105"
    }
  }
  "args": 
  {
    "entryPointArgs": ["-e", "/runner/temp/abc123.sh"],
    "entryPoint": "bash",
    "environmentVariables": {
      "NODE_ENV": "development"
    },
    "prependPath": ["/foo/bar", "bar/foo"],
    "workingDirectory": "/__w/thboop-test2/thboop-test2"
  }
```
  
</details>

<details>
<summary>Field Descriptions</summary>
<br>

```
Arg Fields:

  
"entryPointArgs": **Optional** A list containing the entry point args
"entryPoint": **Optional** The container entry point to use if the default image entrypoint should be overwritten
"prependPath": **Optional** an array of additional paths to prepend to the $PATH variable
"workingDirectory": **Required** A string containing the absolute path of the working directory
"environmentVariables": **Optional** A map of key value env's to set
```

</details>

No output is expected


## Limitations
- We will only support linux on launch
- Hooks are set by the runner admin, and thus are only supported on self hosted runners 

## Consequences
- We support non docker scenarios for self hosted runners and allow customers to customize their docker invocations
- We ship/maintain docs on docker hooks and an open source repo with examples
- We support these hooks and add enough telemetry to be able to troubleshoot support issues as they come in.
