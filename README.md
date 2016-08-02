# Visual Studio Team Services Agent

## Overview

The cross platform build and release agent for Team Services and Team Foundation Server 2015 and beyond.  This replaced the deprecated closed source windows build agent and the existing [xplat agent](https://github.com/Microsoft/vso-agent)

Supported on Windows, OSX, Ubuntu and Red Hat.  Written for the .NET Core CLR as one code base in C#.


## Status

|   | Build & Test |
|---|:-----:|
|![Apple](docs/res/apple_med.png) **OSX**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3080/badge?branch=master)| 
|![Ubuntu14](docs/res/ubuntu_med.png) **Ubuntu 14.04**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/2853/badge?branch=master)|
|![Ubuntu16](docs/res/ubuntu_med.png) **Ubuntu 16.04**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3742/badge?branch=master)| 
|![RedHat](docs/res/redhat_med.png) **RedHat**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/3418/badge?branch=master)| 
|![Win](docs/res/win_med.png) **Windows**|![Build & Test](https://mseng.visualstudio.com/_apis/public/build/definitions/b924d696-3eae-4116-8443-9a18392d8544/2850/badge?branch=master)| 


## Get Started

### System Pre-Requisites

![win](../res/win_med.png) [Windows](../start/envwin.md)    

![osx](../res/apple_med.png) [OSX](../start/envosx.md)  

![ubuntu](../res/ubuntu_med.png) [Ubuntu](../start/envubuntu.md)  

![redhat](../res/redhat_med.png) [RedHat & CentOS](../start/envredhat.md)  

### Get the Agent

![win](docs/res/win_sm.png)  [Start Windows](https://www.visualstudio.com/en-us/docs/build/admin/agents/v2-windows)  

![osx](docs/res/apple_sm.png)  [Start OSX](https://www.visualstudio.com/en-us/docs/build/admin/agents/v2-osx)  

![ubuntu](docs/res/ubuntu_sm.png)  [Start Ubuntu](https://www.visualstudio.com/en-us/docs/build/admin/agents/v2-linux)  

![redhat](docs/res/redhat_sm.png)  [Start RedHat](https://www.visualstudio.com/en-us/docs/build/admin/agents/v2-linux)  

## Supported Usage

This agent can be used for the VSTS service and it replaces the node agent for TFS2015 On-Prem.

| Scenario | OSX/Unix | Windows | Comment |
|:-------------:|:-----:|:-----:|:-----:|
| VSTS      |  Yes  | Yes   |
| TFS2015 (onprem)   |  Yes  | No    | Windows use agent with 2015 |
| TFS.vNext (onprem)   |  Yes  | Yes    |  |


## Troubleshooting

Troubleshooting tips are [located here](docs/troubleshooting.md)

## Contribute

For developers that want to contribute, [read here](docs/contribute.md) on how to build and test.
