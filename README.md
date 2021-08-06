# GitHub Actions Runner + Server

[![Runner CI](https://github.com/ChristopherHX/runner.server/actions/workflows/build.yml/badge.svg)](https://github.com/ChristopherHX/runner.server/actions/workflows/build.yml)

This fork adds two executables to this Project, `Runner.Server` as a runner backend like github actions and `Runner.Client` to schedule workflows via commandline.

<p align="center">
  <img src="src/Runner.Server/webpage1.png">
</p>

## Usage
- [Download the Actions Runner Client + Server](https://github.com/ChristopherHX/runner/releases/latest)
- The installation directory needs to be accessible by docker file sharing
  - On linux (Docker) all non overlayfs folders should work
  - On macOs (Docker Desktop) you might need to add the install path to Docker File Sharing
  - On windows (Docker Desktop) you might need to accept all file sharing requests (hyper-v backend)
  - Docker Settings -> Resources -> File Sharing
  - [Tracking issue for macOS](https://github.com/ChristopherHX/runner.server/issues/72)
- Clone your github actions repo
- Run `Runner.Client`(`.exe`) (It is inside the bin folder of the package) inside your checkout

### Dotnet Tool (gharun)
The new nuget package [can be found here](https://www.nuget.org/packages/io.github.christopherhx.gharun)
- Install the dotnet sdk 5.0.x (https://dotnet.microsoft.com/download/dotnet/5.0)
- `dotnet tool install --global io.github.christopherhx.gharun`
- Run `gharun` like `Runner.Client`

## Troubleshooting

If you get an error like: 
```
Error: No runner is registered for the requested runs-on labels: [ubuntu-latest], please register and run a self-hosted runner with at least these labels...
```

Then you will need to add one of the following cli options, replace `ubuntu-latest` with the content between `runs-on labels: [` The labels here without spaces `]`
- to run it on your local machine `-P ubuntu-latest=-self-hosted`
- to run it in a docker container `-P ubuntu-latest=catthehacker/ubuntu:act-latest`
  For more docker images refer to https://github.com/nektos/act#runners

This Software reads [act configuration files](https://github.com/nektos/act#configuration), you can save this inside a `.actrc` in your current or home folder to avoid typing it in your commandline.

## Building

```
cd src/
dotnet msbuild ./dir.proj -t:GenerateConstant
cd Runner.Client
dotnet build
```

This builds both `Runner.Client` and `Runner.Server`.

## Advanced Usage

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

Run the unofficial runner

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
This Software contains reimplementations of some parts of the github server which aren't open source (yet?). 

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