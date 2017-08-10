# Resources
## Goals
- **Provide generic construct for data flow through a pipeline**: Consume and publish resources which may then be consumed downstream
- **Provide extensible mechanism for consuming any resource type**: Pluggable resource providers allow for future extensibility with minimal changes
- **Provide smarter routing for agent pools**: Provide smarter agent allocation based on required resources matched against what already exists on the agent
- **Provide disk space insights and administration from the server**: Having a concept of resources provides better insight into what is taking space on your agents
- **Decouple the agent from resource acquisition to reduce required agent updates**: Providing an extensible and clean surface area reduces the coupling between the agent and server for fewer forced updates

## Resource Contract
Broken down into the most simple concepts possible, the purpose of the execution engine is to flow variables and data from job to job and machine to machine. Most importantly, a key insight is that the execution engine doesn't need to understand the internals of the data propagating through different stages within a pipeline, but only how to identify, retrieve, and update the different types of data. For this reason, the proposal outlined here is to formalize the concept of an extensible type known simply as `Resource` with the following properties:
```yaml
resource:
	name: string # A local name by which this resource is referenced
	type: string # A type of resource for provider selection
	endpoint: ordinal name to an endpoint # An optional reference to a managed endpoint
	id: string # A provider-specific value for resource identification
	data: object # An opaque structure provided for/by the provider
```
## Resource Identification
 Resources downloaded on the agent, including repositories, build artifacts, nuget packages, etc., should be registered with and tracked by the server to provide better agent routing and visibility for pool administrators to determine which definitions and resources consume the most space. In order to provide this selection, the messages delivered to the agent for running jobs will need to be altered to include a list of the resources which are required for the job. For instance, a pipeline which consumes a build drop artifact, a git repository, and a nuget package may look something like the following:

*Note: The instance identifier is listed as a format string for illustrative purposes and would be computed prior to agent selection or delivery to the agent for matching agent resources to resource requirements. The resource provider, determined by the resource type, is responsible for specifying the set of properties used for identifying a specific version of the corresponding resource type.*
```yaml
job:
  resources:
    - name: build
      type: vsts.build
      id: "{{data.collectionId}}.{{data.buildId}}.{{data.artifactName}}"
      endpoint: system-endpoint-id
      data:
        collectionId: "45a325da-9ad3-4e34-a044-5a6765528113"
        projectId: "ac963673-c64a-48d4-b7f5-28e44a9db45c"
        definitionId: 4
        buildId: 27
        artifactName: drop
			  
    - name: vso
      type: git
      id: "{{data.url}}"
      endpoint: github-endpoint-id
      data:
        url: "https://github.com/Microsoft/vsts-tasks.git"
        ref: master
	      
    - name: nuget_refs
      type: nuget
      id: "{{endpoint.id}}.{{data.feed}}.{{data.package}}.{{data.version}}"
      endpoint: nuget-endpoint-id
      data:
        feed: my feed
        package: my package
        version: "2.1.0.0"
```
When requesting an agent, the system will attach all resources needed for the job to the agent requirements. The pool orchestrator will then take the requested resources into consideration while selecting an agent to attempt to reduce resource downloads to a minimum. Resources will be identified and matched by the tuple (resource.type, resource.id), so a given resource type is **REQUIRED** to specify an ID formatting specification which is unique to a specific resource for that type only. 
## Resource Caching
When the agent receives a job message from the server that includes a resource list, it will determine up-front where the resource should be located based on a combination of the target definition and the resource identifiers provided by the server. By default, the local folder for resources will be contained within the working folder for a build definition (for instance, `$(Agent.InstallDir)\_work\1\{resource.name}`).

Prior to execution of a job, the agent will compute and either reference existing folders or create new empty folders for all resources included in the job. The local folder on disk is always based on the name of the resource which allows for easy discovery of files within a resource. For instance, given the resources in the job above the agent might generate the following stucture on disk:
```
$(Agent.InstallDir)
  _work
    1
      build # locally generated folder for resource 'build'
      vso # locally generated folder for resource 'vso'
      nuget_refs # locally generated folder for resource 'nuget_refs'
```            
The agent will then populate the environment with mappings which translate a resource name to the location on disk allocated for the resource. It is important to note that since the agent itself has no concept of the internals of a resource, it is up to the resource download task to determine what the appropriate behavior is if the current resource folder is detected to be dirty (e.g. re-download, incremental update, etc). 

In order to retain the separation of concerns between the directory manager of the agent and the actual downloading of resources to disk, the task library will be updated to provide the ability to retrieve resources by name. The task library will be updated to provide functions to retrieve a resource by name, similar to the way a task can retrieve a service endpoint by ID:
```
// returns the full resource from the job environment
getResource(name: string): Resource
```
Once a resource has been successfully placed on disk (e.g. the resource download task completes successfully) the agent will register the resource with the list of cached items on the server in addition to tracking the items locally on the agent itself. The server cache list will consist of the following contract for reporting contents:
```yaml
cache:
  size: long # aggregate size of all items in the cache
  items:
    - resource:
        id: string # the computed id for identifying this resource instance/version
        type: string # the resource type or provider name
        name: string # This may not make sense when dealing with shared resources as the name can differ across definitions
      size: long # size of the resource in bytes
      location: string # location of the resource on disk
      createdOn: datetime # date and time of download
      lastAccessedOn: datetime # date and time of last hit from a job
```
As the cache is populated the agent selection algorithm can adjust to prefer agents that have the fewest unavailable resources for running a given job. This should dramatically speed up re-runs of jobs in addition to running a triggered job with previously downloaded resources.

