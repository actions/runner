# OSX Agent

## System Information

[Read here](../preview/latebreaking.md) to ensure system packages are installed

## Step 1: Download from Releases

Download the appropriate agent from [github releases](https://github.com/Microsoft/vsts-agent/releases)

From the cmdline:
```bash
~/Downloads$ curl -kSLO https://github.com/Microsoft/vsts-agent/releases/download/v2.100.1/vsts-agent-ubuntu.14.04-x64-2.100.1.tar.gz
```

## Step 2: Create the agent

```bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar xzf ~/Downloads/vsts-agent-ubuntu.14.04-x64-2.100.1.tar.gz
```
## Step 3: Configure

```bash
~/myagent$ ./config.sh

```

## Step 4: Optionally run the agent interactively

If you didn't run as a service above:

```bash
~/myagent$ ./run.sh
```

**That's It!**  
