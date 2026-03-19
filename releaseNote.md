## What's Changed
* Log inner exception message. by @TingluoHuang in https://github.com/actions/runner/pull/4265
* Fix composite post-step marker display names by @ericsciple in https://github.com/actions/runner/pull/4267
* Bump actions/download-artifact from 7 to 8 by @dependabot[bot] in https://github.com/actions/runner/pull/4269
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4272
* Avoid throw in SelfUpdaters. by @TingluoHuang in https://github.com/actions/runner/pull/4274
* Fix parser comparison mismatches by @ericsciple in https://github.com/actions/runner/pull/4273
* Devcontainer: bump base image Ubuntu version by @MaxHorstmann in https://github.com/actions/runner/pull/4277
* Support `entrypoint` and `command` for service containers by @ericsciple in https://github.com/actions/runner/pull/4276
* Bump actions/upload-artifact from 6 to 7 by @dependabot[bot] in https://github.com/actions/runner/pull/4270
* Bump docker/login-action from 3 to 4 by @dependabot[bot] in https://github.com/actions/runner/pull/4278
* Fix positional arg bug in ExpressionParser.CreateTree by @ericsciple in https://github.com/actions/runner/pull/4279
* Bump docker/build-push-action from 6 to 7 by @dependabot[bot] in https://github.com/actions/runner/pull/4283
* Bump docker/setup-buildx-action from 3 to 4 by @dependabot[bot] in https://github.com/actions/runner/pull/4282
* Bump actions/attest-build-provenance from 3 to 4 by @dependabot[bot] in https://github.com/actions/runner/pull/4266
* Bump @stylistic/eslint-plugin from 5.9.0 to 5.10.0 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4281
* Update Docker to v29.3.0 and Buildx to v0.32.1 by @github-actions[bot] in https://github.com/actions/runner/pull/4286
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4287
* Fix cancellation token race during parser comparison by @ericsciple in https://github.com/actions/runner/pull/4280
* Bump @typescript-eslint/eslint-plugin from 8.47.0 to 8.54.0 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4230
* Exit with specified exit code when runner is outdated by @nikola-jokic in https://github.com/actions/runner/pull/4285
* Report infra_error for action download failures. by @TingluoHuang in https://github.com/actions/runner/pull/4294
* Update dotnet sdk to latest version @8.0.419 by @github-actions[bot] in https://github.com/actions/runner/pull/4301
* Node 24 enforcement + Linux ARM32 deprecation support by @salmanmkc in https://github.com/actions/runner/pull/4303
* Bump @typescript-eslint/eslint-plugin from 8.54.0 to 8.57.1 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4304

## New Contributors
* @MaxHorstmann made their first contribution in https://github.com/actions/runner/pull/4277

**Full Changelog**: https://github.com/actions/runner/compare/v2.332.0...v2.333.0

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
