## Features
  - Support image from ./path/to/Dockerfile or docker://image (#40)
  - run-action shell option (#43)
  - consume new expressions and object templating libraries (#46)
  - context cleanup, add container action prepare steps. (#48)
  - job context: ports, container, etc (#51) 
  - rename actions to steps (#53)
  - use checkout from github graph. (#55)


## Bugs
  - improve action download, add L0 tests. (#44)
  - Allow non-loop message before end (#42) 
  - removing custom localization mechanism, more string cleanup, bug fixes (#45)
  - fix job.status context. (#52)

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
