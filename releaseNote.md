## What's Changed
* Bump System.ServiceProcess.ServiceController from 10.0.6 to 10.0.7 by @dependabot[bot] in https://github.com/actions/runner/pull/4370
* Bump @actions/glob from 0.6.1 to 0.7.0 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4367
* feat: propagate actions dependencies by @nodeselector in https://github.com/actions/runner/pull/4372
* Not retry and report action download 403. by @TingluoHuang in https://github.com/actions/runner/pull/4391
* Update setup job starting logs by @GitPaulo in https://github.com/actions/runner/pull/4383
* fix: expand commit hash regex to support SHA-256 (64-char) hashes by @yaananth in https://github.com/actions/runner/pull/4347
* Move dap setup to setup job step by @rentziass in https://github.com/actions/runner/pull/4403
* Add support for Ubuntu 26.04 (liblttng-ust1t64, libicu77-80) by @dvaldivia in https://github.com/actions/runner/pull/4394
* Update dotnet sdk to latest version @8.0.421 by @github-actions[bot] in https://github.com/actions/runner/pull/4428
* Update Docker to v29.5.0 and Buildx to v0.34.0 by @github-actions[bot] in https://github.com/actions/runner/pull/4425
* Execute debugger REPL commands inside job container by @rentziass in https://github.com/actions/runner/pull/4420
* Send welcome message in debugger console on connect by @rentziass in https://github.com/actions/runner/pull/4419
* Update snapshot-if context and functions by @drielenr in https://github.com/actions/runner/pull/4443
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4452
* Allow disable node v8 maglev jit compiler on node24. by @TingluoHuang in https://github.com/actions/runner/pull/4447
* Update Node 24 default date to June 16th, 2026 by @salmanmkc in https://github.com/actions/runner/pull/4462
* Populate telemetry for non-action post-job steps by @drielenr in https://github.com/actions/runner/pull/4463
* Add SDK types and results plumbing for background step control by @lokesh755 in https://github.com/actions/runner/pull/4472
* Add job execution view model by @rentziass in https://github.com/actions/runner/pull/4470
* Add thread-safety locks to StepsContext by @lokesh755 in https://github.com/actions/runner/pull/4475
* Add background step deferral infrastructure and metadata plumbing by @lokesh755 in https://github.com/actions/runner/pull/4479
* Wire job execution view into DAP by @rentziass in https://github.com/actions/runner/pull/4471
* Background steps execution engine by @lokesh755 in https://github.com/actions/runner/pull/4476
* Update Docker to v29.5.2 and Buildx to v0.34.1 by @github-actions[bot] in https://github.com/actions/runner/pull/4451
* BrokerServer should not retry on 401. by @TingluoHuang in https://github.com/actions/runner/pull/4445
* Add new env var to allow single-prefix multiline logs on stdout by @nuclearpidgeon in https://github.com/actions/runner/pull/4424
* Bump Microsoft.DevTunnels.Connections from 1.3.39 to 1.3.48 by @dependabot[bot] in https://github.com/actions/runner/pull/4441
* Bump System.Formats.Asn1 and System.Security.Cryptography.Pkcs by @dependabot[bot] in https://github.com/actions/runner/pull/4369

## New Contributors
* @GitPaulo made their first contribution in https://github.com/actions/runner/pull/4383
* @dvaldivia made their first contribution in https://github.com/actions/runner/pull/4394
* @drielenr made their first contribution in https://github.com/actions/runner/pull/4443
* @nuclearpidgeon made their first contribution in https://github.com/actions/runner/pull/4424

**Full Changelog**: https://github.com/actions/runner/compare/v2.334.0...v2.335.0

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
