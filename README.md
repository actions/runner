# Visual Studio Team Services Build and Release Agent

## Overview

The cross platform build and release agent for Team Services and Team Foundation Server 2015 and beyond.  This will be replacing/combining the existing closed source windows build agent and the existing [xplat agent](https://github.com/Microsoft/vso-agent)

Supported on Windows, OSX, Ubuntu and Red Hat.  Written for the .NET Core CLR as one code base in C#.


## Status

A preview is available for Ubuntu, RedHat and OSX for VSTS.  The current preview is more feature complete than the node agent bringing Auto Update, Cancellation, Run as a svc on OSX and Linux, and Gated support.

What's missing from the preview? On-prem NTLM support is in the works so preview with Team Services. 

|   | Build & Test | Preview | Release |
|---|:-----:|:-----:|:-----:|
|![Apple](docs/apple_med.png) **OSX**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3080/badge?branch=master)| [Preview 4 v2.100.1](https://github.com/Microsoft/vsts-agent/releases/tag/v2.100.1) | June |
|![Ubuntu](docs/ubuntu_med.png) **Ubuntu**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/2853/badge?branch=master)| [Preview 4 v2.100.1](https://github.com/Microsoft/vsts-agent/releases/tag/v2.100.1) | June |
|![RedHat](docs/redhat_med.png) **RedHat**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3418/badge?branch=master)| [Preview 4 v2.100.1](https://github.com/Microsoft/vsts-agent/releases/tag/v2.100.1) | June |
|![Win](docs/win_med.png) **Windows**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/2850/badge?branch=master)| [Preview 1 v2.100.1](https://github.com/Microsoft/vsts-agent/releases/tag/v2.100.1) | |

## Configure Account and Roles

Add the build account to the proper roles.  [Read Here](docs/roles.md)

## Get Started

![win](docs/win_sm.png)  [Start Windows](docs/start/startwin.md)  

![osx](docs/apple_sm.png)  [Start OSX](docs/start/startosx.md)  

![ubuntu](docs/ubuntu_sm.png)  [Start Ubuntu](docs/start/startubuntu.md)  

![redhat](docs/redhat_sm.png)  [Start RedHat](docs/start/startredhat.md)  

## Configuration

More detailed configuration options are [covered here](docs/config.md)

## Contribute

For developers that want to contribute, [read here](docs/contribute.md) on how to build and test.
