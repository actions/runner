# Configure Resource Limits for Azure Pipelines Agent

## Linux

### Memory
When the agent on a Linux system that is under high memory pressure, it is important to ensure that the agent does not get killed or become otherwise unusable. If the agent process dies or runs out of memory it cannot stream pipeline logs or report pipeline status back to the server, so it is preferable to reclaim system memory from pipeline job processes before the agent process.

#### CGroups
`cgroups` can be used to prevent job processes from consuming too many resources or to isolate resources between multiple agents. For a single agent it is useful to isolate the agent from the jobs it runs. 

It is important to use two groups, because otherwise the pipeline job processes will inherit the group from their parent, the agent, so there will be no distinction in terms of control. A second `job` cgroup allows the job processes to be managed independent of the agent, e.g. in an out-of-memory scenario (when the job exceeds the limits given by the `job` cgroup), the job will be killed instead of the agent. If a single cgroup is used, the agent may killed to reclaim memory from the cgroup. Additionally, using two groups can provide the agent "dedicated" memory to avoid instability caused by thrashing. In the following example `cgconfig.conf`, if the `azpl_job` group memory limit is 6G and the host machine has 7G of total memory, the agent will effectively have access 1G of memory at all times, assuming there are no other significant applications running on the host. Without this, under high memory load where a significant portion of memory is backed by executable code loaded from files on disk, such as when building large dotnet applications, this memory may be evicted before the OOM killer is invoked. This thrashing can degrade the performance of the Linux system and the agent so severely that the agent's connection to the server can time out, causing the pipeline to fail.

The `/etc/cgconfig.conf` [file](https://linux.die.net/man/5/cgconfig.conf) can be used to set up two cgroups that impose different
memory limits. For Microsoft Hosted Ubuntu 1604 agents, which have 7G of memory and 8G of swap, the following configuration is used:

```
group azpl_agent {
    memory {}
}
group azpl_job {
    memory {
        memory.limit_in_bytes = 6g;
        memory.memsw.limit_in_bytes = 13g;

    }
}
```

This is used in conjunction with a `/etc/cgrules.conf` [config file](https://linux.die.net/man/5/cgrules.conf). The `cgrules.conf` file controls what groups a process will run in. `Agent.Listener` and `Agent.Worker` are the two high priority agent processes, so they are run in a group that does not have a memory limit, and all other processes, notably job processes created by the agent, will run in memory limited group. The following configuration is used for Hosted Ubuntu 1604 machines:

```
vsts:Agent.Listener memory azpl_agent
vsts:Agent.Worker memory azpl_agent
vsts memory azpl_job
```

#### Understanding the Out of Memory Killer
If a Linux system runs out of memory, it invokes the [OOM killer](https://lwn.net/Articles/317814/) to reclaim memory. The OOM killer chooses a process to sacrifice based on heuristics, and adjusted by `oom_score_adj`. Higher scores are more likely to get killed, and range from -1000 to 1000. It is important that the agent process has a lower score than the job processes it manages, because if the agent is killed the job effectively dies as well.

The agent can help manage process OOM scores (via `oom_score_adj`). By default, processes that are invoked by the agent will have an `oom_score_adj` of 500, and by Linux defaults, the agent will have an OOM score, and `oom_score_adj` of 0. For machines who's sole purpose is to run an agent, it is reasonable to run the agent with a very low score, such as -999 or -1000, so that it is not killed in OOM scenarios. 

There are multiple ways to set the agent `oom_score_adj` of the agent, if necessary, but the important part for most use-cases is that the agent has a lower OOM score than the job processes. When running interactively the score can be set in the shell, and will be inherited by the agent:

```bash
$ echo $oomScoreAdj > /proc/$$/oom_score_adj
$ ./run.sh
```

If the agent is being managed by systemd, the `OOMScoreAdjust` directive can be set in the unit file:
```
$ cat /etc/systemd/system/vsts.agent.user.linux-host.service
[Unit]
Description=Azure Pipelines Agent (user.linux-host)
After=network.target

[Service]
ExecStart=/home/user/agent/runsvc.sh
User=user
WorkingDirectory=/home/user/agent
KillMode=process
KillSignal=SIGTERM
TimeoutStopSec=5min
OOMScoreAdjust=-999

[Install]
WantedBy=multi-user.target
```

In this configuration, the `Agent.Listener` and `Agent.Worker` processes will run with `oom_score_adj = -999`, and all other processes invoked by the agent will have 500, ensuring the agent is kept alive even if the job causes out-of-memory conditions.
