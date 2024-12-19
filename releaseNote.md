## What's Changed

* Fix release workflow to use distinct artifact names by @ericsciple in https://github.com/actions/runner/pull/3485
* Update dotnet sdk to latest version @6.0.425 by @github-actions in https://github.com/actions/runner/pull/3433
* add ref and type to job completion in run service by @yaananth in https://github.com/actions/runner/pull/3492
* Remove Broker Migration Message logging by @luketomlinson in https://github.com/actions/runner/pull/3493
* Bump dotnet SDK to dotnet 8. by @TingluoHuang in https://github.com/actions/runner/pull/3500
* Remove dotnet8 compatibility test. by @TingluoHuang in https://github.com/actions/runner/pull/3502
* Remove node16 from the runner. by @TingluoHuang in https://github.com/actions/runner/pull/3503
* send action name for run service by @yaananth in https://github.com/actions/runner/pull/3520
* Handle runner not found by @ericsciple in https://github.com/actions/runner/pull/3536
* Publish job telemetry to run-service. by @TingluoHuang in https://github.com/actions/runner/pull/3545
* Fetch repo-level runner groups from API in v2 flow by @lucavallin in https://github.com/actions/runner/pull/3546
* Allow runner to check service connection in background. by @TingluoHuang in https://github.com/actions/runner/pull/3542
* Expose ENV for cache service v2. by @TingluoHuang in https://github.com/actions/runner/pull/3548
* Update runner docker image. by @TingluoHuang in https://github.com/actions/runner/pull/3511
* Bump Azure.Storage.Blobs from 12.19.1 to 12.23.0 in /src by @dependabot in https://github.com/actions/runner/pull/3549
* fix dotnet-upgrade.yml to print right version by @TingluoHuang in https://github.com/actions/runner/pull/3550
* Update dotnet sdk to latest version @8.0.404 by @github-actions in https://github.com/actions/runner/pull/3552
* Configure dependabot to check github-actions updates by @Goooler in https://github.com/actions/runner/pull/3333
* Bump actions/checkout from 3 to 4 by @dependabot in https://github.com/actions/runner/pull/3556

## New Contributors
* @lucavallin made their first contribution in https://github.com/actions/runner/pull/3546
* @Goooler made their first contribution in https://github.com/actions/runner/pull/3333

**Full Changelog**: https://github.com/actions/runner/compare/v2.320.0...v2.321.0

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
Invoke-WebRequest -Uri https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-win-x64-<RUNNER_VERSION>.zip -OutFile actions-runner-win-x64-<RUNNER_VERSION>.zip
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
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
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
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
```

## Linux arm64

```bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
```

## Linux arm

```bash
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
- actions-runner-win-arm64-<RUNNER_VERSION>.zip <!-- BEGIN SHA win-arm64 --><WIN_ARM64_SHA><!-- END SHA win-arm64 -->
- actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA osx-x64 --><OSX_X64_SHA><!-- END SHA osx-x64 -->
- actions-runner-osx-arm64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA osx-arm64 --><OSX_ARM64_SHA><!-- END SHA osx-arm64 -->
- actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-x64 --><LINUX_X64_SHA><!-- END SHA linux-x64 -->
- actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-arm64 --><LINUX_ARM64_SHA><!-- END SHA linux-arm64 -->
- actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-arm --><LINUX_ARM_SHA><!-- END SHA linux-arm -->
