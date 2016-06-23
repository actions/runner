# Visual Studio Team Services Build and Release Agent

## Overview

The cross platform build and release agent for Team Services and Team Foundation Server 2015 and beyond.  This will be replacing/combining the existing closed source windows build agent and the existing [xplat agent](https://github.com/Microsoft/vso-agent)

Supported on Windows, OSX, Ubuntu and Red Hat.  Written for the .NET Core CLR as one code base in C#.


## Status

This agent is feature complete on all platforms and supported for production use.  

This agent replaces the node agent for OSX and Linux bringing Auto Update, Cancellation, Run as a svc on OSX and Linux, and Gated support.  

The windows agent is in RC2 awaiting final validation with internal dogfooding (currently self host, building VSTS etc...)  

|   | Build & Test | Preview | Release |
|---|:-----:|:-----:|:-----:|
|![Apple](docs/res/apple_med.png) **OSX**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3080/badge?branch=master)| [RC 2 v2.102.1](https://github.com/Microsoft/vsts-agent/releases/tag/v2.102.1) | RC2 is candidate |
|![Ubuntu](docs/res/ubuntu_med.png) **Ubuntu**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/2853/badge?branch=master)| [RC 2 v2.102.1](https://github.com/Microsoft/vsts-agent/releases/tag/v2.102.1) | RC2 is candidate |
|![RedHat](docs/res/redhat_med.png) **RedHat**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3418/badge?branch=master)| [RC 2 v2.102.1](https://github.com/Microsoft/vsts-agent/releases/tag/v2.102.1) | RC2 is candidate |
|![Win](docs/res/win_med.png) **Windows**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/2850/badge?branch=master)| [RC2 v2.102.1](https://github.com/Microsoft/vsts-agent/releases/tag/v2.102.1) | July |

## Preview Support

This agent can be used for the VSTS service and it replaces the node agent for TFS2015 On-Prem.

| Scenario | OSX/Unix | Windows | Comment |
|:-------------:|:-----:|:-----:|:-----:|
| VSTS      |  Yes  | Yes   |
| TFS2015 (onprem)   |  Yes  | No    | Windows use agent with 2015 |
| TFS.vNext (onprem)   |  Yes  | Yes    |  |

## Get Started

### First, add the build account to the proper roles
    
[Read Here](docs/start/roles.md)

### Next, get the agent configured

![win](docs/res/win_sm.png)  [Start Windows](docs/start/startwin.md)  

![osx](docs/res/apple_sm.png)  [Start OSX](docs/start/startosx.md)  

![ubuntu](docs/res/ubuntu_sm.png)  [Start Ubuntu](docs/start/startubuntu.md)  

![redhat](docs/res/redhat_sm.png)  [Start RedHat](docs/start/startredhat.md)  

## Troubleshooting

Troubleshooting tips are [located here](docs/troubleshooting.md)

## Contribute

For developers that want to contribute, [read here](docs/contribute.md) on how to build and test.
