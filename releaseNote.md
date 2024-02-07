## What's Changed
* Bump docker and buildx to the latest version by @diogotorres97 in https://github.com/actions/runner/pull/3100
* Revert "Bump container hook version to 0.5.0 in runner image (#3003)" by @TingluoHuang in https://github.com/actions/runner/pull/3101
* Make embedded timeline record has same order as its parent record. by @TingluoHuang in https://github.com/actions/runner/pull/3113
* Fix release workflow. by @TingluoHuang in https://github.com/actions/runner/pull/3102
* Add user-agent to all http clients using RawClientHttpRequestSettings. by @TingluoHuang in https://github.com/actions/runner/pull/3115
* Fix JobDispatcher crash during force cancellation. by @TingluoHuang in https://github.com/actions/runner/pull/3118
* Implement Broker Redirects for Session and Messages by @luketomlinson in https://github.com/actions/runner/pull/3103
* Only keep 1 older version runner around after self-upgrade. by @TingluoHuang in https://github.com/actions/runner/pull/3122
* Upgrade `docker` from `24.0.7` to `24.0.8` by @MPV in https://github.com/actions/runner/pull/3124
* Upload the diagnostic logs to the Results Service by @jtamsut in https://github.com/actions/runner/pull/3114
* Upgrade `docker` from `24.0.8` to `24.0.9` by @MPV in https://github.com/actions/runner/pull/3126
* Make sure to drain the upload queue before clean temp directory by @yacaovsnc in https://github.com/actions/runner/pull/3125
* Specify `Content-Type` for BlockBlob upload by @bethanyj28 in https://github.com/actions/runner/pull/3119
* Improve error report for invalid action.yml by @jsoref in https://github.com/actions/runner/pull/3106
* Add sshd to .devcontainer.json by @pje in https://github.com/actions/runner/pull/3079
* Resolve CVE-2024-21626 by @luka5 in https://github.com/actions/runner/pull/3123
* Handle ForceTokenRefresh message by @luketomlinson in https://github.com/actions/runner/pull/3133

## New Contributors
* @diogotorres97 made their first contribution in https://github.com/actions/runner/pull/3100
* @MPV made their first contribution in https://github.com/actions/runner/pull/3124
* @jtamsut made their first contribution in https://github.com/actions/runner/pull/3114
* @luka5 made their first contribution in https://github.com/actions/runner/pull/3123

**Full Changelog**: https://github.com/actions/runner/compare/v2.312.0...v2.313.0

_Note: Actions Runner follows a progressive release policy, so the latest release might not be available to your enterprise, organization, or repository yet.
To confirm which version of the Actions Runner you should expect, please view the download instructions for your enterprise, organization, or repository.
See https://docs.github.com/en/enterprise-cloud@latest/actions/hosting-your-own-runners/adding-self-hosted-runners_

## Windows x64
We recommend configuring the runner in a root folder of the Windows drive (e.g. "C:\actions-runner"). This will help avoid issues related to service identity folder permissions and long file path restrictions on Windows.

The following snipped needs to be run on `powershell`:
``` powershell
# Create a folder under the drive root
mkdir \actions-runner ; cd \actions-runner
# Download the latest runner package
Invoke-WebRequest -Uri https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-win-x64-<RUNNER_VERSION>.zip -OutFile actions-runner-win-x64-<RUNNER_VERSION>.zip
# Extract the installer
Add-Type -AssemblyName System.IO.Compression.FileSystem ;
[System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD\actions-runner-win-x64-<RUNNER_VERSION>.zip", "$PWD")
```

## [Pre-release] Windows arm64
**Warning:** Windows arm64 runners are currently in preview status and use [unofficial versions of nodejs](https://unofficial-builds.nodejs.org/). They are not intended for production workflows.

We recommend configuring the runner in a root folder of the Windows drive (e.g. "C:\actions-runner"). This will help avoid issues related to service identity folder permissions and long file path restrictions on Windows.

The following snipped needs to be run on `powershell`:
``` powershell
# Create a folder under the drive root
mkdir \actions-runner ; cd \actions-runner
# Download the latest runner package
Invoke-WebRequest -Uri https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-win-arm64-<RUNNER_VERSION>.zip -OutFile actions-runner-win-arm64-<RUNNER_VERSION>.zip
# Extract the installer
Add-Type -AssemblyName System.IO.Compression.FileSystem ;
[System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD\actions-runner-win-arm64-<RUNNER_VERSION>.zip", "$PWD")
```

## OSX x64

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
```

## OSX arm64 (Apple silicon)

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
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
```

## Linux arm64

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
```

## Linux arm

``` bash
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
