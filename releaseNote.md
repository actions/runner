## What's Changed
* Increase error body max length before truncation by @ericsciple in https://github.com/actions/runner/pull/3762
* Fix release.yml break by upgrading actions/github-script by @TingluoHuang in https://github.com/actions/runner/pull/3772
* Small runner code cleanup. by @TingluoHuang in https://github.com/actions/runner/pull/3773
* Enable hostcontext to track auth migration. by @TingluoHuang in https://github.com/actions/runner/pull/3776
* Add option in OAuthCred to load authUrlV2. by @TingluoHuang in https://github.com/actions/runner/pull/3777
* Remove create session with broker in MessageListener. by @TingluoHuang in https://github.com/actions/runner/pull/3782
* Enable auth migration based on config refresh. by @TingluoHuang in https://github.com/actions/runner/pull/3786
* Set JWT.alg to PS256 with PssPadding. by @TingluoHuang in https://github.com/actions/runner/pull/3789
* Enable FIPS by default. by @TingluoHuang in https://github.com/actions/runner/pull/3793
* Support auth migration using authUrlV2 in Runner/MessageListener. by @TingluoHuang in https://github.com/actions/runner/pull/3787
* Cleanup feature flag actions_skip_retry_complete_job_upon_known_errors by @ericsciple in https://github.com/actions/runner/pull/3806
* Update dotnet sdk to latest version @8.0.408 by @github-actions in https://github.com/actions/runner/pull/3808
* Bump hook to 0.7.0 by @nikola-jokic in https://github.com/actions/runner/pull/3813
* Allow enable auth migration by default. by @TingluoHuang in https://github.com/actions/runner/pull/3804
* Do not retry /renewjob on 404 by @ericsciple in https://github.com/actions/runner/pull/3828
* Bump Microsoft.NET.Test.Sdk from 17.12.0 to 17.13.0 in /src by @dependabot in https://github.com/actions/runner/pull/3719
* Add copilot-instructions.md by @pje in https://github.com/actions/runner/pull/3810
* Bump actions/upload-release-asset from 1.0.1 to 1.0.2 by @dependabot in https://github.com/actions/runner/pull/3553
* Ignore exception during auth migration. by @TingluoHuang in https://github.com/actions/runner/pull/3835
* feat: default fromPath for problem matchers by @dsanders11 in https://github.com/actions/runner/pull/3802
* Bump Azure.Storage.Blobs from 12.23.0 to 12.24.0 in /src by @dependabot in https://github.com/actions/runner/pull/3837
* Bump nodejs version. by @TingluoHuang in https://github.com/actions/runner/pull/3840
* Feature-flagged support for `JobContext.CheckRunID` by @pje in https://github.com/actions/runner/pull/3811
* Bump System.ServiceProcess.ServiceController from 8.0.0 to 8.0.1 in /src by @dependabot in https://github.com/actions/runner/pull/3844
* Bump xunit.runner.visualstudio from 2.5.8 to 2.8.2 in /src by @dependabot in https://github.com/actions/runner/pull/3845
* Make sure the token's claims are match as expected. by @TingluoHuang in https://github.com/actions/runner/pull/3846
* Prefer _migrated config on startup by @lokesh755 in https://github.com/actions/runner/pull/3853
* Update docker and buildx by @TingluoHuang in https://github.com/actions/runner/pull/3854

## New Contributors
* @dsanders11 made their first contribution in https://github.com/actions/runner/pull/3802

**Full Changelog**: https://github.com/actions/runner/compare/v2.323.0...v2.324.0

_Note: Actions Runner follows a progressive release policy, so the latest release might not be available to your enterprise, organization, or repository yet.
To confirm which version of the Actions Runner you should expect, please view the download instructions for your enterprise, organization, or repository.
See https://docs.github.com/en/enterprise-cloud@latest/actions/hosting-your-own-runners/adding-self-hosted-runners_

## Windows x64

We recommend configuring the runner in a root folder of the Windows drive (e.g. "C:\actions-runner"). This will help avoid issues related to service identity folder permissions and long file path restrictions on Windows.

The following snipped needs to be run on `powershell`:

```powershell
# Create a folder under the drive root
mkdir \actions-runner ; cd \actions-runner
# Download the latest runner package
Invoke-WebRequest -Uri https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-win-x64-<RUNNER_VERSION>.zip -OutFile actions-runner-win-x64-<RUNNER_VERSION>.zip
# Extract the installer
Add-Type -AssemblyName System.IO.Compression.FileSystem ;
[System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD\actions-runner-win-x64-<RUNNER_VERSION>.zip", "$PWD")
```

## Windows arm64

We recommend configuring the runner in a root folder of the Windows drive (e.g. "C:\actions-runner"). This will help avoid issues related to service identity folder permissions and long file path restrictions on Windows.

The following snipped needs to be run on `powershell`:

```powershell
# Create a folder under the drive root
mkdir \actions-runner ; cd \actions-runner
# Download the latest runner package
Invoke-WebRequest -Uri https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-win-arm64-<RUNNER_VERSION>.zip -OutFile actions-runner-win-arm64-<RUNNER_VERSION>.zip
# Extract the installer
Add-Type -AssemblyName System.IO.Compression.FileSystem ;
[System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD\actions-runner-win-arm64-<RUNNER_VERSION>.zip", "$PWD")
```

## OSX x64

```bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
```

## OSX arm64 (Apple silicon)

```bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-osx-arm64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-osx-arm64-<RUNNER_VERSION>.tar.gz
```

## Linux x64

```bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
```

## Linux arm64

```bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
```

## Linux arm

```bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz
```

## Using your self hosted runner

For additional details about configuring, running, or shutting down the runner please check out our [product docs.](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/adding-self-hosted-runners)

## SHA-256 Checksums

The SHA-256 checksums for the packages included in this build are shown below:

- actions-runner-win-x64-<RUNNER_VERSION>.zip <!-- BEGIN SHA win-x64 --><WIN_X64_SHA><!-- END SHA win-x64 -->
- actions-runner-win-arm64-<RUNNER_VERSION>.zip <!-- BEGIN SHA win-arm64 --><WIN_ARM64_SHA><!-- END SHA win-arm64 -->
- actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA osx-x64 --><OSX_X64_SHA><!-- END SHA osx-x64 -->
- actions-runner-osx-arm64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA osx-arm64 --><OSX_ARM64_SHA><!-- END SHA osx-arm64 -->
- actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-x64 --><LINUX_X64_SHA><!-- END SHA linux-x64 -->
- actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-arm64 --><LINUX_ARM64_SHA><!-- END SHA linux-arm64 -->
- actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-arm --><LINUX_ARM_SHA><!-- END SHA linux-arm -->
