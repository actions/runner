## What's Changed
* Bump @types/node from 12.12.14 to 20.4.10 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2759
* Trace x-github-request-id when download action tarball. by @TingluoHuang in https://github.com/actions/runner/pull/2755
* Fix typo by @kyanny in https://github.com/actions/runner/pull/2741
* Bump prettier from 3.0.1 to 3.0.2 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2772
* Bump @types/node from 20.4.10 to 20.5.0 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2773
* Revert "Fixed a bug where a misplaced `=` character could bypass here… by @cory-miller in https://github.com/actions/runner/pull/2774
* Filter NODE_OPTIONS from env for file output by @cory-miller in https://github.com/actions/runner/pull/2775
* Bump @types/node from 20.5.0 to 20.5.1 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2781
* Update Docker Version in Images by @ajschmidt8 in https://github.com/actions/runner/pull/2694
* Bump @types/node from 20.5.1 to 20.5.4 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2789
* Bump @typescript-eslint/parser from 6.4.0 to 6.4.1 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2785
* Bump Microsoft.AspNet.WebApi.Client from 5.2.4 to 5.2.9 in /src by @dependabot in https://github.com/actions/runner/pull/2751
* Bump System.Buffers from 4.3.0 to 4.5.1 in /src by @dependabot in https://github.com/actions/runner/pull/2749
* Bump dotnet/runtime-deps from 6.0-jammy to 7.0-jammy in /images by @dependabot in https://github.com/actions/runner/pull/2745
* Remove need to manually compile JS binary for hashFiles utility by @vanZeben in https://github.com/actions/runner/pull/2770
* Revert "Bump dotnet/runtime-deps from 6.0-jammy to 7.0-jammy in /images" by @TingluoHuang in https://github.com/actions/runner/pull/2790
* Query runner by name on server side. by @TingluoHuang in https://github.com/actions/runner/pull/2771
* Bump typescript from 5.1.6 to 5.2.2 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2795
* Bump @types/node from 20.5.4 to 20.5.6 in /src/Misc/expressionFunc/hashFiles by @dependabot in https://github.com/actions/runner/pull/2796
* Bump Newtonsoft.Json from 13.0.1 to 13.0.3 in /src by @dependabot in https://github.com/actions/runner/pull/2797
* Support replacing runners in v2 flow by @luketomlinson in https://github.com/actions/runner/pull/2791
* Delegating handler for Http redirects by @paveliak in https://github.com/actions/runner/pull/2814
* Add references to the firewall requirements docs by @paveliak in https://github.com/actions/runner/pull/2815
* Create automated workflow that will auto-generate dotnet sdk patches by @vanZeben in https://github.com/actions/runner/pull/2776
* Fixes minor issues with using proper output varaibles by @vanZeben in https://github.com/actions/runner/pull/2818
* Throw NonRetryableException on GetNextMessage from broker as needed. by @TingluoHuang in https://github.com/actions/runner/pull/2828
* Mark action download failures as infra failures by @cory-miller in https://github.com/actions/runner/pull/2827

## New Contributors
* @kyanny made their first contribution in https://github.com/actions/runner/pull/2741
* @ajschmidt8 made their first contribution in https://github.com/actions/runner/pull/2694

**Full Changelog**: https://github.com/actions/runner/compare/v2.308.0...v2.309.0

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
