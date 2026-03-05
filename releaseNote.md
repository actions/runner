## What's Changed
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4200
* Update dotnet sdk to latest version @8.0.417 by @github-actions[bot] in https://github.com/actions/runner/pull/4201
* Bump System.Formats.Asn1 and System.Security.Cryptography.Pkcs by @dependabot[bot] in https://github.com/actions/runner/pull/4202
* Allow empty container options by @ericsciple in https://github.com/actions/runner/pull/4208
* Update Docker to v29.1.5 and Buildx to v0.31.0 by @github-actions[bot] in https://github.com/actions/runner/pull/4212
* Report job level annotations by @TingluoHuang in https://github.com/actions/runner/pull/4216
* Fix local action display name showing `Run /./` instead of `Run ./` by @ericsciple in https://github.com/actions/runner/pull/4218
* Update Docker to v29.2.0 and Buildx to v0.31.1 by @github-actions[bot] in https://github.com/actions/runner/pull/4219
* Add support for libssl3 and libssl3t64 for newer Debian/Ubuntu versions by @nekketsuuu in https://github.com/actions/runner/pull/4213
* Validate work dir during runner start up. by @TingluoHuang in https://github.com/actions/runner/pull/4227
* Bump hook to 0.8.1 by @nikola-jokic in https://github.com/actions/runner/pull/4222
* Support return job result as exitcode in hosted runner. by @TingluoHuang in https://github.com/actions/runner/pull/4233
* Add telemetry tracking for deprecated set-output and save-state commands by @ericsciple in https://github.com/actions/runner/pull/4221
* Fix parser comparison mismatches by @ericsciple in https://github.com/actions/runner/pull/4220
* Remove unnecessary connection test during some registration flows by @zarenner in https://github.com/actions/runner/pull/4244
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4249
* Update dotnet sdk to latest version @8.0.418 by @github-actions[bot] in https://github.com/actions/runner/pull/4250
* Fix link to SECURITY.md in README by @TingluoHuang in https://github.com/actions/runner/pull/4253
* Try to infer runner is on hosted/ghes when githuburl is empty. by @TingluoHuang in https://github.com/actions/runner/pull/4254
* Add Node.js 20 deprecation warning annotation (Phase 1) by @salmanmkc in https://github.com/actions/runner/pull/4242
* Update Node.js 20 deprecation date to June 2nd, 2026 by @salmanmkc in https://github.com/actions/runner/pull/4258
* Composite Action Step Markers by @ericsciple in https://github.com/actions/runner/pull/4243
* Symlink actions cache by @paveliak in https://github.com/actions/runner/pull/4260
* Bump minimatch in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4261
* Bump @stylistic/eslint-plugin from 3.1.0 to 5.9.0 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4257

## New Contributors
* @nekketsuuu made their first contribution in https://github.com/actions/runner/pull/4213
* @zarenner made their first contribution in https://github.com/actions/runner/pull/4244

**Full Changelog**: https://github.com/actions/runner/compare/v2.331.0...v2.332.0

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
