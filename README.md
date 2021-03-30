# GitHub Actions Runner + Server

This fork adds two executables to this Project, `Runner.Server` as a runner backend like github and `Runner.Client` to schedule workflows via commandline from a local `workflow.yml` and a local webhook `payload.json`.

<p align="center">
  <img src="src/Runner.Server/webpage1.png">
</p>

## Building

```
cd src/Runner.Client
dotnet build
```
This builds both `Runner.Client` and `Runner.Server`.

## Usage

Create a Github Personal Access token (PAT) and replace the GITHUB_TOKEN propery in `src\Runner.Server\appsettings.json` and `src\Runner.Server\appsettings.Development.json`.

[Download an official Runner](https://github.com/actions/runner/releases/latest).

Start the `Runner.Server`, will have to use the default port http(s) port, or register runners will fail.
```
cd src/Runner.Server
dotnet run
```

Open a 2nd Terminal
Setup the official runner, you can type anything for registration and removal token authentication isn't implemented yet.
```
.\config.cmd --unattended --url http://localhost/runner/server --token "ThisIsIgnored"
```

Run the official runner

```
.\run.cmd
```

Open a 3rd Terminal
Schedule one or more job's
```
cd src/Runner.Client
dotnet run -- --workflow workflow.yml --event push --payload payload.json
```

Open http://localhost to see the progress.

## Notes
This contains a reimplementations of some parts of the github server which aren't open source (yet?). 

- matrix parsing
- job parsing
- `on` parsing incl. filter
- api server of the open source client
- context creation
- scheduling

## Something not working?
Please open an issue at this fork, to get it fixed.

<p align="center">
  <img src="docs/res/github-graph.png">
</p>

# GitHub Actions Runner

[![Actions Status](https://github.com/actions/runner/workflows/Runner%20CI/badge.svg)](https://github.com/actions/runner/actions)
[![Runner E2E Test](https://github.com/actions/runner/workflows/Runner%20E2E%20Test/badge.svg)](https://github.com/actions/runner/actions)

The runner is the application that runs a job from a GitHub Actions workflow. It is used by GitHub Actions in the [hosted virtual environments](https://github.com/actions/virtual-environments), or you can [self-host the runner](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/about-self-hosted-runners) in your own environment.

## Get Started

For more information about installing and using self-hosted runners, see [Adding self-hosted runners](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/adding-self-hosted-runners) and [Using self-hosted runners in a workflow](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/using-self-hosted-runners-in-a-workflow)

Runner releases:

![win](docs/res/win_sm.png) [Pre-reqs](docs/start/envwin.md) | [Download](https://github.com/actions/runner/releases)  

![macOS](docs/res/apple_sm.png)  [Pre-reqs](docs/start/envosx.md) | [Download](https://github.com/actions/runner/releases)  

![linux](docs/res/linux_sm.png)  [Pre-reqs](docs/start/envlinux.md) | [Download](https://github.com/actions/runner/releases)

## Contribute

We accept contributions in the form of issues and pull requests.  [Read more here](docs/contribute.md) before contributing.
