## What's Changed
* Prepare runner release 2.309.0 by @johnsudol in https://github.com/actions/runner/pull/2833
* remove debug-only flag from stale bot action by @ruvceskistefan in https://github.com/actions/runner/pull/2834
* Calculate docker instance label based on the hash of the config by @nikola-jokic in https://github.com/actions/runner/pull/2683
* Correcting `zen` address by @Pantelis-Santorinios in https://github.com/actions/runner/pull/2855
* Update dotnet sdk to latest version @6.0.414 by @github-actions in https://github.com/actions/runner/pull/2852
* Bump @typescript-eslint/parser from 6.4.1 to 6.7.0 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2845
* Bump @types/node from 20.5.6 to 20.6.2 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2854
* Bump eslint-plugin-github from 4.9.2 to 4.10.0 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2808
* Bump @typescript-eslint/parser from 6.7.0 to 6.7.2 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2858
* Bump prettier from 3.0.2 to 3.0.3 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2860
* Bump @vercel/ncc from 0.36.1 to 0.38.0 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2859
* Bump @typescript-eslint/eslint-plugin from 6.4.1 to 6.7.2 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2861
* Remove unused code in AgentManager. by @TingluoHuang in https://github.com/actions/runner/pull/2866
* GetAgents from all runner groups durning config. by @TingluoHuang in https://github.com/actions/runner/pull/2865
* Change alpine from vst blobs to OSS gha alpine build by @vanZeben in https://github.com/actions/runner/pull/2871
* Bump node 16 to v16.20.2 by @vanZeben in https://github.com/actions/runner/pull/2872
* Bump directly dotnet vulnerable packages by @nikola-jokic in https://github.com/actions/runner/pull/2870
* Fix ArgumentOutOfRangeException in PowerShellPostAmpersandEscape. by @TingluoHuang in https://github.com/actions/runner/pull/2875
* bump container hook version in runner image by @nikola-jokic in https://github.com/actions/runner/pull/2881
* Use `Directory.EnumerateFiles` instead of `Directory.GetFiles` in WhichUtil. by @TingluoHuang in https://github.com/actions/runner/pull/2882
* Add warning about node16 deprecation by @takost in https://github.com/actions/runner/pull/2887
* Throw TimeoutException instead of OperationCanceledException on the final retry in DownloadRepositoryAction by @TingluoHuang in https://github.com/actions/runner/pull/2895
* Update message when runners are deleted by @thboop in https://github.com/actions/runner/pull/2896
* Do not give up if Results is powering logs by @yacaovsnc in https://github.com/actions/runner/pull/2893
* Allow use action archive cache to speed up workflow jobs. by @TingluoHuang in https://github.com/actions/runner/pull/2857
* Upgrade docker engine to 24.0.6 in the runner container image by @Link- in https://github.com/actions/runner/pull/2886
* Collect telemetry to measure upload speed for different backend. by @TingluoHuang in https://github.com/actions/runner/pull/2912
* Use RawHttpMessageHandler and VssHttpRetryMessageHandler in ResultsHttpClient by @yacaovsnc in https://github.com/actions/runner/pull/2908
* Retries to lock Services database on Windows by @sugymt in https://github.com/actions/runner/pull/2880
* Update default version to node20 by @takost in https://github.com/actions/runner/pull/2844
* Fixed Attempt typo by @corycalahan in https://github.com/actions/runner/pull/2849
* Fix typo by @rajbos in https://github.com/actions/runner/pull/2670

## New Contributors
* @Pantelis-Santorinios made their first contribution in https://github.com/actions/runner/pull/2855
* @github-actions made their first contribution in https://github.com/actions/runner/pull/2852
* @sugymt made their first contribution in https://github.com/actions/runner/pull/2880
* @corycalahan made their first contribution in https://github.com/actions/runner/pull/2849

**Full Changelog**: https://github.com/actions/runner/compare/v2.309.0...v2.310.0

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

