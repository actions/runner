# Linux Agent

## System Information

[Read here](docs/preview/latebreaking.md) to ensure system packages are installed

## Step 1: Download from Releases

Download the appropriate agent from [github releases](https://github.com/Microsoft/vsts-agent/releases)

From the cmdline:
```bash
~/Downloads$ curl -kSLO https://github.com/Microsoft/vsts-agent/releases/download/v2.99.0/vsts-agent-linux-2.99.0-0428.tar.gz
```

## Step 2: Create the agent

```bash
~/$ mkdir myagent && cd myagent
~/myagent$ tar zxvf ~/Downloads/vsts-agent-linux-2.99.0-0428.tar.gz
```

## Step 3: Run the agent

```bash
~/myagent$ ./run.sh
```

**That's It! Your agent is running interactively and ready for builds**  
