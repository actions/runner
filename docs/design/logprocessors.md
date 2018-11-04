# Agent Log Processors RFC

## Goals

Provide an extensibility mechanism which offers other teams and partners to do additional processing on log output cooperatively as part of the job (as opposed to post job log processing).

Performance and reliability will be critical.  

## Scenarios 

### Publishing Test Results

In addition to our tasks, testing tools can be invoked from command lines and 
Scan the output looking for well known output patterns and publish test results to our test management service.  In addition many test frameworks do not have reporters by default.

These test tools can be called from our tasks but also via command lines and scripts such as powershell, shellscripts, python, javascript or any scripting technology.  They can also be called via `npm test` which is simply an indirection to a set of cmd lines and script calls. 

### Telemetry on Tool Usage

It is useful to know the usage and trends of certain scenarios being leverage via Azure Pipelines.  For example, we may want to know the numbers and trends of packages published to npm or nuget, docker images published or kubernetes configurations applied using our pipelines.

Once again, these may be called via tasks, cmd lines, scripts and even runners like `npm run publish`.

### Send Output to Another Service

Output could be processed and sent to another service for storage and processing.  Alternatively, via config, writing back logs to Azure Pipelines can be disabled with the plugin logger offering a message to be substituted.

## Log Processing Plugins

We will introduce a log processing plugins very similar to other agent plugins.

Currently the task handlers output is sent to a command pluging if the line starts with ##vso.  Else, it's passed to the agents logger which sends to the live console circular buffer and permanent log storage in pages.

In a companion out of proc log processing extensibility point, output can be processed in parallel with our log processing.  Plugins will be loaded in-proc of the log host and will be written in net core loadable by the host.  It will not block our live console and log publishing.

Not keeping up with stdin can cause it to fail.  In order to avoid having every plugin to get that right (and to reduce risk), we will create one log processing host which buffers

![layers](res/AgentLogProcessors.png)

## Log Processing Host

To ensure log processing plugins do not block stdin, the host will take care of buffering output, processing that buffer or queue of log lines and processing that queue.  That buffering may start out as in memory similar to our other queues but we could consider backing it by files if required.

As it's processed each plugin will be called with `line()`.  That will be a blocking call per plugin which would ideally do light processing or alter internal tracking state and return.

If a plugin writes transient state data, it should do it in the agent temp folder so it gets cleaned up automatically by the agent.  To encourage this, the host plugin will set a property on the plugin where to write transient state if needed.

It is a requirement that plugins process output in a stream sax style processing style.  Buffering the full log and then processing will not be efficient and may get you flagged in telemetry or terminated.

Each plugin will profer a friendly user message on it's role used in user feedback (see below).

The processing host will also have deep tracing in agent diagnostics.

## Lifetime

We will call each plugin waiting for it to complete processing.  Clear output in the users live console and log will make it clear that we are waiting on "processing test results", "processing telemetry".  This will be done **in the context of the job**.  That feedback will hang off of a finishing up job style step.

We will explictly avoid long investigation that we've had in the past where it appears a tasks work is complete (the tool outputted done) but the tasks appears to hang for minutes when in reality something is doing processing before the task or job can complete.

## Circuit Breaking

The worker will monitor the log host process.  If it crashes or returns a non success code, telemetry will be sent.  

The agent and worker should continue reliably in the even of any issues with side processing.

Question: Can the log host monitor memory and CPU usage of itself and circuit break itself?  Ideally yes.  Investigate across platforms supported.

## Telemetry

We need telemetry on:  

  - Disabling log hosts
  - Failure to load a plugin: it will be disabled
  - Memory usage of the out of proc log host processor (plugins are in proc to that)
  - Add more here

## Testing  

Since this work has the potential to be impactful on performance and reliability we will do heavy L0 testing around both the positive cases and the negative scenarios (getting circuit breaks etc...).  In the negative case testing, we can simply set the threshholds extremely low.  For exampleset memory consumption or processor utilization very low to avoid taking down the box running the tests.  We are testing the circuit breaking functionality.

Each plugin should be heavily tested in L0 fashion by contributing a set of output files and baseline results.  The tests will feed the output test files into the log processing host with the plugin writing it's conclusions to an output file that we baseline and automate.

## Scope and Delivery

Initially this will be first party plugins packaged with the agent.  Eventually, this may be opened to external third party plugins.  Achieving that would require service side features to deliver as an extension.  It would also introduce another compatibility issue moving independently of the agent.

If we expose externally (not delivered as part of the agent), we will offer the ability to be your own log processing host because of the compat and dependency problems (agents stay back and get auto updated).  This is a long discussion out of the scope of this design document.

