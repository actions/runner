## Fixes
- Don't share the templatecontext to avoid accumulated evaluation memory limits across jobs
- `--version` switch now working again with an `.actrc`
- Errors in `.actrc` are no longer fatal errors
- Trim white spaces in `.actrc` files
- `--list` flag works again, was broken between `v3.5.0` and `v3.6.4`
- Force cancellation no longer waits for any running agent, can be used to resync a stale runner
- Send cancellation Message not within 5s after sending the job message, **to mitigate a runner bug**

## Features
- Specify deployment environment secrets and use different secrets per job
- Simple oidc stub, eventually works with cloud providers or not
- Update actions/runner 2.289.1
- Accept live logs via websockets, Protocol addition of github actions March 2022
- Allow to rerun from HEAD commit of the branch or tag, faster testing of release and issue workflows 
- Option to run a command when a new job gets queued, e.g. for basic upscaling
- Better error messages of cyclic and missing dependencies 

## Breaking Changes
- Specifing an job ( deployment ) environment no longer uses secrets of `-s` or `--secret` flag
- Yaml anchors are now disabled again
- The `GITHUB_TOKEN` in appsettings.json is no longer sent to jobs with contents: read / none permissions
  Added `GITHUB_TOKEN_NONE` and `GITHUB_TOKEN_READ_ONLY` properties to be able to set a value
- The `gharun` / `Runner.Client` `-C` flag no longer uses `.github/workflows` as default argument, instead it uses a default path relative to the `-C` flag
  workflow filenames are now resolved relative to the `-C` flag before sending the name to the server

## Windows x64
We recommend configuring the runner in a root folder of the Windows drive (e.g. "C:\actions-runner"). This will help avoid issues related to service identity folder permissions and long file path restrictions on Windows.

The following snipped needs to be run on `powershell`:
``` powershell
# Create a folder under the drive root
mkdir \actions-runner ; cd \actions-runner
# Download the latest runner package
Invoke-WebRequest -Uri https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-win-x64-<RUNNER_VERSION>.zip -OutFile actions-runner-win-x64-<RUNNER_VERSION>.zip
# Extract the installer
Add-Type -AssemblyName System.IO.Compression.FileSystem ;
[System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD\actions-runner-win-x64-<RUNNER_VERSION>.zip", "$PWD")
```

## OSX

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
```

## Linux x64

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
```

## Linux arm64

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
```

## Linux arm

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz
```

## Using your self hosted runner
For additional details about configuring, running, or shutting down the runner please check out our [product docs.](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/adding-self-hosted-runners)

## SHA-256 Checksums

The SHA-256 checksums for the packages included in this build are shown below:

- actions-runner-win-x64-<RUNNER_VERSION>.zip <!-- BEGIN SHA win-x64 --><WIN_X64_SHA><!-- END SHA win-x64 -->
- actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA osx-x64 --><OSX_X64_SHA><!-- END SHA osx-x64 -->
- actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-x64 --><LINUX_X64_SHA><!-- END SHA linux-x64 -->
- actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-arm64 --><LINUX_ARM64_SHA><!-- END SHA linux-arm64 -->
- actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-arm --><LINUX_ARM_SHA><!-- END SHA linux-arm -->

- actions-runner-win-x64-<RUNNER_VERSION>-noexternals.zip <!-- BEGIN SHA win-x64_noexternals --><WIN_X64_SHA_NOEXTERNALS><!-- END SHA win-x64_noexternals -->
- actions-runner-osx-x64-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA osx-x64_noexternals --><OSX_X64_SHA_NOEXTERNALS><!-- END SHA osx-x64_noexternals -->
- actions-runner-linux-x64-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA linux-x64_noexternals --><LINUX_X64_SHA_NOEXTERNALS><!-- END SHA linux-x64_noexternals -->
- actions-runner-linux-arm64-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA linux-arm64_noexternals --><LINUX_ARM64_SHA_NOEXTERNALS><!-- END SHA linux-arm64_noexternals -->
- actions-runner-linux-arm-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA linux-arm_noexternals --><LINUX_ARM_SHA_NOEXTERNALS><!-- END SHA linux-arm_noexternals -->

- actions-runner-win-x64-<RUNNER_VERSION>-noruntime.zip <!-- BEGIN SHA win-x64_noruntime --><WIN_X64_SHA_NORUNTIME><!-- END SHA win-x64_noruntime -->
- actions-runner-osx-x64-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA osx-x64_noruntime --><OSX_X64_SHA_NORUNTIME><!-- END SHA osx-x64_noruntime -->
- actions-runner-linux-x64-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA linux-x64_noruntime --><LINUX_X64_SHA_NORUNTIME><!-- END SHA linux-x64_noruntime -->
- actions-runner-linux-arm64-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA linux-arm64_noruntime --><LINUX_ARM64_SHA_NORUNTIME><!-- END SHA linux-arm64_noruntime -->
- actions-runner-linux-arm-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA linux-arm_noruntime --><LINUX_ARM_SHA_NORUNTIME><!-- END SHA linux-arm_noruntime -->

- actions-runner-win-x64-<RUNNER_VERSION>-noruntime-noexternals.zip <!-- BEGIN SHA win-x64_noruntime_noexternals --><WIN_X64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA win-x64_noruntime_noexternals -->
- actions-runner-osx-x64-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA osx-x64_noruntime_noexternals --><OSX_X64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA osx-x64_noruntime_noexternals -->
- actions-runner-linux-x64-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA linux-x64_noruntime_noexternals --><LINUX_X64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA linux-x64_noruntime_noexternals -->
- actions-runner-linux-arm64-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA linux-arm64_noruntime_noexternals --><LINUX_ARM64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA linux-arm64_noruntime_noexternals -->
- actions-runner-linux-arm-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA linux-arm_noruntime_noexternals --><LINUX_ARM_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA linux-arm_noruntime_noexternals -->
