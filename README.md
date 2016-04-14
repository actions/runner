# VSTS Cross Platform Agent (CoreCLR)

## Overview

A cross platform build and release agent for Visual Studio Team Services and Team Foundation Server 2015 and beyond.  This will be replacing/combining the existing closed source windows build agent and the existing [xplat agent](https://github.com/Microsoft/vso-agent)

Supported on Windows, OSX and Linux.  Written for the .NET Core CLR as one code base in C#.

Will run all existing tasks (typescript/javascript and powershell) including [our in the box](https://github.com/Microsoft/vso-agent-tasks) and your custom tasks written with our [vsts task SDK](https://github.com/Microsoft/vsts-task-lib). 


## Status

A preview is available for Linux/OSX for VSTS.  A release is coming soon with more capabilities than the deprecated node agent: Auto Update, Cancellation, Run as a svc on OSX and Linux, and Gated support.

What's missing from the preview?  Run as svc on OSX, TfsVC support, RM, test publishing (all being worked on).  On-prem NTLM support is in the works so preview with VSTS.

A preview for windows is coming soon (finishing powershell handlers and tfsvc support)  

|   | Build & Test | Preview | Release |
|---|:-----:|:-----:|:-----:|
|![Linux](docs/linux_med.png) **Ubuntu 14.04**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/2853/badge?branch=master)| [v0.7](https://github.com/Microsoft/vsts-agent/releases) | Soon |
|![Apple](docs/apple_med.png) **OSX 10.11**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3080/badge?branch=master)| [v0.7](https://github.com/Microsoft/vsts-agent/releases) | Soon
|![Win](docs/win_med.png) **Windows 10**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/2850/badge?branch=master)| Soon | |

## Configure Account

VSTS only for now.  On-prem coming with NTLM support in the works.

Create a PAT token.  [Step by Step here](http://roadtoalm.com/2015/07/22/using-personal-access-tokens-to-access-visual-studio-online/)

Add the user you created the PAT token for to *both*:

  1. Agent Pool Administrators (allows to register)
  2. Agent Pool Service Accounts (allows listening to build queue)

![Agent Roles](docs/roles.png "Agent Roles")

>> TIPS:
>> You can add to roles for a specific pool or select "All Pools" on the left and grant for all pools.  This allows the account owner to delegate build administration globally or for specific pools.  [More here](https://msdn.microsoft.com/en-us/Library/vs/alm/Build/agents/admin)
>> The PAT token is only used to listen to the message queue for a build job
>> When a build is run, it will generate an OAuth token for the scoped identity selected on the general tab of the build definition.  That token is short lived and will be used to access resource in VSTS

## Get Agent

![linux](docs/linux_sm.png)  [Get Started Linux](docs/startLinux.md)  

![osx](docs/apple_sm.png)  [Get Started OSX](docs/startOSX.md)  

## Configuration

Other detailed configuration options are [covered here](docs/config.md)

## Contribute (Dev)

### Dev Dependencies

![Win](docs/win_sm.png)![*nix](docs/linux_sm.png) [Install .NET Core Required for our Build](docs/dev/netcore.md)

![Win](docs/win_sm.png) Git for Windows [Install Here](https://git-scm.com/downloads) _(needed for dev sh script)

### Build, Test, Clean, Restore 

From src:

![Win](docs/win_sm.png) `dev {command}`  

![*nix](docs/linux_sm.png) `./dev.sh {command}`
  
**Commands:**  

`restore` (`r`): Run first time and any time you change a project.json  

`build` (`b`):   build everything  

`test` (`t`):    run unit tests
        
  results in: 
  Test/bin/Debug/dnxcore50/{platform}/testResults.xml

`buildtest` (`bt`): build and test

`clean` (`c`):   deletes build output for each projects
 
`layout` (`l`): Creates a full layout in {root}/_layout  
   Does a clean, restore, build, publish and copy
   Default is Debug.  Passing Release as argument is supported (dev l Release)

`update` (`u`) {dirname}: Builds and publishes just one dir.  Patches the layout
   update {dirname}
   Use if you change code in an assembly and don't want to wait for the full layout.

`validate` (`v`): Precheckin validation.  Runs git clean, layout and test.

### Editors

[Using Visual Studio 2015](docs/dev/vs.md)  
[Using Visual Studio Code and Mono Debugger](docs/dev/code.md)  

### Styling

We use the dotnet foundation and CoreCLR style guidelines [located here](
https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md)
