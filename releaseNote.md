## Features
  - New command keyword `::` with back compat for `##[` (#71)
  - Support oauthaccesstoken auth schema, add problem matcher for git checkout. (#74)

## Bugs
  - Preserve date strings in github event payload (#77)
  - Stop pull and build docker image for action multiple times (#79)

## Misc
  - Remove code for uploading artifact to artifact service (#78)

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
