<!-- # YAML getting started - Run local (feature is in progress, removing docs for now since it is in flux)

The agent supports locally testing YAML configuration without queuing a build against the server.

When running in local mode, the YAML file(s) will be converted into a pipeline, and worker processes
invoked for each job. The agent will actually run the job but all agent calls to the VSTS server are
stubbed (and the server URL is hardcoded to 127.0.0.1).

A variable `agent.runMode`=`Local` is added to each job that is run. The variable can be leveraged
within your task conditions to optionally execute the step.

To run the agent in local mode, first change-directory to the root folder of your git repo. If you have
a single .yml file in the root of your repo, then you can simply run:

```
# Windows:
<PATH_TO_AGENT>\run.cmd localRun

# macOS and Linux:
<PATH_TO_AGENT>/run.sh localRun
```

Otherwise, you can use the `--yml` parameter to specify the location of your .yml file.

## What-if mode

A \"what-if\" mode is supported for debugging the .yml deserialization process. What-if mode loads
the .yml file, validates it, dumps it back out to the console, and exits.

```
# Windows:
<PATH_TO_AGENT>\run.cmd localRun --whatif

# macOS and Linux:
<PATH_TO_AGENT>/run.sh localRun --whatif
```

## Task version resolution and caching

In local mode, you will be prompted for a server URL and credentials in order to download each
referenced task. Upon download, each task is cached under `<AGENT_ROOT>/_work/_tasks`. You will
not be prompted again after the tasks are cached.

When prompted for an URL, use the server URL and not the collection URL. For example:

VSTS: https://contoso.visualstudio.com

TFS: http://localhost:8080/tfs

When authenticating to VSTS, use auth type `pat`.

When authenticating to TFS, auth type `integrated` is typically used on Windows and `negotiate` on macOS/Linux.

## Command line help

Refer to the command line help for additional options.

```
# Windows:
<PATH_TO_AGENT>\run.cmd localRun --help

# macOS and Linux:
<PATH_TO_AGENT>/run.sh localRun --help
``` -->
