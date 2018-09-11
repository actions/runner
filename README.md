# Azure Pipelines Agent

## Overview

The cross platform build and release agent for Azure Pipelines and Team Foundation Server 2015 and beyond.  This replaced the deprecated closed source windows build agent and the existing [xplat agent](https://github.com/Microsoft/vso-agent)

Supported on Windows, OSX, Ubuntu and Red Hat.  Written for the .NET Core CLR as one code base in C#.


## Status

|   | Build & Test |
|---|:-----:|
|![Win](docs/res/win_med.png) **Windows**|[![Build & Test][win-build-badge]][win-build]| 
|![macOS](docs/res/apple_med.png) **macOS**|[![Build & Test][macOS-build-badge]][macOS-build]| 
|![Linux](docs/res/linux_med.png) **Linux**|[![Build & Test][linux-build-badge]][linux-build]|

[win-build-badge]: https://dev.azure.com/mseng/pipelinetools/_apis/build/status/azure-pipelines-agent.ci-windows?branchName=master
[win-build]: https://dev.azure.com/mseng/pipelinetools/_build?_a=completed&definitionId=6916

[macOS-build-badge]: https://dev.azure.com/mseng/pipelinetools/_apis/build/status/azure-pipelines-agent.ci-macos?branchName=master
[macOS-build]: https://dev.azure.com/mseng/pipelinetools/_build?_a=completed&definitionId=6917

[linux-build-badge]: https://dev.azure.com/mseng/pipelinetools/_apis/build/status/azure-pipelines-agent.ci-linux?branchName=master
[linux-build]: https://dev.azure.com/mseng/pipelinetools/_build?_a=completed&definitionId=6915

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
| Azure Pipelines      |  Yes  | Yes   |
| TFS2015 (onprem)   |  Yes  | No    | Windows use agent with 2015 |
| TFS2017 (onprem)   |  Yes  | Yes    |  |
| TFS2018 (onprem)   |  Yes  | Yes    |  |

## More Documentation

[Documentation Here](https://aka.ms/tfbuild)

## Troubleshooting

Troubleshooting tips are [located here](docs/troubleshooting.md)

## Contribute

For developers that want to contribute, [read here](docs/contribute.md) on how to build and test.
