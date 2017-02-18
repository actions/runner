# Pipelines

#### Note to readers: This is still in flight so some concepts appear that are not fully flushed out

## Goals
- **Define constructs which provide a more powerful and flexible execution engine for RM/Build/Deployment**: Allow pipeline execution with minimal intervention points required from consumers
- **Provide a simple yet powerful config as code model**: Easily scale from very simple processes to more complex processes without requiring cumbersome hierarchies and concepts
- **Provide data flow constructs for simple variables and complex resources**: Provide semantic constructs for describing how data flows through the system

## Non-Goals
- **Provide a full replacement for all existing application-level constructs**: This is not meant to encompass all application semantics in the Build and RM systems

## Terms
- **Pipeline**: A construct which defines the inputs and outputs necessary to complete a set of work, including how the data flows through the system and in what order the steps are executed
- **Job**: A container for task execution which supports different execution targets such as server, queue, or deploymentGroup
- **Condition**: An [expression language](conditions.md) supporting rich evaluation of context for conditional execution
- **Task**: A smallest unit of work in the system, allowing consumers to plug custom behaviors into jobs
- **Variable**: A name/value pair, similar to environment variables, for passing simple data values
- **Resource**: An object which defines complex data and semantics for import and export using a pluggable provider model. See [resources](resources.md) for a more in-depth look at the resource extensibility model.

## Semantic concepts for resources
### Import
A keyword which conveys the intent to utilize an external resource in the current job. The resource which is imported will be placed in the job's working directory in a folder of the same name. References to contents within the resource may simply use relative paths  starting with the resource name. For instance, if you import a resource named `vso`, then the file `foo.txt` may be referenced within  the job simply as `vso/foo.txt`.

### Export
A keyword which conveys the intent to publish a resource for potential consumption in a downstream job. The inputs provided to the `export` item are dependent upon the type of resource which is being exported. 

### How it works
Under the covers `import` and `export` are simply semantic mappings to the resource provider tasks. When the system reads an `import` the statement is replaced with the resource-specific import task as specified by the resource provider. Likewise in place of an `export` the system injects the resource-specific export task as specified by the resource provider. While we could simply document and inform consumers to utlize the tasks directly, this provides a more loosely coupled and easy to read mechanism for performing the same purpose. The keywords also allow the system to infer dependencies between jobs in the system automatically, which further reduces the verbosity of the document.

## Simple pipeline
The pipeline process may be defined completely in the repository using YAML as the definition format. A very simple definition may look like the following:
```yaml
resources:
  - name: vso
    type: self

jobs:
  - name: simple build
    target:
      type: queue
      name: default
    steps:
      - import: vso
      - task: msbuild@1.*
        name: Build solution 
        inputs:
          project: vso/src/project.sln
          arguments: /m /v:minimal
      - export: artifact
        name: drop
        inputs:
          include: ['bin/**/*.dll']
          exclude: ['bin/**/*Test*.dll']
```
This defines a pipeline with a single job which acts on the current source repository. Since all file paths are relative to a resource within the working directory, there is a resource defined with the type `self` which indicates the current repository. This allows the pipeline author to alias the current repository like other repositories, and allows separation of process and source if that model is desired as there is no implicit mapping of the current repository. After selecting an available agent from a queue named `default`, the agent runs the msbuild task from the server locked to the latest version within the 1.0 major milestone. Once the project has been built successfully the system will run an automatically injected  task for the `artifact` resource provider to publish the specified data to the server at the name `drop`.

