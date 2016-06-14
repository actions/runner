# ![win](../res/win_med.png) Windows Agent

## Step 1: System Requirements

[Read here](../preview/latebreaking.md) to ensure system packages are installed

## Step 2: Download from Releases

Download the agent from [github releases](https://github.com/Microsoft/vsts-agent/releases/tag/v2.102.0)

## Step 3: Create the agent

```bash
c:\ mkdir myagent && cd myagent
C:\myagent> Add-Type -AssemblyName System.IO.Compression.FileSystem ; [System.IO.Compression.ZipFile]::ExtractToDirectory("$HOME\Downloads\vsts-agent-win7-x64-2.102.0.zip", "$PWD")
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
