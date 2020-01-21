## Features
  - Remove runner flow: Change from PAT to "deletion token" in prompt (#225) 
  - Expose github.run_id and github.run_number to action runtime env. (#224)

## Bugs
  - Clean up error messages for container scenarios (#221)
  - Pick shell from prependpath (#231)

## Misc
  - Runner code cleanup  (#218 #227, #228, #229, #230) 
  - Consume dotnet core 3.1 in runner. (#213)

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
