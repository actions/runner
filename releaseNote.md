## What's Changed
* Bump flatted from 3.2.7 to 3.4.2 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4307
* Add DAP server by @rentziass in https://github.com/actions/runner/pull/4298
* Bump @typescript-eslint/eslint-plugin from 8.57.1 to 8.57.2 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4310
* Remove AllowCaseFunction feature flag by @ericsciple in https://github.com/actions/runner/pull/4316
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4319
* Batch and deduplicate action resolution across composite depths by @stefanpenner in https://github.com/actions/runner/pull/4296
* Add support for Bearer token in action archive downloads by @TingluoHuang in https://github.com/actions/runner/pull/4321
* Bump brace-expansion in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4318
* Add devtunnel connection for debugger jobs by @rentziass in https://github.com/actions/runner/pull/4317
* Update Docker to v29.3.1 and Buildx to v0.33.0 by @github-actions[bot] in https://github.com/actions/runner/pull/4324
* Bump @typescript-eslint/eslint-plugin from 8.57.2 to 8.58.1 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4327
* Bump actions/github-script from 8 to 9 by @dependabot[bot] in https://github.com/actions/runner/pull/4331
* Bump typescript from 5.9.3 to 6.0.2 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4329
* fix: only show changed versions in node upgrade PR description by @salmanmkc in https://github.com/actions/runner/pull/4332
* Bump System.Formats.Asn1, Cryptography.Pkcs, ProtectedData, ServiceController, CodePages, Threading.Channels, @actions/glob, @typescript-eslint/parser, lint-staged, picomatch by @Copilot in https://github.com/actions/runner/pull/4333
* feat: add `job.workflow_*` typed accessors to JobContext by @salmanmkc in https://github.com/actions/runner/pull/4335
* Add WS bridge over DAP TCP server by @rentziass in https://github.com/actions/runner/pull/4328
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4355
* Bump Docker version to 29.4.0 by @Copilot in https://github.com/actions/runner/pull/4352
* Update dotnet sdk to latest version @8.0.420 by @github-actions[bot] in https://github.com/actions/runner/pull/4356
* Bump @typescript-eslint/parser from 8.58.1 to 8.59.0 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4360
* Bump System.Formats.Asn1 and System.Security.Cryptography.Pkcs by @dependabot[bot] in https://github.com/actions/runner/pull/4362
* Add vulnerability-alerts permission by @salmanmkc in https://github.com/actions/runner/pull/4350
* Bump @typescript-eslint/eslint-plugin from 8.58.1 to 8.59.0 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4359
* Bump System.ServiceProcess.ServiceController from 10.0.3 to 10.0.6 by @dependabot[bot] in https://github.com/actions/runner/pull/4358
* Bump typescript from 6.0.2 to 6.0.3 in /src/Misc/expressionFunc/hashFiles by @dependabot[bot] in https://github.com/actions/runner/pull/4353
* Bump Microsoft.DevTunnels.Connections from 1.3.16 to 1.3.39 by @dependabot[bot] in https://github.com/actions/runner/pull/4339

## New Contributors
* @stefanpenner made their first contribution in https://github.com/actions/runner/pull/4296

**Full Changelog**: https://github.com/actions/runner/compare/v2.333.1...v2.334.0

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
