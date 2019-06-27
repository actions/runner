## Features
  - N/A

## Bugs
  - Support container action's action.yml, fix server exception mapping, fix job container. #38

## Misc
  - N/A

## Agent Downloads  

|         | Package                                                                                                       |
| ------- | ----------------------------------------------------------------------------------------------------------- |
| Windows x64 | [actions-runner-win-x64-<AGENT_VERSION>.zip](https://githubassets.azureedge.net/runners/<AGENT_VERSION>/actions-runner-win-x64-<AGENT_VERSION>.zip)      |
| macOS   | [actions-runner-osx-x64-<AGENT_VERSION>.tar.gz](https://githubassets.azureedge.net/runners/<AGENT_VERSION>/actions-runner-osx-x64-<AGENT_VERSION>.tar.gz)   |
| Linux x64  | [actions-runner-linux-x64-<AGENT_VERSION>.tar.gz](https://githubassets.azureedge.net/runners/<AGENT_VERSION>/actions-runner-linux-x64-<AGENT_VERSION>.tar.gz) |

After Download:  

## Windows x64

``` bash
C:\> mkdir myagent && cd myagent
C:\myagent> Add-Type -AssemblyName System.IO.Compression.FileSystem ; [System.IO.Compression.ZipFile]::ExtractToDirectory("$HOME\Downloads\actions-runner-win-x64-<AGENT_VERSION>.zip", "$PWD")
```

## OSX

``` bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/actions-runner-osx-x64-<AGENT_VERSION>.tar.gz
```

## Linux x64

``` bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/actions-runner-linux-x64-<AGENT_VERSION>.tar.gz
```
