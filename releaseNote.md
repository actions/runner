## Features
  - Out of proc agent system plugins. #1537
  - Added support for recursively checking out sub-modules for Git and GitHub artifacts. #1544 
  - Reading owner attribute from junit xml And not using context owner as test case owner. #1587
  - Consume latest version of TEE. #1586
  - Bump git version to include security patch. #1597
  - Reading NUnit attachments in Publish Test Result Task. #1609

## Bugs
  - Also populate java in case user only has jdk installed. #1560
  - Handling duplicate attachments upload scenario while running using multi thread. #1578
  - Shallow git submodule fetch. #1594
  - Docker requirement check. #1619
  - Ignore STDERR from tf.exe when query workspaces. #1642
## Misc
  - Add assets.json to release assets. #1644

## Agent Downloads  

|         | Package                                                                                                       |
| ------- | ----------------------------------------------------------------------------------------------------------- |
| Windows | [vsts-agent-win-x64-<AGENT_VERSION>.zip](https://vstsagentpackage.azureedge.net/agent/<AGENT_VERSION>/vsts-agent-win-x64-<AGENT_VERSION>.zip)      |
| macOS   | [vsts-agent-osx-x64-<AGENT_VERSION>.tar.gz](https://vstsagentpackage.azureedge.net/agent/<AGENT_VERSION>/vsts-agent-osx-x64-<AGENT_VERSION>.tar.gz)   |
| Linux   | [vsts-agent-linux-x64-<AGENT_VERSION>.tar.gz](https://vstsagentpackage.azureedge.net/agent/<AGENT_VERSION>/vsts-agent-linux-x64-<AGENT_VERSION>.tar.gz) |

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
