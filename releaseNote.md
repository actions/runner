## Changes
* Improve job names in https://github.com/ChristopherHX/runner.server/pull/78
* Test call workflow with different case of inputs in https://github.com/ChristopherHX/runner.server/pull/81
* Test for case insensitive workflow_dispatch inputs in https://github.com/ChristopherHX/runner.server/pull/82
* Default for choice input in https://github.com/ChristopherHX/runner.server/pull/79
* Fix crash duplicated environment secrets in https://github.com/ChristopherHX/runner.server/pull/83
* Feature yaml env and secret file in https://github.com/ChristopherHX/runner.server/pull/84
* Runner.Client validate input value in https://github.com/ChristopherHX/runner.server/pull/86
* Allow custom runner base directory, shorten path in https://github.com/ChristopherHX/runner.server/pull/87
* Add github connect support / instance chaining in https://github.com/ChristopherHX/runner.server/pull/90
* rerun failed, ignore succeeded in previous rerun in https://github.com/ChristopherHX/runner.server/pull/91
* Fix database error for InMemory db during start in https://github.com/ChristopherHX/runner.server/pull/93
* Fix rerun artifacts in https://github.com/ChristopherHX/runner.server/pull/92
* Replace more case sensitive comparsions in https://github.com/ChristopherHX/runner.server/pull/94

## Fixes
- Fix bugs in concurrency implementation 
  - groups are case insensitive, `main` and `Main` are the same group
  - if a reusable workflow cancells itself workflow cancellation works
  - add support for concurrency of uses jobs
- Missing job completed status in Runner.Client
- Improve container path handling on windows
- Remove Minimatch dependency fix pattern matching
  - Pattern matching is now more verbose
- Fix success and failure functions one or more args 
  - Always return false if cancelled
- Add on.workflow_run.workflows filter support
- Fix inputs are caseinsensitive

## Features
- Update actions/runner to [v2.292.0](https://github.com/actions/runner/releases/tag/v2.292.0)
- job / step summary in webui and downloadable as special named artifact
- `secrets: inherit` is now supported for reusable workflows
- Add `--input` option workflow_dispatch subcommand

## Breaking Changes
- workflow_dispatch inputs context disabled, based on [Github Feedback](https://github.com/github/feedback/discussions/9092#discussioncomment-2453678) and [Issue Comment](https://github.com/actions/runner/issues/1483#issuecomment-1091025877)
  boolean workflow_dispatch values of the inputs context are actual booleans values like workflow_call inputs
- Changed non expression job.name match github, this changes required check names
- Validate permissions and jobs.*.secrets text value
- Based on github docs skipped => success, skipped required checks are no longer pending
- Upload attachments as artifact, you can now download ACTIONS_RUNNER_DEBUG logs as an artifact this may cause collisions with your artifacts
- `Runner.Client` / `gharun` ctrl-c behavior changed, depends on how often you press it
  1. cancel workflows
  2. force cancel workflows, ignore jobs.*.if and don't wait for finish ( NEW )
  3. kill Agents and server

## Windows x64
We recommend configuring the runner in a root folder of the Windows drive (e.g. "C:\actions-runner"). This will help avoid issues related to service identity folder permissions and long file path restrictions on Windows.

The following snipped needs to be run on `powershell`:
``` powershell
# Create a folder under the drive root
mkdir \actions-runner ; cd \actions-runner
# Download the latest runner package
Invoke-WebRequest -Uri https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-win-x64-<RUNNER_VERSION>.zip -OutFile actions-runner-win-x64-<RUNNER_VERSION>.zip
# Extract the installer
Add-Type -AssemblyName System.IO.Compression.FileSystem ;
[System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD\actions-runner-win-x64-<RUNNER_VERSION>.zip", "$PWD")
```

## OSX x64

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
```

## [Pre-release] OSX arm64 (Apple silicon)

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-osx-arm64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-osx-arm64-<RUNNER_VERSION>.tar.gz
```

## Linux x64

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
```

## Linux arm64

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
```

## Linux arm

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz
```

## Using your self hosted runner
For additional details about configuring, running, or shutting down the runner please check out our [product docs.](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/adding-self-hosted-runners)

## SHA-256 Checksums

The SHA-256 checksums for the packages included in this build are shown below:

- actions-runner-win-x64-<RUNNER_VERSION>.zip <!-- BEGIN SHA win-x64 --><WIN_X64_SHA><!-- END SHA win-x64 -->
- actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA osx-x64 --><OSX_X64_SHA><!-- END SHA osx-x64 -->
- actions-runner-osx-arm64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA osx-arm64 --><OSX_ARM64_SHA><!-- END SHA osx-arm64 -->
- actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-x64 --><LINUX_X64_SHA><!-- END SHA linux-x64 -->
- actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-arm64 --><LINUX_ARM64_SHA><!-- END SHA linux-arm64 -->
- actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-arm --><LINUX_ARM_SHA><!-- END SHA linux-arm -->