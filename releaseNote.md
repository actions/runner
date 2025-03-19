## What's Changed
* Bump docker/login-action from 2 to 3 by @dependabot in https://github.com/actions/runner/pull/3673
* Bump actions/stale from 8 to 9 by @dependabot in https://github.com/actions/runner/pull/3554
* Bump docker/build-push-action from 3 to 6 by @dependabot in https://github.com/actions/runner/pull/3674
* update node version from 20.18.0 -> 20.18.2 by @aiqiaoy in https://github.com/actions/runner/pull/3682
* Pass BillingOwnerId through Acquire/Complete calls by @luketomlinson in https://github.com/actions/runner/pull/3689
* Do not retry CompleteJobAsync for known non-retryable errors by @ericsciple in https://github.com/actions/runner/pull/3696
* Update dotnet sdk to latest version @8.0.406 by @github-actions in https://github.com/actions/runner/pull/3712
* Update Dockerfile with new docker and buildx versions by @thboop in https://github.com/actions/runner/pull/3680
* chore: remove redundant words by @finaltrip in https://github.com/actions/runner/pull/3705
* fix: actions feedback link is incorrect by @Yaminyam in https://github.com/actions/runner/pull/3165
* Bump actions/github-script from 0.3.0 to 7.0.1 by @dependabot in https://github.com/actions/runner/pull/3557
* Docker container provenance by @paveliak in https://github.com/actions/runner/pull/3736
* Add request-id to http eventsource trace. by @TingluoHuang in https://github.com/actions/runner/pull/3740
* Update Bocker and Buildx version to mitigate images scanners alerts by @Blizter in https://github.com/actions/runner/pull/3750
* Fix typo, add invariant culture to timestamp for workflow log reporting by @GhadimiR in https://github.com/actions/runner/pull/3749
* Create vssconnection to actions service when URL provided. by @TingluoHuang in https://github.com/actions/runner/pull/3751
* Housekeeping: Update npm packages and node version by @thboop in https://github.com/actions/runner/pull/3752
* Improve the out-of-date warning message. by @tecimovic in https://github.com/actions/runner/pull/3595
* Update dotnet sdk to latest version @8.0.407 by @github-actions in https://github.com/actions/runner/pull/3753
* Exit hosted runner cleanly during deprovisioning. by @TingluoHuang in https://github.com/actions/runner/pull/3755
* Send annotation title to run-service. by @TingluoHuang in https://github.com/actions/runner/pull/3757
* Allow server enforce runner settings. by @TingluoHuang in https://github.com/actions/runner/pull/3758
* Support refresh runner configs with pipelines service. by @TingluoHuang in https://github.com/actions/runner/pull/3706

## New Contributors
* @finaltrip made their first contribution in https://github.com/actions/runner/pull/3705
* @Yaminyam made their first contribution in https://github.com/actions/runner/pull/3165
* @Blizter made their first contribution in https://github.com/actions/runner/pull/3750
* @GhadimiR made their first contribution in https://github.com/actions/runner/pull/3749
* @tecimovic made their first contribution in https://github.com/actions/runner/pull/3595

**Full Changelog**: https://github.com/actions/runner/compare/v2.322.0...v2.323.0

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