## Resources
While the previous examples only show a single repository resource, it is entirely possible in this model to provide multiple repositories or any number of resources for that matter in a job. For instance, you could have a job that pulls a `TfsGit` repository in addition to a `GitHub` repository or multiple repositories of the same type. For this particular instance the repository which contains the pipeline definition does not contain code itself, and as such there is no self referenced resource defined or needed.
```yaml
resources:
  - name: vsts-agent
    type: git
    endpoint: git-hub-endpoint # TBD on how to reference endpoints from this format
    data:
      url: https://github.com/Microsoft/vsts-agent.git
      ref: master

  - name: vsts-tasks
    type: git
    endpoint: git-hub-endpoint # TBD on how to reference endpoints from this format
    data:
      url: https://github.com/Microsoft/vsts-tasks.git
      ref: master

jobs:
  - name: job1
    target:
      type: queue
      name: default
    steps:
      - import: vsts-agent
      - import: vsts-tasks
      - task: msbuild@1.*
        name: Compile vsts-agent
        inputs:
          project: vsts-agent/src/build.proj
      - task: gulp@0.*
        name: Compile vsts-tasks
        inputs:
          gulpfile: vsts-tasks/src/gulpfile.js
```
## Job dependencies
For a slightly more complex model, here is the definition of two jobs which depend on each other, propagating the outputs of the first job including environment and artifacts into the second job.
```yaml
resources:
  - name: vso
    type: self

jobs:
  - name: job1
    target: 
      type: queue
      name: default
    steps:
      - import: vso
      - task: msbuild@1.*
        name: Build solution 
        inputs:
          project: vso/src/project.sln
          arguments: /m /v:minimal
      - export: artifact 
        name: drop
        inputs:
          include: ['/bin/**/*.dll']
          exclude: ['/bin/**/*Test*.dll']
      - export: environment
        name: outputs
        inputs:
          var1: myvalue1
          var2: myvalue2

  - name: job2
    target: 
      type: queue
      name: default
    steps:
      - import: jobs('job1').exports('drop')
      - import: jobs('job1').exports('outputs')
      - task: powershell@1.*
        name: Run dostuff script
        inputs:
          script: drop/scripts/dostuff.ps1
          arguments: /a:$(job1.var1) $(job1.var2)
```
This is significant in a few of ways. First, we have defined an implicit ordering dependency between the first and second job which informs the system of execution order without explicit definition. Second, we have declared a flow of data through our system using the `export` and `import` verbs to constitute state within the actively running job. In addition we have illustrated that the behavior for the propagation of outputs across jobs which will be well-understood by the system; the importing of an external environment will automatically create a namespace for the variable names based on the source which generated them. In this example, the source of the environment was named `job1` so the variables are prefixed accordingly as `job1.var1` and `job1.var2`.

## Conditional job execution
By default a job dependency requires successful execution of all previous dependent jobs. Job dependencies are discovered by looking at the `condition` and `import` statements for a job to determine usages of the `jobs(<job name>)` function. All referenced jobs from these statements are considered dependencies and if no custom condition is present a default expression is provided by the system requiring successful execution of all dependencies. This default behavior may be modified by specifying a custom job execution [condition](conditions.md). For instance, we can modify the second job from above as follows to provide different execution behaviors:

### Always run
```yaml
- name: job2
  target: 
    type: queue
    name: default
  condition: "in(jobs('job1').result, 'succeeded', 'failed', 'canceled', 'skipped')"
  ....
```
The condition above places an implicit ordering dependency on the completion of `job1`. Since all result conditions are mentioned `job2` will always run after the completion of `job1`. The presence of the custom condition completely overrides the default behavior of success, configuring this job to run for any result.

### Run based on outputs
```yaml
- name: job2
  target: 
    type: queue
    name: default
  condition: "and(eq(jobs('job1').result, 'succeeded'), eq(jobs('job1').exports.outputs.var1, 'myvalue'))"
  ....
```
The condition above places both a success requirement and the comparison of an output from `job1` which may be dynamically determined during execution. The ability to include output variables from a previous job execution to provide control flow decisions later opens up all sorts of conditional execution policies not available in the current system. Again, as in the previous example, the presence of a custom condition overrides the default behavior.

### Run if a previous job failed
```yaml
jobs:
  - name: job1
    target: 
      type: queue
      name: default
    steps:
      .....
    
  - name: job1-error
    target: 
      type: server
    condition: "eq(jobs('job1').result, 'failed')"
    steps:
      .....
```
In the above example the expression depends on an output of the `job1`. This will place an implicit execution dependency on the completion of `job1` in order to evaluate the execution condition of `job1-error`. Since we only execute this job on failure of a previous job, under normal circumstances it will be skipped. This is useful for performing cleanup or notification handling when a critical step in the pipeline fails.

## Job Toolset Plugins
The default language for a job will be the presented thus far which, while powerful and quite simple, still requires rigid knowledge of the available tasks and system to accomplish even the simplest of tasks. Individual project types, like those which build and test node projects, may find the learning curve for getting started higher than it needs to be. One important tenet of our system is that it is not only powerful but also approachable for newcomers alike. In order to satisfy the on-boarding of more simple projects, we will allow for the job definition language to be extended via `toolset` plug-ins. The general idea behind toolsets would be that for certain tools, such as node, there are common actions which need to occur in most, if not all, jobs which build/test using that specific tool. The plug-in would simply authoring of the job contents by providing custom pluggable points that make sense for that particular job type. Additionally certain things would *just happen*, such as installing the toolset and placing it in the path automatically.
           
