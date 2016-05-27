# Configure Agent for On-Premises TFS

## Support

This agent is supported for:
![osx](../res/apple_sm.png) ![linux](../res/linux_sm.png) OSX/Linux: TFS2015 and beyond.
![win](../res/win_sm.png) Windows: TFS.vNext

If you want to run windows agent for TFS2015, use the agent that you download from that server.  This agent will ship with TFS.vNext major release.

Key Points:  
  - Use server URL (http://mytfsserver:8080/tfs)
  - Windows will default to Integrated.  You will not have to enter credentials
  - Linux will default to Negotiate for an on-premises URL.  Prefer a local account created on the server 
  - Add the account to proper roles.  [Details](roles.md)

## Configure Windows

```bash
C:\myagent\config.cmd

>> Connect:

Enter server URL > http://myserver:8080/tfs
Enter authentication type (press enter for Integrated) > 
Connecting to server ...

>> Register Agent:

Enter agent pool (press enter for default) > 
Enter agent name (press enter for mymachine) > myAgentName
Scanning for tool capabilities.
Connecting to the server.
Successfully added the agent
Enter work folder (press enter for _work) >
2016-05-27 11:03:33Z: Settings Saved.
Enter run agent as service? (Y/N) (press enter for N) >
```

## Configure OSX/Linux

```bash
$ ./config.sh

>> Connect:

Enter server URL > http://myserver:8080/tfs
Enter authentication type (press enter for Negotiate) > 
Enter user name > myserver\someuser
Enter password > ********
Connecting to server ...
Saving credentials...

>> Register Agent:

Enter agent pool (press enter for default) > 
Enter agent name (press enter for mymachine) > myAgentName
Scanning for tool capabilities.
Connecting to the server.
Successfully added the agent
Enter work folder (press enter for _work) >
2016-05-27 11:03:33Z: Settings Saved.
Enter run agent as service? (Y/N) (press enter for N) >
```

## Run the agent interactively

**If you did not run as a service**, you can start it interactively by running

![win](../res/win_sm.png) Windows: run.cmd  

![osx](../res/apple_sm.png) ![linux](../res/linux_sm.png) OSX/Linux: ./run.sh

```bash
$ ./run.sh 
Scanning for tool capabilities.
Connecting to the server.
2016-05-27 11:07:41Z: Listening for Jobs
```

## Replace

If you are asked whether to replace an agent, [read details here](moreconfig.md)

[Details here](moreconfig.md)

## Reconfigure or Unconfigure

[Details here](moreconfig.md)


