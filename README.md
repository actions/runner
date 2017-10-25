# Visual Studio Team Services Agent

## Overview

The cross platform build and release agent for Team Services and Team Foundation Server 2015 and beyond.  This replaced the deprecated closed source windows build agent and the existing [xplat agent](https://github.com/Microsoft/vso-agent)

Supported on Windows, OSX, Ubuntu and Red Hat.  Written for the .NET Core CLR as one code base in C#.


## Status

|   | Build & Test |
|---|:-----:|
|![Win](docs/res/win_med.png) **Windows**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3531/badge?branch=master)| 
|![macOS](docs/res/apple_med.png) **macOS**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3080/badge?branch=master)| 
|![Linux](docs/res/linux_med.png) **Linux**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/4411/badge?branch=master)|


## System Pre-Requisites

First, ensure you have the necessary system prequisites

![win](docs/res/win_sm.png) [Windows](docs/start/envwin.md)    

![macOS](docs/res/apple_sm.png) [macOS](docs/start/envosx.md)    

![linux](docs/res/linux_sm.png) [Linux](docs/start/envlinux.md)    

## Get the Agent

Next, download and configure the agent

![win](docs/res/win_sm.png)  [Start Windows](https://www.visualstudio.com/en-us/docs/build/admin/agents/v2-windows)  

![macOS](docs/res/apple_sm.png)  [Start macOS](https://www.visualstudio.com/en-us/docs/build/admin/agents/v2-osx)  

![linux](docs/res/linux_sm.png)  [Start Linux](https://www.visualstudio.com/en-us/docs/build/admin/agents/v2-linux)  

## Supported Usage

This agent can be used for the VSTS service and it replaces the node agent for TFS2015 On-Prem.

| Scenario | OSX/Unix | Windows | Comment |
|:-------------:|:-----:|:-----:|:-----:|
| VSTS      |  Yes  | Yes   |
| TFS2015 (onprem)   |  Yes  | No    | Windows use agent with 2015 |
| TFS2017 (onprem)   |  Yes  | Yes    |  |
| TFS.vNext (onprem)   |  Yes  | Yes    |  |

## More Documentation

[Documentation Here](https://aka.ms/tfbuild)

## Troubleshooting

Troubleshooting tips are [located here](docs/troubleshooting.md)

## Contribute

For developers that want to contribute, [read here](docs/contribute.md) on how to build and test.
