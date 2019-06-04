# GitHub Action Runner

## Overview

The cross platform action runner for GitHub Pipelines.  

Supported on Windows, OSX, Linux.  Written for the .NET Core CLR as one code base in C#.


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

![win](docs/res/win_sm.png)  [Start Windows](https://github.com/actions/runner/releases/latest)  

![macOS](docs/res/apple_sm.png)  [Start macOS](https://github.com/actions/runner/releases/latest)  

![linux](docs/res/linux_sm.png)  [Start Linux](https://github.com/actions/runner/releases/latest)  

## Contribute

For developers that want to contribute, [read here](docs/contribute.md) on how to build and test.
