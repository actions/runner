## Changes
- Fixed broken gitea / ghes compatibility of 3.3.0-3.3.2, `http://localhost:5000/api/v1` dropped v1 in url building after refactoring
  - TODO add test cases
- Improved reuseable workflow support
  - Status checks should now match github
  - Added input / secrets declaration support for `workflow_call`, no longer cause an error
  - Validate `workflow_call` inputs
- Validate and apply defaults to `workflow_dispatch` events
- Runner.Client / gharun now prints the job status after completion, also for previously invisible skipped jobs
- Execute status check updates sequentially for each workflow run, avoid races
- Runner.Client / gharun now prints the workflow name in the logoutput instead of only the jobname
- Runner.Client / gharun now prints the webpage url in watch mode, if you want to use the webui instead of a console window
- webui: show job status of completed jobs in the headline
- webui: hide cancel button in completed jobs
- webui: probably show skipped jobs in job list instead of telling the job is queued

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

## OSX

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
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
- actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-x64 --><LINUX_X64_SHA><!-- END SHA linux-x64 -->
- actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-arm64 --><LINUX_ARM64_SHA><!-- END SHA linux-arm64 -->
- actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-arm --><LINUX_ARM_SHA><!-- END SHA linux-arm -->
