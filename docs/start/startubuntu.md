# ![ubuntu](../res/ubuntu_med.png) Ubuntu Agent

## Step 1: System Requirements

> NOTE: Prefer Ubuntu 16.04 LTS.  We configure as a service using systemd and it's not in 14.04.  [More here on configuring as a service](nixsvc.md)

[Read here](envubuntu.md) to ensure system packages are installed

## Step 2: Download from Releases

Download the agent from [github releases](https://github.com/Microsoft/vsts-agent/releases/tag/v2.103.1)

## Step 3: Create the agent

**16.04**

```bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/vsts-agent-ubuntu.16.04-x64-2.103.1.tar.gz
```

**14.04**

```bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/vsts-agent-ubuntu.14.04-x64-2.103.1.tar.gz
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
