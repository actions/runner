
# Agent Platform Plans

## Goals

  - Expand our Linux support: Right now we only support RedHat 7.2 and Ubuntu 14.04/16.04.  We routinely get requests versions like RedHat 6 and new distros like SUSE.  
  - Produce a minimal number of agent packages: Right now we drop [5 agents for every build](https://github.com/Microsoft/vsts-agent/releases).  This won't scale.  Ideally, only Windows, Mac, and Linux
  - Allow task authors to create core-CLR tasks and package core clr assets: Right now we only [support typescript (via node) and powershell](https://github.com/Microsoft/vsts-task-lib/blob/master/README.md). 
  - Rationalize all of this with emerging [Docker initiative](https://youtu.be/OOV5bXcJHpc)

## Phases

We must first move the agent to 2.0 core CLR and figure out the linux-x64 single target before we can implement core CLR tasks which can run in a container of any other linux distro and version.

For that reason, we have separated the work into two clear phases.  Move the agent to core CLR with one linux-x64 target and then support writing tasks in core CLR.

## Phase One: Core CLR 2.0 Agent

### Officially Supported and Tested

**Windows**: Windows 7 SP1+, Server 2008 R2 SP1+  
**Mac OS**: 10.12 (Sierra)+ (reduction)  
**RedHat Linux**: 7.3+ minimum.  6+ stretch goal as part of this work (mitigated with containers)  
**Ubuntu Linux**: 14.04+ (LTS versions)
**openSUSE Linux**: 42.2+

### Expanding Linux

We are currently building an agent per Linux distro and version that we support: RedHat 7.2, Ubuntu 14.04, Ubuntu 16.04.   We need to expand to officially supported SUSE and other enterprise distros like Oracle.

.Net Core 2.0 will allow us to only build a **portable linux x64** package which will work across [these supported OS distros and versions](https://github.com/dotnet/core/blob/master/roadmap.md#supported-os-versions). 

### Expanding Linux: Unsupported CoreCLR 2.0 Versions

We need to expand the versions (RedHat 6) and expand the distros (SUSE).  Some of the supported version restrictions are soft limits (official support, encouraging moving forward, RedHat) and others are hard limits (won't technically work, Mac OS and openssl).  

We will attempt to make versions like RedHat 6 work, but the ultimate Linux solution is [our container story](https://youtu.be/OOV5bXcJHpc).  With our container story, the agent runs in the host and it's jobs and tasks in any linux image you select.  The limits will only come from our task story.  See Phase 2 below.

### OS Dependencies

Customers need to install OS dependencies.  Getting the OS dependencies installed [has been a pain point for customers](https://github.com/Microsoft/vsts-agent/issues/232).  For tasks and docker container support, it's critical (covered in phase 2 below). 

Here is a [list of the OS dependencies](https://github.com/dotnet/core/blob/master/Documentation/prereqs.md).

For OSX, openssl via homebrew will no longer be required in core clr 2.0.  For Linux, core CLR 2.0 has a new feature to allow loading OS dependencies from a folder for [self contained linux apps](https://github.com/dotnet/core/blob/master/Documentation/self-contained-linux-apps.md).

### Reducing Supported Versions Implications

Core CLR 2.0, while expanding distros, is contracting supported versions.  Most impactful to us is RedHat 7.2 and OSX 10.10 (Yosemite) and 10.11 (El Capitan) which we currently support today with our agent.  It only supports the recently released 10.12 (Sierra).

RedHat is a soft limit so we will attempt to work back to RH6.  OSX is a hard technical limit (openssl) so we will only support 10.12+

### Agent Builds and Updates

Customers can update their agents from our web UI.  New tasks and new features can demand a new agent version.  *Customers will find themselves stuck as they are potentially surprised they need to update yet updates will not work until they upgrade their OS*.  This is a mac OS and Redhat 7.2 issue.

Agents query the server for advertised versions by platform by querying https://{account}.visualstudio.com/_apis/distributedtask/packages/agent.  The backend holds a registry of agents by platform and version.  It will currently download from github releases by version and platform.  For example: https://github.com/Microsoft/vsts-agent/releases/download/v2.114.0/vsts-agent-win7-x64-2.114.0.zip

Currently, we advertise these platforms in the UI and APIs.

  - win7-x64
  - osx.10.11-x64
  - rhel.7.2-x64
  - ubuntu.14.04-x64
  - ubuntu.16.04-x64

We will change the build to only produce.

  - win7-x64
  - osx.10.12-x64 (ouch)
  - linux-x64

We will change download urls to an azure blob url (firewall considerations) but we will continue to offer [release metadata](https://github.com/Microsoft/vsts-agent/releases) along with the source.  

The agent major version will remain 2.x.  Agents will still update along major version lines if we choose to register the appropriate paths.

The UI will only show **Windows, Mac OS and Linux** tabs (drop distro specific tabs).

The REST APIs (what drives updates) and the backend will continue to point our old platform names to the new drop names so it just works for them.

There will be a sprint cutoff where (1) stop incrementing a osx 10.11 drop and (2) start redirecting old platform names to linux-x86.

So, if sprint # is {SSS}, then at that sprint cutoff:

**Platform --> Drop**    
win7-x64  --> win7-x64-{SSS}.zip  
osx.10.11-x64 --> osx.10.11-x64-{SSS-1}.zip  Deadend.  
osx.10.12-x64 --> osx.10.12-x64-{SSS}.zip.  New Installs and forward  
linux-x64 --> linux-x64-{SSS}.zip.  New Installs and forward  
rhel.7.2-x64 --> linux-x64-{SSS}.zip.  Redirection for old agents  
ubuntu.14.04-x64 --> linux-x64-{SSS}.zip.  Redirection for old agents  
ubuntu.16.04-x64 --> linux-x64-{SSS}.zip.  Redirection for old agents  

Unfortunately every release will still have to write the redirection rows for our old dist story but that can easily be automated.


**Alternatives**  

We considered moving the version to 3.0 which requires an explicit 'migration' from customers due to the OS constraints but that will cause too much friction as new tasks and features demand 3.0.  On premise upgrades will upgrade only to find their builds are failing.  This can still happen for OSX 10.10 and 10.11 but we shouldn't push the pain to all our platforms (especially windows) due to this.

This will also require mac OS users to manually migrate after that sprint which is better than requiring all platforms (windows being the majority) to migrate manually.  We have also discussed whether there's a way to detect server side if OS version is 10.12 and redirect them to osx.10.12-x64 agent download and platform.

### Timeline

Core CLR 2.0 releases Q3 2017 (soon).  We have a [branch ready to go](https://github.com/Microsoft/vsts-agent/tree/users/tihuang/netcore20).  We will target the 2.01 core CLR release which contains critical fixes.

## Phase Two: Core CLR Tasks

A guiding principle from the inception of the build.vNext is that the agent needs to carry everything it needs to be able to execute any task from the market place.

It is important to separate

For Linux, that's node which [goes back to RH5](https://nodesource.com/blog/node-binaries-for-enterprise-linux/), although RedHat 6 is the important one that we get repeated requests for. 

### Tool Runner Task

For a typical tool runner task (msbuild, gradle) the user is bringing the environment in the form of a machine, VM or docker image.  The agent carries the task script engine (node)
```bash
         AGENT             +    User 
                           |
+---------+   +--------+   |
| Handler|----->Node   |   |
+-------+-+   +--------+   |
        |                  |
       +v-------+    which |  +---------+
       | Script +-------------> Gradle  |
       +--------+          |  +---------+
         +---------+       |   +------+
         | Modules |       |   | Java |
         +---------+       |   +------+
                           |
                           +

```

### Utility Task

```bash
                           +
                           |
+---------+   +--------+   | run
| Handler|----->Node   +------->
+-------+-+   +--------+   |
        |                  |
       +v-------+          |
       | Script |          |
       +--------+          |
         +---------+       |
         | Modules |       |
         +---------+       |
                           |
         +---------+       |  run
         |  deps   +-------+---->
         +---------+

```



