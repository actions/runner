# Azure Pipelines Agent

## Overview

The cross platform build and release agent for Azure Pipelines and Team Foundation Server 2015 and beyond.  This replaced the deprecated closed source windows build agent and the existing [xplat agent](https://github.com/Microsoft/vso-agent)

Supported on Windows, OSX, Ubuntu and Red Hat.  Written for the .NET Core CLR as one code base in C#.


## Status

|   | Build & Test |
|---|:-----:|
|![Win-x64](docs/res/win_med.png) **Windows x64**|[![Build & Test][win-x64-build-badge]][build]| 
|![macOS](docs/res/apple_med.png) **macOS**|[![Build & Test][macOS-build-badge]][build]| 
|![Linux-x64](docs/res/linux_med.png) **Linux x64**|[![Build & Test][linux-x64-build-badge]][build]|

[win-x64-build-badge]: https://dev.azure.com/mseng/AzureDevOps/_apis/build/status/Products/Azure-pipelines-agent/actions-runner.ci?branchName=features/actionsrunner&jobName=Windows%20Agent%20(x64)
[macOS-build-badge]: https://dev.azure.com/mseng/AzureDevOps/_apis/build/status/Products/Azure-pipelines-agent/actions-runner.ci?branchName=features/actionsrunner&jobName=macOS%20Agent%20(x64)
[linux-x64-build-badge]: https://dev.azure.com/mseng/AzureDevOps/_apis/build/status/Products/Azure-pipelines-agent/actions-runner.ci?branchName=features/actionsrunner&jobName=Linux%20Agent%20(x64)
[build]: https://dev.azure.com/mseng/AzureDevOps/_build/latest?definitionId=8777&branchName=features/actionsrunner

## System Pre-Requisites

First, ensure you have the necessary system pre-requisites

![win](docs/res/win_sm.png) [Windows](docs/start/envwin.md)    

![macOS](docs/res/apple_sm.png) [macOS](docs/start/envosx.md)    

![linux](docs/res/linux_sm.png) [Linux and RHEL6](docs/start/envlinux.md)

## Get the Agent

Next, download and configure the agent

![win](docs/res/win_sm.png)  [Start Windows](https://www.visualstudio.com/en-us/docs/build/admin/agents/v2-windows)  

![macOS](docs/res/apple_sm.png)  [Start macOS](https://www.visualstudio.com/en-us/docs/build/admin/agents/v2-osx)  

![linux](docs/res/linux_sm.png)  [Start Linux](https://www.visualstudio.com/en-us/docs/build/admin/agents/v2-linux)  

## Supported Usage

This agent can be used for Azure Pipelines and it replaces the node agent for TFS2015 On-Prem.

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
