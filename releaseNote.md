## Features
  - N/A

## Bugs
  - Fixed an issue with Strong Name Validation when running as a service on Windows (#185)

## Misc
  - N/A

## Agent Downloads  

|         | Package                                                                                                       |
| ------- | ----------------------------------------------------------------------------------------------------------- |
| Windows x64 | [actions-runner-win-x64-<RUNNER_VERSION>.zip](https://githubassets.azureedge.net/runners/<RUNNER_VERSION>/actions-runner-win-x64-<RUNNER_VERSION>.zip)      |
| macOS   | [actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz](https://githubassets.azureedge.net/runners/<RUNNER_VERSION>/actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz)   |
| Linux x64  | [actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz](https://githubassets.azureedge.net/runners/<RUNNER_VERSION>/actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz) |

After Download:  

## Windows x64

``` bash
C:\> mkdir myagent && cd myagent
C:\myagent> Add-Type -AssemblyName System.IO.Compression.FileSystem ; [System.IO.Compression.ZipFile]::ExtractToDirectory("$HOME\Downloads\actions-runner-win-x64-<RUNNER_VERSION>.zip", "$PWD")
```

## OSX

``` bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
```

## Linux x64

``` bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
```
