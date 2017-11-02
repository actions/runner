# YAML getting started - Endpoints (internal only, public preview soon)

Endpoints can be specified by name. For example,

```yaml
queue: Hosted VS2017
steps:
- task: TODO@1
  inputs:
    subscription: MyAzureSubscription
```

Note, for rename resiliency, endpoints can be specified by their GUID instead of name.

For details about tasks, refer [here](yamlgettingstarted-tasks.md).

For details about endpoint authorization, refer [here](yamlgettingstarted-authz.md).