- actions-runner-win-x64-<RUNNER_VERSION>-noexternals.zip <!-- BEGIN SHA win-x64_noexternals --><WIN_X64_SHA_NOEXTERNALS><!-- END SHA win-x64_noexternals -->
- actions-runner-win-arm64-<RUNNER_VERSION>-noexternals.zip <!-- BEGIN SHA win-arm64_noexternals --><WIN_ARM64_SHA_NOEXTERNALS><!-- END SHA win-arm64_noexternals -->
- actions-runner-osx-x64-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA osx-x64_noexternals --><OSX_X64_SHA_NOEXTERNALS><!-- END SHA osx-x64_noexternals -->
- actions-runner-osx-arm64-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA osx-arm64_noexternals --><OSX_ARM64_SHA_NOEXTERNALS><!-- END SHA osx-arm64_noexternals -->
- actions-runner-linux-x64-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA linux-x64_noexternals --><LINUX_X64_SHA_NOEXTERNALS><!-- END SHA linux-x64_noexternals -->
- actions-runner-linux-arm64-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA linux-arm64_noexternals --><LINUX_ARM64_SHA_NOEXTERNALS><!-- END SHA linux-arm64_noexternals -->
- actions-runner-linux-arm-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA linux-arm_noexternals --><LINUX_ARM_SHA_NOEXTERNALS><!-- END SHA linux-arm_noexternals -->

- actions-runner-win-x64-<RUNNER_VERSION>-noruntime.zip <!-- BEGIN SHA win-x64_noruntime --><WIN_X64_SHA_NORUNTIME><!-- END SHA win-x64_noruntime -->
- actions-runner-win-arm64-<RUNNER_VERSION>-noruntime.zip <!-- BEGIN SHA win-arm64_noruntime --><WIN_ARM64_SHA_NORUNTIME><!-- END SHA win-arm64_noruntime -->
- actions-runner-osx-x64-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA osx-x64_noruntime --><OSX_X64_SHA_NORUNTIME><!-- END SHA osx-x64_noruntime -->
- actions-runner-osx-arm64-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA osx-arm64_noruntime --><OSX_ARM64_SHA_NORUNTIME><!-- END SHA osx-arm64_noruntime -->
- actions-runner-linux-x64-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA linux-x64_noruntime --><LINUX_X64_SHA_NORUNTIME><!-- END SHA linux-x64_noruntime -->
- actions-runner-linux-arm64-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA linux-arm64_noruntime --><LINUX_ARM64_SHA_NORUNTIME><!-- END SHA linux-arm64_noruntime -->
- actions-runner-linux-arm-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA linux-arm_noruntime --><LINUX_ARM_SHA_NORUNTIME><!-- END SHA linux-arm_noruntime -->

- actions-runner-win-x64-<RUNNER_VERSION>-noruntime-noexternals.zip <!-- BEGIN SHA win-x64_noruntime_noexternals --><WIN_X64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA win-x64_noruntime_noexternals -->
- actions-runner-win-arm64-<RUNNER_VERSION>-noruntime-noexternals.zip <!-- BEGIN SHA win-arm64_noruntime_noexternals --><WIN_ARM64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA win-arm64_noruntime_noexternals -->
- actions-runner-osx-x64-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA osx-x64_noruntime_noexternals --><OSX_X64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA osx-x64_noruntime_noexternals -->
- actions-runner-osx-arm64-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA osx-arm64_noruntime_noexternals --><OSX_ARM64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA osx-arm64_noruntime_noexternals -->
- actions-runner-linux-x64-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA linux-x64_noruntime_noexternals --><LINUX_X64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA linux-x64_noruntime_noexternals -->
- actions-runner-linux-arm64-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA linux-arm64_noruntime_noexternals --><LINUX_ARM64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA linux-arm64_noruntime_noexternals -->
- actions-runner-linux-arm-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA linux-arm_noruntime_noexternals --><LINUX_ARM_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA linux-arm_noruntime_noexternals -->
