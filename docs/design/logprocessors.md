# Agent Log Processors RFC

## Goals

Provide an extensibility mechanism to process stdout logs.  

Initially this will be first party plugins packaged with the agent.  Eventually, this may be opened to external third party plugins.  Achieving that would require service side features to deliver as an extension.  It would also introduce another compatibility issue moving independently of the agent.

## Scenarios

  - Process log output for automatically publishing test results  
  - Process stdout to publish development tool usage

## Log Processing Plugins

We will introduce a log processing plugins very similar to other agent plugins.  Stdout will be passed to stdin.  Not keeping up with stdin can cause it to fail.  In order to avoid having every plugin to get that right (and to reduce risk), we will create one log processing host which buffers

![layers](res/AgentLogProcessors.png)

## Log Processing Host

TODO

## Lifetime

TODO

## Circuit Breaking

TODO

## Telemetry

TODO






