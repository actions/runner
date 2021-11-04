## Changes
- Fixed Skipping jobs with a name using expression syntax leads to job failure. bug
- Fixed [PARITY] Runs-on key allows the inputs context bug
- Fixed [WEBUI] leading spaces in log are truncated bug
- Fixed Cannot call workflow_dispatch with custom inputs, always uses default if specified in event.json bug
- Allow calling reuseable workflows in the .github/workflows dir via `uses: ./.github/workflows/workflow1.yml`, however github may don't support it
- Fixed [WEBUI] joblist is truncated on job finish reload to fix
- Fixed [WEBUI] logs disappar on job finish reload or collapse and expand to fix
- Fixed commit sha for `pull_request_target` to be the head commit, to show in PR's
- Fixed including and excluding of maps and sequences in matrix strategy
- Fixed default Jobname if you specify maps or sequences as matrix keys, now traverse the values depth-first like github
- Fixed workflow_call: ignore case of secret for required
- Fixed cannot cancel Runner.Server from unix Terminal
- Status Checks are now queued per job and no longer per workflow
- Rerun workflow
- Rerun a single job, this won't update status checks or trigger other jobs
- Fix sample windows container of description to include a tag
- Added CI Tests for windows and linux container
- Updated runner to [v2.284.0](https://github.com/actions/runner/releases/tag/v2.284.0)
- Allow to configure a runner registration token, to restrict runner registrations
- No longer trigger `pull_request` events by default only `pull_request_target`, configure Runner.Server to override
- Updated ReadMe with more instructions
- Allow to successfuly replace runners, while this Server won't remove the old one
- No longer set runtimeframeworkversion to 5.0.0 in projects, releases might now correctly use the current sdk
- Added webhook secret validation
- Added `--no-default-payload` option, use with caution
- Security: More fine grained jwt access tokens in api server, based on the official github actions service

## Known Issues

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
