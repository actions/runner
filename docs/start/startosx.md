# ![osx](../res/apple_med.png) OSX Agent

## Step 1: System Requirements

[Read here](../preview/latebreaking.md) to ensure system packages are installed

## Step 2: Download from Releases

Download the agent from [github releases](https://github.com/Microsoft/vsts-agent/releases/tag/v2.102.0)

## Step 3: Create the agent

```bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/vsts-agent-osx.10.11-x64-2.102.0.tar.gz
```
## Step 4: Configure

```bash
~/myagent$ ./config.sh

```

[Config VSTS Details](configvsts.md)  

[Config On-Prem Details](configonprem.md)

> NOTE: running as a service [details here](nixsvc.md)

## Step 5: Optionally run the agent interactively

```bash
~/myagent$ ./run.sh
```

**That's It!**  
