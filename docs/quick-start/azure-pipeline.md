# How to run Azure Pipelines locally.

[ChristopherHX/runner.server](https://github.com/ChristopherHX/runner.server) is a project that allows local development and testing of your pipelines.

This introduction guide will install the tool globally so you can easily use the command from any directory.

## Prerequisites

.Net Sdk 6 or 7


## Getting Started

### Install [ChristopherHX/runner.server](https://github.com/ChristopherHX/runner.server) as a dotnet tool

`dotnet tool install --global io.github.christopherhx.gharun`

#### Updating the dotnet tool

`dotnet tool update --global io.github.christopherhx.gharun`

### Open a terminal or command prompt in a directory that contains a pipeline.yml file.

`gharun --event azpipelines -W pipeline.yml`

The output of your pipeline should output to your cli.

## Useful commands

### Keep Web UI alive

By the runner server will spawn a web ui however it exits onces all tasks have completed.

To keep the server alive and also watch you directory for live changes run:

add `-w` or `--watch` to the command.

`gharun --event azpipelines -W pipeline.yml --watch`

### Multiple Workers/Parallel Jobs

To simulate multiple runners to use Jobs in parallel use this command.

`--parallel n`

where N is the number of runners.

`gharun --event azpipelines -W ./pipeline.yml --parallel 5`

### Using a specfic Azure Pipelines Agent Version

`--runner-version VERSION`

where VERSION is the tagname of [microsoft/azure-pipelines-agent](https://github.com/microsoft/azure-pipelines-agent/releases) without the `v` prefix.

`gharun --event azpipelines -W ./pipeline.yml --runner-version 3.217.1`

### Known Issues

See [GitHub Issues](https://github.com/ChristopherHX/runner.server/issues)

## Reference Azure Pipelines:

https://github.com/ChristopherHX/runner.server/tree/main/testworkflows/azpipelines

