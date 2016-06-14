# Replacing an agent

If an agent already exists, configuration will ask you if you want to replace it.  The name will default to the machine name so if configure two agents on the same machine, you can enter N and it will give you a chance to provide another name.

If you are reconfiguring the agent, then choose Y.

If you intended to actually replace an different agent, ensure the other agent is unconfigured.  If two instances run with the same agent name, one will get a conflict.  After a few minutes of conflicts, one will shut down.

```bash
Enter agent name (press enter for mymachine) > 
Scanning for tool capabilities.
Connecting to the server.
Enter replace? (Y/N) (press enter for N) > N
Enter agent name (press enter for mymachine) > testagent
Scanning for tool capabilities.
Connecting to the server.
Successfully added the agent
```

# Unconfigure

> Important: If you're running as a service on Linux/OSX, ensure you `stop` then `uninstall` the service before unconfiguring.  See [Nix Service Config](nixsvc.md)

```bash
$ ./config.sh remove
Removing service
Does not exist. Skipping Removing service
Removing agent from the server
Enter authentication type (press enter for PAT) >
Enter personal access token > ****************************************************
Succeeded: Removing agent from the server
Removing .Credentials
Succeeded: Removing .Credentials
Removing .Agent
Succeeded: Removing .Agent
```

# Help

```bash
./config.sh --help
```