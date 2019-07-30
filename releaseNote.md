## Features
  - Template evaluation in action.yml (#56)
  - Add inputs for container action, more cleanup, more l0 tests. (#58)
  - Allow expression for step continue-on-error and step timeout (#61)

## Bugs
  - Fix working folder for hosted linux/mac pool (#63) 
  - Error when using container action on linux (#65)
  - Action log clean up. (#60)
  - Disable Debug by default, add ACTIONS_STEP_DEBUG to enable (#64)

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
