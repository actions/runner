# ![redhat](../redhat_med.png) Red Hat Agent

## System Information

[Read here](../preview/latebreaking.md) to ensure system packages are installed

## Step 1: Download from Releases

Download the agent from [github releases](https://github.com/Microsoft/vsts-agent/releases/tag/v2.100.1)

## Step 2: Create the agent

```bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/vsts-agent-rhel.7.2-x64-2.100.1.tar.gz
```
## Step 3: Configure

```bash
~/myagent$ ./config.sh

```

> NOTE: running as a service [details here](nixsvc.md)

## Step 4: Optionally run the agent interactively

If you didn't run as a service above:

```bash
~/myagent$ ./run.sh
```

**That's It!**  
