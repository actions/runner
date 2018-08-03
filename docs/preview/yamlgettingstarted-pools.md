# YAML getting started - Pools

When a phase is started, it is assigned to run on a specific pool.

## Hosted pools

The VSTS hosted pool has multiple images to choose from:
- Hosted VS2017
- Hosted
- Hosted macOS Preview
- Hosted Linux Preview

For example:

```yaml
queue: Hosted VS2017
steps:
- script: echo hello world
```

## Private pools

Private pools support `demands`, which can be used to route the phase to an available agent
within the pool. The demands are matched against agent capabilities.

For example:

```yaml
queue:
  name: MyPool
  demands: agent.os -equals Windows_NT
steps:
- script: echo hello world
```

Another example:

```yaml
queue:
  name: MyPool
  demands:
  - agent.os -equals Darwin
  - myCustomCapability -equals foo
  - myOtherCapability # exists
steps:
- script: echo hello world
```

## Server pool

The `Server` pool is a special type of pool, for tasks that do not require an agent.
Only a subset of tasks can run on the server pool.

For example:

```yaml
server: true
steps:
- task: InvokeRestApi@1
  inputs:
    serviceConnection: httpbin
    method: GET
    headers: '{ "Content-Type":"application/json" }'
    urlSuffix: get
```

## Authorization

For details about pool authorization, refer [here](yamlgettingstarted-authz.md).
