## Features
  - Expose whether debug is on/off via RUNNER_DEBUG. (#253)
  - Upload log on runner when worker get killed due to cancellation timeout. (#255)
  - Update config.sh/cmd --help documentation (#282) 
  - Set http_proxy and related env vars for job/service containers (#304)
  - Set both http_proxy and HTTP_PROXY env for runner/worker processes. (#298)

## Bugs
  - Verify runner Windows service hash started successfully after configuration (#236)
  - Detect source file path in L0 without using env. (#257)
  - Handle escaped '%' in commands data section (#200)
  - Allow container to be null/empty during matrix expansion (#266)
  - Translate problem matcher file to host path (#272)
  - Change hashFiles() expression function to use @actions/glob. (#268)
  - Default post-job action's condition to always(). (#293)
  - Support action.yaml file as action's entry file (#288) 
  - Trace javascript action exit code to debug instead of user logs (#290)
  - Change prompt message when removing a runner to lines up with GitHub.com UI (#303) 
  - Include step.env as part of env context. (#300)
  - Update Base64 Encoders to deal with suffixes (#284)

## Misc
  - Move .sln file under ./src (#238)
  - Treat warnings as errors during compile (#249) 

## Windows x64
We recommend configuring the runner under "<DRIVE>:\actions-runner". This will help avoid issues related to service identity folder permissions and long file path restrictions on Windows
``` 
// Create a folder under the drive root
mkdir \actions-runner ; cd \actions-runner
// Download the latest runner package
Invoke-WebRequest -Uri https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-win-x64-<RUNNER_VERSION>.zip -OutFile actions-runner-win-x64-<RUNNER_VERSION>.zip
// Extract the installer
Add-Type -AssemblyName System.IO.Compression.FileSystem ; 
[System.IO.Compression.ZipFile]::ExtractToDirectory("$HOME\Downloads\actions-runner-win-x64-<RUNNER_VERSION>.zip", "$PWD")
```

## OSX

``` bash
// Create a folder
mkdir actions-runner && cd actions-runner
// Download the latest runner package
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
// Extract the installer
tar xzf ./actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
```

## Linux x64

``` bash
// Create a folder
mkdir actions-runner && cd actions-runner
// Download the latest runner package
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
// Extract the installer
tar xzf ./actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
```

## Linux arm64 (Pre-release)

``` bash
// Create a folder
mkdir actions-runner && cd actions-runner
// Download the latest runner package
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
// Extract the installer
tar xzf ./actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
```

## Linux arm (Pre-release)

``` bash
// Create a folder
mkdir actions-runner && cd actions-runner
// Download the latest runner package
curl -O -L https://github.com/actions/runner/releases/download/v<RUNNER_VERSION>/actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz
// Extract the installer
tar xzf ./actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz
```

## Using your self hosted runner
For additional details about configuring, running, or shutting down the runner please check out our [product docs.](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/adding-self-hosted-runners)
