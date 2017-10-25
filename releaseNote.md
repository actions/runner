## Features
 - Consume NetCore 2.0 in agent. #1219

<!--- Add when needed
## Bugs
  - Bug 1

## Misc
  - Misc 1
--->

## Agent Downloads  

|         | Package                                                                                                       |
| ------- | :-----------------------------------------------------------------------------------------------------------: |
| Windows | [Download](https://vstsagent.azureedge.net/agent/<AGENT_VERSION>/vsts-agent-win-x64-<AGENT_VERSION>.zip)      |
| macOS   | [Download](https://vstsagent.azureedge.net/agent/<AGENT_VERSION>/vsts-agent-osx-x64-<AGENT_VERSION>.tar.gz)   |
| Linux   | [Download](https://vstsagent.azureedge.net/agent/<AGENT_VERSION>/vsts-agent-linux-x64-<AGENT_VERSION>.tar.gz) |

After Download:  

## Windows

``` bash
C:\> mkdir myagent && cd myagent
C:\myagent> Add-Type -AssemblyName System.IO.Compression.FileSystem ; [System.IO.Compression.ZipFile]::ExtractToDirectory("$HOME\Downloads\vsts-agent-win-x64-<AGENT_VERSION>.zip", "$PWD")
```

## OSX

``` bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/vsts-agent-osx-x64-<AGENT_VERSION>.tar.gz
```

## Linux

``` bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/vsts-agent-linux-x64-<AGENT_VERSION>.tar.gz
```
