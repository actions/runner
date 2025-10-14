## What's Changed
* Update safe_sleep.sh for bug when scheduler is paused for more than 1 second by @horner in https://github.com/actions/runner/pull/3157
* Acknowledge runner request by @ericsciple in https://github.com/actions/runner/pull/3996
* Update Docker to v28.3.3 and Buildx to v0.27.0 by @github-actions[bot] in https://github.com/actions/runner/pull/3999
* Update dotnet sdk to latest version @8.0.413 by @github-actions[bot] in https://github.com/actions/runner/pull/4000
* Bump actions/attest-build-provenance from 2 to 3 by @dependabot[bot] in https://github.com/actions/runner/pull/4002
* Bump @typescript-eslint/eslint-plugin from 6.7.2 to 8.35.0 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/3920
* Bump husky from 8.0.3 to 9.1.7 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/3842
* Bump @vercel/ncc from 0.38.0 to 0.38.3 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/3841
* Bump eslint-plugin-github from 4.10.0 to 4.10.2 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/3180
* Bump typescript from 5.2.2 to 5.9.2 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4007
* chore: migrate Husky config from v8 to v9 format by @salmanmkc in https://github.com/actions/runner/pull/4003
* Map RUNNER_TEMP for container action by @ericsciple in https://github.com/actions/runner/pull/4011
* Break UseV2Flow into UseV2Flow and UseRunnerAdminFlow. by @TingluoHuang in https://github.com/actions/runner/pull/4013
* Update Docker to v28.4.0 and Buildx to v0.28.0 by @github-actions[bot] in https://github.com/actions/runner/pull/4020
* Bump node.js to latest version in runner. by @TingluoHuang in https://github.com/actions/runner/pull/4022
* feat: add automated .NET dependency management workflow by @salmanmkc in https://github.com/actions/runner/pull/4028
* feat: add automated Docker BuildX dependency management workflow by @salmanmkc in https://github.com/actions/runner/pull/4029
* feat: add automated Node.js version management workflow by @salmanmkc in https://github.com/actions/runner/pull/4026
* feat: add comprehensive NPM security management workflow by @salmanmkc in https://github.com/actions/runner/pull/4027
* feat: add comprehensive dependency monitoring system by @salmanmkc in https://github.com/actions/runner/pull/4025
* Use BrokerURL when using RunnerAdmin by @luketomlinson in https://github.com/actions/runner/pull/4044
* Bump actions/github-script from 7.0.1 to 8.0.0 by @dependabot[bot] in https://github.com/actions/runner/pull/4016
* Bump actions/stale from 9 to 10 by @dependabot[bot] in https://github.com/actions/runner/pull/4015
* fix: prevent Node.js upgrade workflow from creating PRs with empty versions by @salmanmkc in https://github.com/actions/runner/pull/4055
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4057
* Bump actions/setup-node from 4 to 5 by @dependabot[bot] in https://github.com/actions/runner/pull/4037
* Bump Azure.Storage.Blobs from 12.25.0 to 12.25.1 by @dependabot[bot] in https://github.com/actions/runner/pull/4058
* Update Docker to v28.5.0 and Buildx to v0.29.1 by @github-actions[bot] in https://github.com/actions/runner/pull/4069
* Bump github/codeql-action from 3 to 4 by @dependabot[bot] in https://github.com/actions/runner/pull/4072
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4075
* Include k8s novolume (version v0.8.0) by @nikola-jokic in https://github.com/actions/runner/pull/4063
* Make sure runner-admin has both auth_url and auth_url_v2. by @TingluoHuang in https://github.com/actions/runner/pull/4066
* Report job has infra failure to run-service by @TingluoHuang in https://github.com/actions/runner/pull/4073
* Bump actions/setup-node from 5 to 6 by @dependabot[bot] in https://github.com/actions/runner/pull/4078

## New Contributors
* @horner made their first contribution in https://github.com/actions/runner/pull/3157

**Full Changelog**: https://github.com/actions/runner/compare/v2.328.0...v2.329.0

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
