# YAML getting started - Phase options

## Continue on error

When `continueOnError` is set to true and the phase fails, the phase result will be \"Succeeded with issues\" instead of "Failed\".

## Timeout

The `timeoutInMinutes` allows a limit to be set for the job execution time. When not specified, the default is 60 minutes.

The `cancelTimeoutInMinutes` allows a limit to be set for the job cancel time. When not specified, the default is 5 minutes.

The schema is:

```yaml
queue:
  timeoutInMinutes: number
  cancelTimeoutInMinutes: number
```

and:

```yaml
server:
  timeoutInMinutes: number
  cancelTimeoutInMinutes: number
```

## Variables

Variables can be specified on a phase. Refer [here](yamlgettingstarted.md#Variables) for more information about variables.
