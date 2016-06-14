# Troubleshooting

The agent sends logs to the server but some failures such as configuration, networking or permissions prevent that.  It requires investigating within the agent.

Often these logs are most relevant to the product but they can sometimes provide hints to a user as what could be wrong.

## System.Debug

If you are having issues with a build, the first step is to set System.Debug to true on the build definitions variables tab.  The agent and tasks will emit [debug]xxx lines for more detailed insight into what the specific task is doing.

## Agent Trace Logs

Logs are in the _diag folder.

The agent has two parts.  The agent which listens to the build queue.  When it gets a build message, it creates a worker process to run that build.  

For example:
```bash
$ ls -la _diag/
-rwxr--r--   1 bryanmac  staff   23126 Jun 11 06:43 Agent_20160611-104223-utc.log
-rwxr--r--   1 bryanmac  staff   26046 Jun 11 08:39 Agent_20160611-123755-utc.log
-rwxr--r--   1 bryanmac  staff  240035 Jun 11 08:38 Worker_20160611-123825-utc.log
-rwxr--r--   1 bryanmac  staff  220196 Jun 11 08:38 Worker_20160611-123843-utc.log
-rwxr--r--   1 bryanmac  staff  220012 Jun 11 08:39 Worker_20160611-123858-utc.log
```

If the agent isn't picking up builds, the agent logs are likely the most relevant.  If a build starts running and you want to get details of that build, the specific worker log is relevant.

Secrets are masked out of the logs.

## Http Tracing Windows

Start [Fiddler](http://www.telerik.com/fiddler).  
It's recommended to only listen to agent traffic.  File, Capture Traffic off (F12) 

Let the agent know to use the proxy:

```bash
set VSTS_HTTP_PROXY=https://127.0.0.1:8888
```

Run the agent interactively.  If you're running as a service, you can set as the environment variable in control panel for the account the service is running as.

Restart the agent.

TODO: video

## Http Tracing OSX / Linux

It's easy to capture the http trace of the agent using Charles Proxy (similar to Fiddler on windows).  

TODO: video

Start Charles Proxy
Charles: Proxy > Proxy Settings > SSL Tab.  Enable.  Add URL
Charles: Proxy > Mac OSX Proxy.  Recommend disabling to only see agent traffic.

```bash
export VSTS_HTTP_PROXY=https://127.0.0.1:8888
```

Run the agent interactively.  If it's running as a service, you can set in the .env file.  See [nix service](start/nixsvc.md)

Restart the agent.

## Security Notice

HTTP traces and trace files can contain credentials.  

1. Do not POST them on a publically accessible site.
2. If you send them to the product team, they will be treated securely and discarded after the investigation.
