## What's Changed
* Canceled background steps should not impact job result by @lokesh755 in https://github.com/actions/runner/pull/4482
* Report actions archive size in telemetry. by @TingluoHuang in https://github.com/actions/runner/pull/4509
* Bump actions/checkout from 6 to 7 by @dependabot[bot] in https://github.com/actions/runner/pull/4511
* Update Docker to v29.6.0 and Buildx to v0.35.0 by @github-actions[bot] in https://github.com/actions/runner/pull/4516
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4519
* chore: update Node versions by @github-actions[bot] in https://github.com/actions/runner/pull/4530
* feat: enhance telemetry for action download resolution and failures by @TingluoHuang in https://github.com/actions/runner/pull/4536
* Update Docker version to 29.6.1 by @AllanGuigou in https://github.com/actions/runner/pull/4539
* feat: add self-repository action reference syntax by @nodeselector in https://github.com/actions/runner/pull/4457
* Update dotnet sdk to latest version @8.0.422 by @github-actions[bot] in https://github.com/actions/runner/pull/4504
* Link config.sh and installdependencies.sh in docs by @Wuodan in https://github.com/actions/runner/pull/4526
* Add support for $GITHUB_ARTIFACTS environment files by @bdehamer in https://github.com/actions/runner/pull/4527
* feat: expose effective cache-mode to steps via ACTIONS_CACHE_MODE by @philip-gai in https://github.com/actions/runner/pull/4538
* Setup Job: announce when running with locked dependencies by @nodeselector in https://github.com/actions/runner/pull/4546
* Setup Job: reword locked-dependencies log line to use lockfile language by @nodeselector in https://github.com/actions/runner/pull/4550
* Wait for worker to finish during cancel by @TingluoHuang in https://github.com/actions/runner/pull/4553
* do not cap migrated setting retry is exception is session conflict by @aiqiaoy in https://github.com/actions/runner/pull/4557
* Allow checking DNS with api.gihub.com. by @TingluoHuang in https://github.com/actions/runner/pull/4547
* Exit ephemeral runners on broker acknowledge job-not-found by @rentziass in https://github.com/actions/runner/pull/4540
* Cleanup session files on get message or session deleted error by @nikola-jokic in https://github.com/actions/runner/pull/4551
* Recreate session on RunnerSessionInvalid from broker by @luketomlinson in https://github.com/actions/runner/pull/4556

## New Contributors
* @Wuodan made their first contribution in https://github.com/actions/runner/pull/4526
* @bdehamer made their first contribution in https://github.com/actions/runner/pull/4527
* @philip-gai made their first contribution in https://github.com/actions/runner/pull/4538

**Full Changelog**: https://github.com/actions/runner/compare/v2.335.0...v2.336.0

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