A key point to reiterate in this section is the responsibility of the agent is to determine the local folder in which resource should be placed, not to actually manage or place them. This decouples the responsibility of local disk management and resource download, ensuring we have a clean contract between the agent and the tasks which it runs.
## Resource Download and Upload
Since the core pipeline engine does not understand how to actually acquire or update specific resources, we will need to provide a pluggable mechanism by which resource providers may inject logic to perform this actions on behalf of the system. The current mechanism for plugging into the agent is tasks and that is the proposed mechanism for extended the agent for resource consumption and production. Similar to the release management artifact extensibility model, resource types will register tasks for both the download and upload actions on a particular resource. The main difference between the release management model and the proposed model is a resource provider **MUST** implement a download task as well as an upload task, as there will be no known types to the system.  

By analyzing the resources needed in a job, the server will automatically inject the necessary tasks implementations as specified by the provider. The key to representing these as tasks rather than implicit plugins on agent is it allows consumers to rearrange the acquisition of resources with respect to their own custom tasks, where today the artifact and repository acquisitions **MUST** be the first step in the job. A secondary advantage to driving all resource acquisition with tasks is it allows us to decouple the agent from any specific artifact implementation and reduce our schedule of required agent updates short of a security issue, core agent logic bug, or breaking change in the contract between the agent and server.
## Resource Sharing
While out of scope for the current work, it is useful to describe how we might begin to share resources across definitions to get further improvements and a fairly large reduction in disk space usage on the agent. With a few small changes to the layout structure on disk, in addition to the introduction of containers, we may be able to further improve resuse of resources on an agent.

Instead of downloading resources to per-definition folders, we would instead download them to a shared cache folder which sits side by side with the per-definition working folder. Prior to handing over to the resource download tasks, the agent would setup junction points (shown below as rN folders) for definition-specific mappings into the shared resource folders.
```
$(Agent.InstallDir)
  _work
    1
      build => r1
      vso => r2
      nuget_refs => r3
    r1 # locally generated folder for resource 'build' based on the id
    r2 # locally generated folder for resource 'vso' based on the id
    r2 # locally generated folder for resource 'nuget_refs' based on the id
```
While this would dramatically improve sharing of resources and reduce disk space, it does require knowledge of sharing on the part of the definition author and may pose more challenges than it is worth. Ideally we would mount the shared directories into the definition working space in a copy-on-write mode, where the source folder is read only and changes are applied on top the source volume per-definition. Further investigation will need to be done in order to determine if containers help us out in this area.
## History (how did we get here)
Team build provides the ability to explicitly select a single repository for automatic download to the agent and for the purposes of triggering. While this works for the simple case of a product with source contained to a single repository, this does not work for larger projects which may have source aggregated across multiple repositories and even repository providers. There is currently an abstraction for repository in build which factors out the common properties as first-class while leaving the provider-specific properties as an opaque data dictionary with the following contract:
```yaml
repository:
	id: string
	type: string
	name: string	
	defaultBranch: string
	rootFolder: string
	clean: string
	checkoutSubmodules: boolean
	properties: (string, string)
```
A few of non-first class properties leaked into the core object contract, such as 'clean', 'checkoutSubmodules', and 'rootFolder'. In addition to non-shared properties being driven into the core contract, we also never formally introduced the concept of a repository to core the core execution engine. Due to the lack of a formal concept in distributed task the repository is first converted to a `ServiceEndpoint` before being sent to the agent as the core execution system does not have a concept of a `Repository`. While this works in many cases it is not without problems as well.

 - It overloads the meaning of a `ServiceEndpoint` to not only convey shared credentials to a remote endpoint but also represent configuration options specific to the instance
 - It confuses the agent when running the build plug-in since we have no way of knowing if the service endpoint was injected by the build system from a repository object or if the user simply defined a custom endpoint that points to `GitHub` for the purposes of working with data on that service
 - The concept of a repository and how it is downloaded is currently tightly coupled with the agent binary, which requires an agent update any time new functionality is to be delivered or bugs are fixed

While it may make sense to introduce a first-class concept of repository into the core execution engine, it's not clear there is a necessity to do so. Taking a step back at the existing concepts and semantics we support in our application layers today:

- Build
	- Supports a repository with rich triggering semantics and the ability to produce and associate artifacts with a build, such as build drop artifacts or azure packages to be consumed by a downstream release
	- Does **NOT** provide a mechanism for consuming the outputs of a previous build as the input to another build

- Release Management
	- Supports a generic concept of `Artifact` which can represent a build output artifact, a repository, a nuget package, or any other type of resource which is extensible
	- Does **NOT** support triggering based on source changes since the application layer knows only of generic artifacts and doesn't understand the semantics of a repository or any other artifact other than a build. 
	- Does **NOT** not support publishing capabilities, only consumption, so you cannot produce a zip from a release and attach it as an output for triggering another release

Both application layers have their strengths and weaknesses when it comes to artifact management, and the goal of this design is to take both application layer concepts and expose them in the execution engine so as to supersede and represent both concepts equally for a more powerful runtime. Another key prinicple to keep in mind is there needs to be a clear separation between what we consider the application layer, which deals in semantics (repositories are an example of a resource with strong semantics), and the execution engine, which deals with generic constructs and the flow of data (repositories are modeled as a generic resource, seen like any other opaque source of data).
