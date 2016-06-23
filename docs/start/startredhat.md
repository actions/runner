# ![redhat](../res/redhat_med.png) Red Hat Agent

## Step 1: System Requirements

[Read here](../preview/latebreaking.md) to ensure system packages are installed

## Step 2: Download from Releases

Download the agent from [github releases](https://github.com/Microsoft/vsts-agent/releases/tag/v2.102.1)

## Step 3: Create the agent

```bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/vsts-agent-rhel.7.2-x64-2.102.1.tar.gz
```
## Step 4: Configure

```bash
~/myagent$ ./config.sh

```

[Config VSTS Details](configvsts.md)  

[Config On-Prem Details](configonprem.md)

## Step 5: Run the agent

You can run the agent interactively or as a SystemD service.

### Interactively

```bash
~/myagent$ ./run.sh
```

### As a SystemD Service

[details here](svcsystemd.md)
