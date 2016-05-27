# Configure Agent for VSTS Service

Key Points:  
  - Use https account URL (https://contoso.visualstudio.com)  
  - Should use default of PAT Authentication for VSTS.  [Details](roles.md)
  - Copy and Paste your PAT into the terminal.

```bash
>> Connect:

Enter server URL > https://contoso.visualstudio.com
Enter authentication type (press enter for PAT) >
Enter personal access token > ****************************************************
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


