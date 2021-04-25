# GitHub Actions Runner + Server

[![Runner CI](https://github.com/ChristopherHX/runner.server/actions/workflows/build.yml/badge.svg)](https://github.com/ChristopherHX/runner.server/actions/workflows/build.yml)

This fork adds two executables to this Project, `Runner.Server` as a runner backend like github and `Runner.Client` to schedule workflows via commandline from a local `workflow.yml` and a local webhook `payload.json`.

<p align="center">
  <img src="src/Runner.Server/webpage1.png">
</p>

## Building

```
cd src/
dotnet msbuild ./dir.proj -t:GenerateConstant
cd Runner.Client
dotnet build
```

This builds both `Runner.Client` and `Runner.Server`.

## Usage

Create a Github Personal Access token (PAT) and replace the GITHUB_TOKEN property in `src\Runner.Server\appsettings.json` and `src\Runner.Server\appsettings.Development.json`.

[Download an unofficial Runner](https://github.com/ChristopherHX/runner/releases/latest).

Using port 5000 prevents offical unmodified runners to connect to the server, because the runner drops the port of the repository during configure. This fork has a patch applied to allow a random port.

Linux or macOS:
```
./bin/Runner.Server
```

Windows
```
.\bin\Runner.Server.exe
```

Open a 2nd Terminal

Setup the unofficial runner, you can type anything for registration and removal token authentication isn't implemented yet.

Linux or macOS:
```
./config.sh --unattended --url http://localhost:5000/runner/server --token "ThisIsIgnored"
```

Windows:
```
.\config.cmd --unattended --url http://localhost:5000/runner/server --token "ThisIsIgnored"
```

Run the official runner

Linux or macOS:
```
./run.sh
```

Windows:
```
.\run.cmd
```

Open a 3rd Terminal

Schedule one or more job's

Linux or macOS:
```
./bin/Runner.Client --workflow workflow.yml --event push --payload payload.json --server http://localhost:5000
```

Windows
```
.\bin\Runner.Client.exe --workflow workflow.yml --event push --payload payload.json --server http://localhost:5000
```

Open http://localhost:5000 to see the progress.

## Notes
This contains a reimplementations of some parts of the github server which aren't open source (yet?). 

- matrix parsing
- job parsing
- `on` parsing incl. filter
- api server of the open source client
- context creation
- scheduling
- job inputs / outputs, based on documentation

The following things will behave as expected

- expression evaluation
- step evaluation on the runner
- container actions

## Something not working?
Please open an issue at this fork, to get it fixed.

<p align="center">
  <img src="docs/res/github-graph.png">
</p>

# GitHub Actions Runner

The runner is the application that runs a job from a GitHub Actions workflow. It is used by GitHub Actions in the [hosted virtual environments](https://github.com/actions/virtual-environments), or you can [self-host the runner](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/about-self-hosted-runners) in your own environment.

## Get Started

For more information about installing and using self-hosted runners, see [Adding self-hosted runners](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/adding-self-hosted-runners) and [Using self-hosted runners in a workflow](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/using-self-hosted-runners-in-a-workflow)

Runner releases:

![win](docs/res/win_sm.png) [Pre-reqs](docs/start/envwin.md) | [Download](https://github.com/actions/runner/releases)  

![macOS](docs/res/apple_sm.png)  [Pre-reqs](docs/start/envosx.md) | [Download](https://github.com/actions/runner/releases)  

![linux](docs/res/linux_sm.png)  [Pre-reqs](docs/start/envlinux.md) | [Download](https://github.com/actions/runner/releases)

## Contribute

We accept contributions in the form of issues and pull requests.  [Read more here](docs/contribute.md) before contributing.
