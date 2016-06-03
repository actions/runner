# Visual Studio Team Services Build and Release Agent

## Overview

The cross platform build and release agent for Team Services and Team Foundation Server 2015 and beyond.  This will be replacing/combining the existing closed source windows build agent and the existing [xplat agent](https://github.com/Microsoft/vso-agent)

Supported on Windows, OSX, Ubuntu and Red Hat.  Written for the .NET Core CLR as one code base in C#.


## Status

A preview is available for Ubuntu, RedHat and OSX for VSTS.  The current preview is more feature complete than the node agent bringing Auto Update, Cancellation, Run as a svc on OSX and Linux, and Gated support.

What's missing from the preview? Windows tf.exe support is finishing up. 

|   | Build & Test | Preview | Release |
|---|:-----:|:-----:|:-----:|
|![Apple](docs/res/apple_med.png) **OSX**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3080/badge?branch=master)| [Preview 6 v2.101.1](https://github.com/Microsoft/vsts-agent/releases/tag/v2.101.1) | June |
|![Ubuntu](docs/res/ubuntu_med.png) **Ubuntu**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/2853/badge?branch=master)| [Preview 6 v2.101.1](https://github.com/Microsoft/vsts-agent/releases/tag/v2.101.1) | June |
|![RedHat](docs/res/redhat_med.png) **RedHat**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3418/badge?branch=master)| [Preview 6 v2.101.1](https://github.com/Microsoft/vsts-agent/releases/tag/v2.101.1) | June |
|![Win](docs/res/win_med.png) **Windows**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/2850/badge?branch=master)| [Preview 3 v2.101.1](https://github.com/Microsoft/vsts-agent/releases/tag/v2.101.1) | |

## Preview Support

This is the current state of what works with the preview.  

This agent can be used for the VSTS service and it replaces the node agent for TFS2015 On-Prem.

| Scenario | OSX/Unix | Windows | Comment |
|:-------------:|:-----:|:-----:|:-----:|
| VSTS Git      |  Yes  | Yes   |
| VSTS TfsVC    |  Yes  | Soon  |
| TFS2015 Git   |  Yes  | No    | Win use agent with 2015 |
| TFS2015 TfsVC |  Yes  | No    | Win use agent with 2015 |

## Get Started

### First, add the build account to the proper roles
    
[Read Here](docs/start/roles.md)

### Next, get the agent configured

![win](docs/res/win_sm.png)  [Start Windows](docs/start/startwin.md)  

![osx](docs/res/apple_sm.png)  [Start OSX](docs/start/startosx.md)  

![ubuntu](docs/res/ubuntu_sm.png)  [Start Ubuntu](docs/start/startubuntu.md)  

![redhat](docs/res/redhat_sm.png)  [Start RedHat](docs/start/startredhat.md)  

## Contribute

For developers that want to contribute, [read here](docs/contribute.md) on how to build and test.
