# YAML getting started - Run local (internal only, public preview soon)

The agent supports locally testing YAML configuration without queuing a build against the server.

When running in local mode, the YAML file(s) will be converted into a pipeline, and worker processes
invoked for each job. The agent will actually run the job but all agent calls to the VSTS server are
stubbed (and the server URL is hardcoded to 127.0.0.1).

A variable `agent.runMode`=`Local` is added to each job that is run. The variable can be leveraged
within your task conditions to optionally execute the step.

```
.\run.cmd --yaml <MY_FILE_PATH>

OR

./run.sh --yaml <MY_FILE_PATH>
```

Note, this is still in flux. Only task steps are currently supported. Sync-sources and resource-import/export
are not supported yet. Each job is run with syncSources=false.

## What-if mode

A \"what-if\" mode is supported for debugging the mustache text templating and YAML deserialization process.
What-if mode dumps the constructed pipeline to the console, and exits.

```
.\run.cmd --yaml <MY_FILE_PATH> --whatif

OR

./run.sh --yaml <MY_FILE_PATH> --whatif
```

## Task version resolution and caching

In local mode, all referenced tasks must either be pre-cached under \_work/\_tasks, or optionally credentials
can be supplied to query and download each referenced task from VSTS/TFS.

### Resolve and cache tasks from VSTS

```
.\run.cmd --yaml <MY_FILE_PATH> --url https://contoso.visualstudio.com --auth pat --token <TOKEN>

OR

./run.sh --yaml <MY_FILE_PATH> --url https://contoso.visualstudio.com --auth pat --token <TOKEN>
```

### Resolve and cache tasks from TFS (integrated auth) 

```
.\run.cmd --yaml <MY_FILE_PATH> --url http://localhost:8080/tfs

OR

./run.sh --yaml <MY_FILE_PATH> --url http://localhost:8080/tfs
```

### Resolve and cache tasks from TFS (negotiate auth)

Refer `--help` for all auth options.

```
.\run.cmd --yaml <MY_FILE_PATH> --url http://localhost:8080/tfs --auth negotiate --username <USERNAME> --password <PASSWORD>

OR

./run.sh --yaml <MY_FILE_PATH> --url http://localhost:8080/tfs --auth negotiate --username <USERNAME> --password <PASSWORD>
```
