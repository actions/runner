# GitHub Actions Runner + Server

[![Runner CI](https://github.com/ChristopherHX/runner.server/actions/workflows/build.yml/badge.svg)](https://github.com/ChristopherHX/runner.server/actions/workflows/build.yml)

This fork adds two executables to this Project, `Runner.Server` as a runner backend like github and `Runner.Client` to schedule workflows via commandline from a local `workflow.yml` and a local webhook `payload.json`.

<p align="center">
  <img src="src/Runner.Server/webpage1.png">
</p>

## Usage
- [Download the Actions Runner Client + Server](https://github.com/ChristopherHX/runner/releases/latest)
- Extract it anywhere you want
- Clone your github actions repo
- Run `Runner.Client`(`.exe`) (It is inside the bin folder of the package) inside your checkout

## Commandline Options
```
Runner.Client:
  Run your workflows locally.

Usage:
  Runner.Client [options] [command]

Options:
  --workflow <workflow>                                Workflow(s) to run. Use multiple times to execute more workflows parallel.
  --server <server>                                    Runner.Server address, e.g. `http://localhost:5000` or
                                                       `https://localhost:5001`.
  -e, --eventpath, --payload <eventpath>               Webhook payload to send to the Runner.
  --event <event>                                      Which event to send to a worker, ignored if you use subcommands which
                                                       overriding the event. [default: push]
  --env <env>                                          Environment variable for your workflow, overrides keys from your env file.
                                                       E.g. `--env Name` or `--env Name=Value`. You will be asked for a value if
                                                       you add `--env name`, but no environment variable with name `name` exists.
  --env-file <env-file>                                Environment variables for your workflow. [default: .env]
  -s, --secret <secret>                                Secret for your workflow, overrides keys from your secrets file. E.g. `-s
                                                       Name` or `-s Name=Value`. You will be asked for a value if you add `--secret
                                                       name`, but no environment variable with name `name` exists.
  --secret-file <secret-file>                          Secrets for your workflow. [default: .secrets]
  -j, --job <job>                                      Job to run. If multiple jobs have the same name in multiple workflows, all
                                                       matching jobs will run. Use together with `--workflow <workflow>` to run
                                                       exact one job.
  -m, --matrix <matrix>                                Matrix filter e.g. `-m Key:value`, use together with `--job <job>`. Use
                                                       multiple times to filter more specifically. If you want to force a value to
                                                       be a string you need to quote it, e.g. `"-m Key:\"1\""` or `"-m Key:""1"""`
                                                       (requires shell escaping)
  -l, --list                                           List jobs for the selected event (defaults to push).
  -W, --workflows <workflows>                          Workflow file or directory which contains workflows, only used if no
                                                       `--workflow <workflow>` option is set. [default: .github/workflows]
  -P, --platform <platform>                            Platform mapping to run the workflow in a docker container (similar behavior
                                                       as using the container property of a workflow job) or host. E.g. `-P
                                                       ubuntu-latest=ubuntu:latest` (Docker Linux Container), `-P
                                                       ubuntu-latest=-self-hosted` (Local Machine), `-P
                                                       windows-latest=-self-hosted` (Local Machine), `-P
                                                       windows-latest=mcr.microsoft.com/windows/servercore` (Docker Windows
                                                       container, windows only), `-P macos-latest=-self-hosted` (Local Machine).
  -a, --actor <actor>                                  The login of the user who initiated the workflow run, ignored if already in
                                                       your event payload.
  -w, --watch                                          Run automatically on every file change.
  -q, --quiet                                          Display no progress in the cli.
  --privileged                                         Run the docker container under privileged mode, only applies to container
                                                       jobs using this Runner fork.
  --userns <userns>                                    Change the docker container linux user namespace, only applies to container
                                                       jobs using this Runner fork.
  --container-architecture <container-architecture>    Change the docker container platform, if docker supports it. Only applies to
                                                       container jobs using this Runner fork.
  --defaultbranch <defaultbranch>                      The default branch of your workflow run, ignored if already in your event
                                                       payload.
  -C, --directory <directory>                          Change the directory of your local repository, provided file or directory
                                                       names are still resolved relative to your current working directory.
  -v, --verbose                                        Print more details like server / runner logs to stdout.
  --parallel <parallel>                                Run n parallel runners, ignored if `--server <server>` is used. [default: 4]
  --version                                            Show version information
  -?, -h, --help                                       Show help and usage information

Commands:
  schedule                       Same as adding `--event schedule` to the cli, overrides any `--event <event>` option.
  workflow_dispatch              Same as adding `--event workflow_dispatch` to the cli, overrides any `--event <event>` option.
  repository_dispatch            Same as adding `--event repository_dispatch` to the cli, overrides any `--event <event>` option.
  check_run                      Same as adding `--event check_run` to the cli, overrides any `--event <event>` option.
  check_suite                    Same as adding `--event check_suite` to the cli, overrides any `--event <event>` option.
  create                         Same as adding `--event create` to the cli, overrides any `--event <event>` option.
  delete                         Same as adding `--event delete` to the cli, overrides any `--event <event>` option.
  deployment                     Same as adding `--event deployment` to the cli, overrides any `--event <event>` option.
  deployment_status              Same as adding `--event deployment_status` to the cli, overrides any `--event <event>` option.
  fork                           Same as adding `--event fork` to the cli, overrides any `--event <event>` option.
  gollum                         Same as adding `--event gollum` to the cli, overrides any `--event <event>` option.
  issue_comment                  Same as adding `--event issue_comment` to the cli, overrides any `--event <event>` option.
  issues                         Same as adding `--event issues` to the cli, overrides any `--event <event>` option.
  label                          Same as adding `--event label` to the cli, overrides any `--event <event>` option.
  milestone                      Same as adding `--event milestone` to the cli, overrides any `--event <event>` option.
  page_build                     Same as adding `--event page_build` to the cli, overrides any `--event <event>` option.
  project                        Same as adding `--event project` to the cli, overrides any `--event <event>` option.
  project_card                   Same as adding `--event project_card` to the cli, overrides any `--event <event>` option.
  project_column                 Same as adding `--event project_column` to the cli, overrides any `--event <event>` option.
  public                         Same as adding `--event public` to the cli, overrides any `--event <event>` option.
  pull_request                   Same as adding `--event pull_request` to the cli, overrides any `--event <event>` option.
  pull_request_review            Same as adding `--event pull_request_review` to the cli, overrides any `--event <event>` option.
  pull_request_review_comment    Same as adding `--event pull_request_review_comment` to the cli, overrides any `--event <event>`
                                 option.
  pull_request_target            Same as adding `--event pull_request_target` to the cli, overrides any `--event <event>` option.
  push                           Same as adding `--event push` to the cli, overrides any `--event <event>` option.
  registry_package               Same as adding `--event registry_package` to the cli, overrides any `--event <event>` option.
  release                        Same as adding `--event release` to the cli, overrides any `--event <event>` option.
  status                         Same as adding `--event status` to the cli, overrides any `--event <event>` option.
  watch                          Same as adding `--event watch` to the cli, overrides any `--event <event>` option.
  workflow_run                   Same as adding `--event workflow_run` to the cli, overrides any `--event <event>` option.
  startserver                    Starts a server listening on the supplied address or selects a random free http address.
  startrunner                    Configures and runs n runner.
```

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

## Coming from [act](https://github.com/nektos/act)?
This Software shares absolutly no source code with `act` and may behave differently.
Just replace `act` with `Runner.Client`(`.exe`) (It is inside the bin folder of the package).
`Runner.Client` doesn't have all commandline options of `act`, type `--help` and compare the available commandlineoptions.
Then you will be able to use
- `actions/cache@v2`
- `actions/upload-artifact@v2`
- `actions/download-artifact@v2`
- post run steps
- matrix filter from the cli, `--job test --matrix os:ubuntu-latest`. Repeat `--matrix` to filter more specifically, matches like github actions include.
- exact the same expression interpreter like on github

This implementation is more leightweight than act (Binary size is bigger, due to net5), it is fully compatible with the official github actions self-hosted runner and reuses it's sources.
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