For an example of how the internals of a custom language may look, see the [following document](https://github.com/Microsoft/vsts-tasks/blob/master/docs/yaml.md).

## Task Templates
Tasks are another construct which may be templated. On the server these are known as `TaskGroups`, and this provides a mechanism for performing the same style of reuse without requiring interaction with the server model. 
```yaml
inputs:
  - name: project
    type: string
  - name: platform
    type: string
    defaultValue: AnyCPU
  - name: configuration
    type: string
    defaultValue: Debug
  - name: testAssemblies
    type: string

- task: msbuild@1.*
  name: "Build {{ inputs('project') }}"
  inputs:
    project: "{{ inputs('project') }}"
    arguments: "/p:Platform={{ inputs('platform') }} /p:Configuration={{ inputs('configuration') }}"
- task: vstest@1.*
  name: "Test {{ inputs('testAssemblies') }}"
  inputs: 
    assemblies: "{{ inputs('testAssemblies') }}"
```
If the above file were located in a folder `src/tasks/buildandtest.yml`, a job may include this group with the following syntax:
```yaml
jobs:
  - name: build
    target:
      type: queue
      name: default
    steps:
      - import: code
      - include: code/src/tasks/buildandtest.yml
        inputs:
          project: code/src/dirs.proj
          testAssemblies: code/bin/**/*Test*.dll
```
This provides the ability to build up libararies of useful functionality by aggregating individual tasks into larger pieces of logic. 
## Looping
Often it is desirable to run a job across different environments, toolsets, or inputs. In examples we have analyzed thus far the user has the requirement of being very explicit about all combinations of inputs which may become daunting when the list grows beyond 2 or 3. The solution to this growth problem is the introduction of a looping construct, which allows the author to define a list of items to be used as items to apply to the template. 

In order to illustrate the scenario, consider the task template from the previous section. We would now like to run the same set of steps in different jobs for a set of inputs. With the constructs we have defined thus far, we would be required to list each job explicitly for the different input sets desired.
```yaml
resources:
  - name: code
    type: git
    data:
      url: https://github.com/Microsoft/vsts-agent.git
      ref: master

jobs:
  - name: x86-release
    target:
      type: queue
      name: default
    steps:
      - import: code
      - task: code/src/tasks/buildandtest.yml
        inputs:
          project: code/src/dirs.proj
          platform: x86
          configuration: release
          testAssemblies: code/bin/x86/**Test*.dll

  - name: x64-release
    target:
      type: queue
      name: default
    steps:
      - import: code
      - task: code/src/tasks/buildandtest.yml
        inputs:
          project: code/src/dirs.proj
          platform: x64
          configuration: release
          testAssemblies: code/bin/x64/**Test*.dll
          
  - name: finalize
    target: server
    condition: and(succeeded('x86-release'), succeeded('x64-release'))
    steps:
      ....
```
Using looping constructs, we can reduce duplication and simplify our process considerably. Taking a look at the previous example, we are effectively performing the same work twice with two different values for the `release` input to our task. Instead of listing this twice, we could simply apply a list of items and allow the system to expand this for us.
```yaml
resources:
  - name: code
    type: git
    data:
      url: https://github.com/Microsoft/vsts-agent.git
      ref: master

jobs:
  - name: "build-{{item}}-release"
    target:
      type: queue
      name: default
    steps:
      - import: code
      - task: code/src/tasks/buildandtest.yml
        inputs:
          project: code/src/dirs.proj
          platform: "{{item}}"
          configuration: release
          testAssemblies: "code/bin/{{item}}/**Test*.dll"
    with_items:
      - x86
      - x64

- name: finalize
    target: server
    condition: and(succeeded('x86-release'), succeeded('x64-release'))
    steps:
      ....
```
As you can see in our example above, the looping construct removed our duplicated job logic and allowed us to more concisely define the desired logic and input sets. If more than a single value should be considered for each iteration, the system will also allow for an array of dictionaries as the input source. This allows for more complex and powerful iterators where there is more than a single dimension:
```yaml
resources:
  - name: code
    type: git
    data:
      url: https://github.com/Microsoft/vsts-agent.git
      ref: master

jobs:
  - name: "build-{{item.platform}}-{{item.configuration}}"
    target:
      type: queue
      name: default
    variables:
      "{{item}}"
    steps:
      - import: code
        clean: false
      - task: code/src/tasks/buildandtest.yml
        inputs:
          project: code/src/dirs.proj
          platform: $(platform)
          configuration: $(configuration)
          testAssemblies: code/bin/$(platform)/**Test*.dll
    with_items: 
      - platform: x86
        configuration: release 
      - platform: x86
        configuration: debug
      - platform: x64
        configuration: release
      - platform: x64
        configuration: debug
        
- name: finalize
    target: server
    condition: and(succeeded('x86-release'), succeeded('x64-release'), succeeded('x86-debug'), succeeded('x64-debug'))
    steps:
      ....
```
Other looping constructs may be introduced in the future, such as the concept of a cross product is computed from multiple lists in order to build matrix. At this time, however, the explicit looping construct should be sufficent for most scenarios and provides for a cleaner description language.

## Pipeline Templates
Pipelines may be authored as stand-alone definitions or as templates to be inherited. The advantage of providing a model for process inheritance is it provides the ability to enforce policy on a set of pipeline definitions by providing a master process with configurable overrides. 

### Defining a Template
The definition for a template from which other pipelines inherit, in the most simple case, looks similar to the following pipeline. This particular file would be dropped in `src/toolsets/dotnet/pipeline.yml` and is modeled after the existing ASP.NET Core template found on the service.
```yaml
# All values which appear in the inputs section are overridable by a definition
# which extends the template.
parameters:

  # Controls the name of the queue which jobs should use
  queueName: default

  # Controls the pattern for build project discovery
  projects: **/project.json

  # Controls the input pattern for test project discovery
  testProjects: **/*Tests/project.json

  # Controls whether or not web projects should be published
  publishWebProjects: true

  # Controls whether or not the published projects should be zipped
  zipPublishedProjects: true

  # Defines the input matrix for driving job generation from a template
  matrix:
    - buildConfiguration: release
      dotnet: 1.1

# Defines the customizable stages that may be overridden. Each group
# is expected to contain 0 or more task directives, which will be injected
# at specific points in the template output.
groups:
    before_install:
    before_restore:
    before_build:
    before_test:
    before_publish:
    after_publish:
      
# In our resource list a self reference type is inferred by the system. The name 's' has been chosen in this
# case for backward compatibility with the location of $(build.sourcesdirectory).
resources:
  - name: s
    type: self
    
jobs:
  - with_items: 
      "{{matrix}}"
    name: "build-{{item.buildConfiguration}}"
    target: 
      type: queue
      name: "{{queueName}}"
    variables:
      "{{item}}"
    steps:
      - import: s
      - group: before_install
      - task: dotnetcore@0.*
        name: install
        inputs:
          command: install
          arguments: "--version {{item.dotnet}}"
      - group: before_restore
      - task: dotnetcore@0.*
        name: restore
        inputs:
          command: restore
          projects: "{{projects}}"
      - group: before_build
      - task: dotnetcore@0.*
        name: build
        inputs:
          command: build
          arguments: --configuration $(buildConfiguration)
      - group: before_test
      - task: dotnetcore@0.*
        name: test
        inputs:
          command: test
          projects: {{testProjects}}
          arguments: --configuration $(buildConfiguration)
      - group: before_publish
      - task: dotnetcore@0.*
        name: publish
        inputs:
          command: publish
          arguments: --configuration $(buildConfiguration) --output $(build.artifactstagingdirectory)
          publishWebProjects: {{publishWebProjects}}
          zipPublishedProject: {{zipPublishedProjects}}
      - export: artifact
        name: drop
        condition: always()
        inputs:
          pathToPublish: $(build.artifactstagingdirectory)
      - group: after_publish
```
There are a couple of points which should be made clear before we move on. First, the context within a template is implicitly set to the `inputs` object to avoid the need to reference it explicitly. Second, we have a couple of examples where we are using an object expansion to inject an array variable as the array of another property. For instance, the `group` tag is just a place-holder for a task group, which is itself just an object which contains a list of tasks. The `group` tag is special in that the template author is allowing the derived definition to replace or inject behavior at particular points of the process. 

We also see this when providing all of the values from the matrix item as variables which will then be accessible as environment variables within the job downstream. Since the item being iterated is an array of dictionaries, and the `variables` property is expected itself to be a dictionary, we are able to safely perform this replacement using templating syntax.
```yaml
- variables:
    "{{item}}"
```
### Using a Template
A usage of this template is shown below. Assuming the code being built lives in the same repository as this file and the defaults provided are sufficient (e.g. using project.json, you want zip and publish your web application, and you only want to build, test, and package a release build verified against the latest dotnet framework) then your file may be as simple as what you see below.
```yaml
# Since this file does not have a location qualifier and the toolset does not have required inputs, this is
# all that is required for the most simple of definitions that fit our pre-defined model.
uses: dotnet
```
If the code author desires to build and test their code on multiple dotnet versions or multiple build configurations, there is a top-level `matrix` property which may be overridden to specify specific configurations and versions. The defaults provided by the template above are `buildConfiguration: release, dotnet: 1.1`. In our example below, we want to build and verify our application against both `dotnet: 1.0` and `dotnet: 1.1`, so we override the matrix with the necessary values. 
```yaml
# Since this file does not have a location qualifier and the toolset does not have required inputs, this is
# all that is required for the most simple of definitions that fit our pre-defined model.
uses: dotnet

# Specify the matrix input by defining it inline here. In this example we will run the default project, test, 
# publish step for the release configuration and dotnet versions 1.0 and 1.1.
matrix:
  - buildConfiguration: release
    dotnet: 1.0
  - buildConfiguration: release
    dotnet: 1.1
```
Assuming more control is needed, such as the injection of custom steps into the pre-defined lifecycle, there are a few override points defined in the initial template as the empty `group` elements with the job steps. These may be specified in the top-level file and will be overlayed on top of the base template execution time as appropriate.
```yaml
# Since this file does not have a location qualifier and the toolset does not have required inputs, this is
# all that is required for the most simple of definitions that fit our pre-defined model.
uses: dotnet

# Individual steps within the toolset lifecycle may be overridden here. In this case the following injection
# points are allowed. Each overridable section is denoted in the template by the 'group' step type, which serves
# as a named placeholder for implementations to inject custom logic and well-understood points without 
# understanding the entire job execution.
groups:
  before_install:
    - task: powershell@1.*
      name: My custom powershell install step
      inputs:
        script: src/scripts/preinstall.ps1
        
#  before_restore:
#  before_build:
#  before_test:
#  before_publish:
#  after_publish:

# Specify the matrix input by defining it inline here. In this example we will run the default project, test, 
# publish step for the release configuration and dotnet versions 1.0 and 1.1.
matrix:
  - buildConfiguration: release
    dotnet: 1.0
  - buildConfiguration: release
    dotnet: 1.1
```
## Containers
Containers can provide for much more flexible pipelines by enabling individual jobs to define their execution environemtn without requiring toolsets and dependencies to be installed on the agent machines. Each job can sepcify one or more container images to be used to execute tasks along with additional container images to be started and linked to the job execution container. Container image operating systems must match the host operating system the agent is running on.

Prior to running tasks the agent will start a container based on the image specified, map in the resources as volumes, start and link any additional services and setup environment variables.  If you want to build containers as part of your job you will need to specify that the docker daemon should be made available to your job by setting maphost property to true

```yaml
# define a container image resource.  
resources:
  - name: job1image
    type: docker-image
    endpoint: 
      id: msazure-docker-endpoint-id
    data:
      image: msazure/nodestandard
      tag: 2017-1
  - name: redis-services
    type: docker-image
    data:
      image: redis
      tag: 3.0.7

jobs:
  - name: job1
# define the container for the job along with any services that should be linked to the container
    container:
      image: job1image
      maphost: true
      services:
        - name: redis
          image: redis-service
    steps:
      - task: bash@1.x 
        name: Run build script
        inputs:
          script: build.sh
      - task: bash@1.x 
        name: Test app
        inputs:
          script: test.sh

```
Having the agent start the container on the host prior to running tasks potentially enables some other interesting capabilities like controlling access to certian internet resources.  For example if you have a policy in your organziation that you should not pull packages from nuget.org the container networking could be configured wiht a proxy that prevents access.
