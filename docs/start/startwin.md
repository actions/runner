# ![win](../res/win_med.png) Windows Agent

## Step 1: System Requirements

The minimum supported version is Windows 7.

Even though the agent has no pre-requisites, many of the tasks we run require Visual Studio.

## Step 2: Download from Releases

Download the agent from [github releases](https://github.com/Microsoft/vsts-agent/releases/tag/v2.103.1)

## Step 3: Create the agent

Create a directory for the agent and unzip.  Use explorer or from cmd:  
```bash
c:\ mkdir myagent && cd myagent
C:\myagent> Add-Type -AssemblyName System.IO.Compression.FileSystem ; [System.IO.Compression.ZipFile]::ExtractToDirectory("$HOME\Downloads\vsts-agent-win7-x64-2.103.1.zip", "$PWD")
```
## Step 4: Configure

```bash
c:\myagent\config.cmd
```

[Config VSTS Details](configvsts.md)  

[Config On-Prem Details](configonprem.md)

## Step 5: Optionally run the agent interactively

If you didn't run as a service above:

```bash
c:\myagent\run.cmd
```

**That's It!**  
