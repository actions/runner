## Features
  - Add packages for Windows x86 (win-x6), Linux ARM32 (linux-arm), Linux ARM64 (linux-arm64)

## Bugs
  - N/A

## Misc
  - N/A

## Agent Downloads  

|         | Package                                                                                                       |
| ------- | ----------------------------------------------------------------------------------------------------------- |
| Windows x64 | [actions-runner-win-x64-<RUNNER_VERSION>.zip](https://githubassets.azureedge.net/runners/<RUNNER_VERSION>/actions-runner-win-x64-<RUNNER_VERSION>.zip)      |
| macOS   | [actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz](https://githubassets.azureedge.net/runners/<RUNNER_VERSION>/actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz)   |
| Linux x64  | [actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz](https://githubassets.azureedge.net/runners/<RUNNER_VERSION>/actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz) |
| Linux arm64  | [actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz](https://githubassets.azureedge.net/runners/<RUNNER_VERSION>/actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz) |
| Linux arm  | [actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz](https://githubassets.azureedge.net/runners/<RUNNER_VERSION>/actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz) |

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

## Linux arm64

``` bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
```

## Linux arm

``` bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz
```