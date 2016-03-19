# VSTS Cross Platform Agent (CoreCLR)

## Overview

A cross platform build and release agent for Visual Studio Team Services and Team Foundation Server 2015 and beyond.  This will be replacing/combining the existing closed source windows build agent and the existing [xplat agent](https://github.com/Microsoft/vso-agent)

Supported on Windows, OSX and Linux.  Written for the .NET Core CLR as one code base in C#.

Will run all existing tasks (typescript/javascript and powershell) including [our in the box](https://github.com/Microsoft/vso-agent-tasks) and your custom tasks written with our [vsts task SDK](https://github.com/Microsoft/vsts-task-lib). 

> Status:  Work is just beginning on this project.  Please do not log issues yet.  You are welcome to inspect and try it out as we build it.  We are targetting a preview early in April 2016, available with VSTS soon after that and is targetting the next TFS release for on-premises.

## Build Status
|   | Debug |
|---|:-----:|
|**Ubuntu 15.10**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/2853/badge?branch=master)|
|**OSX 10.11**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3080/badge?branch=master)|
|**Windows Server 2012R2**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/2850/badge?branch=master)|


## Install

Not available yet.  Need to build from source.  See Contribute below.

## Contribute (Dev)

### Dev Dependencies

![Win](docs/win_sm.png)![*nix](docs/linux_sm.png) [Install .NET Core Required for our Build](docs/dev/netcore.md)

![Win](docs/win_sm.png) Git for Windows [Install Here](https://git-scm.com/downloads) _(needed for dev sh script)

### Build, Test, Clean, Restore 

From src:

![Win](docs/win_sm.png) `dev {command}`  

![*nix](docs/linux_sm.png) `./dev.sh {command}`
  
** Commands: **

`restore` (`r`): Run first time and any time you change a project.json  

`build` (`b`):   build everything  

`test` (`t`):    run unit tests
        
  results in: 
  Test/bin/Debug/dnxcore50/{platform}/testResults.xml

`buildtest` (`bt`): build and test

`clean` (`c`):   deletes build output for each projects
 
`layout` (`l`): Creates a full layout in {root}/_layout  
   Does a clean, restore, build, publish and copy

`update` (`u`) {dirname}: Builds and publishes just one dir.  Patches the layout
   update {dirname}
   Use if you change code in an assembly and don't want to wait for the full layout.

`validate` (`v`): Precheckin validation.  Runs git clean, layout and test.

### Editors

[Using Visual Studio 2015](docs/dev/vs.md)  
[Using Visual Studio Code and Mono Debugger](docs/dev/code.md)  

# Styling convention

We use the dotnet foundation and CoreCLR style guidelines [located here](
https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md)
