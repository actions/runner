## What's Changed
* Custom Image: Preflight checks by @lawrencegripper in https://github.com/actions/runner/pull/4081
* Update dotnet sdk to latest version @8.0.415 by @github-actions[bot] in https://github.com/actions/runner/pull/4080
* Link to an extant discussion category by @jsoref in https://github.com/actions/runner/pull/4084
* Improve logic around decide IsHostedServer. by @TingluoHuang in https://github.com/actions/runner/pull/4086
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4093
* Compare updated template evaluator by @ericsciple in https://github.com/actions/runner/pull/4092
* fix(dockerfile): set more lenient permissions on /home/runner by @caxu-rh in https://github.com/actions/runner/pull/4083
* Add support for libicu73-76 for newer Debian/Ubuntu versions by @lets-build-an-ocean in https://github.com/actions/runner/pull/4098
* Bump actions/download-artifact from 5 to 6 by @dependabot[bot] in https://github.com/actions/runner/pull/4089
* Bump actions/upload-artifact from 4 to 5 by @dependabot[bot] in https://github.com/actions/runner/pull/4088
* Bump Azure.Storage.Blobs from 12.25.1 to 12.26.0 by @dependabot[bot] in https://github.com/actions/runner/pull/4077
* Only start runner after network is online by @dupondje in https://github.com/actions/runner/pull/4094
* Retry http error related to DNS resolution failure. by @TingluoHuang in https://github.com/actions/runner/pull/4110
* Update Docker to v29.0.1 and Buildx to v0.30.0 by @github-actions[bot] in https://github.com/actions/runner/pull/4114
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4115
* Update dotnet sdk to latest version @8.0.416 by @github-actions[bot] in https://github.com/actions/runner/pull/4116
* Compare updated workflow parser for ActionManifestManager by @ericsciple in https://github.com/actions/runner/pull/4111
* Bump npm pkg version for hashFiles. by @TingluoHuang in https://github.com/actions/runner/pull/4122

## New Contributors
* @lawrencegripper made their first contribution in https://github.com/actions/runner/pull/4081
* @caxu-rh made their first contribution in https://github.com/actions/runner/pull/4083
* @lets-build-an-ocean made their first contribution in https://github.com/actions/runner/pull/4098
* @dupondje made their first contribution in https://github.com/actions/runner/pull/4094

**Full Changelog**: https://github.com/actions/runner/compare/v2.329.0...v2.330.0

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
