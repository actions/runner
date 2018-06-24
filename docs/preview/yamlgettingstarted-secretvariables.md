# YAML getting started - Secret variables

## Secrets are masked from output logs

The agent masks secret values from the streaming output from steps.

For example, consider the following definition:

```yaml
steps:

# Create a secret variable
- powershell: |
    write-host '##vso[task.setvariable variable=mySecret;isSecret=true]abc123'

# Print the secret variable
- powershell: |
    write-host 'The secret is: $(mySecret)'
```

The output from the second step is:

```
The secret is: ***
```

Note, secrets are masked before the streaming output is posted to the server, and before the output is written to the job logs on disk.

## Ad hoc script requires explicit secret mapping

Use `env` to map secrets variables into the process environment block for your script. Otherwise secret variables are not mapped for ad hoc scripts.

This is a precautionary measure to avoid inadvertently recording the values. For example, a crash dump or a downstream process logging environment variables.

The following definition illustrates the behavior:

```yaml
steps:

# Create a secret variable
- powershell: |
    Write-Host '##vso[task.setvariable variable=mySecret;issecret=true]abc'

# Attempt to output the value in various ways
- powershell: |
    # Using an input-macro:
    Write-Host "This works: $(mySecret)"

    # Using the env var directly:
    Write-Host "This does not work: $env:MYSECRET"

    # Using the mapped env var:
    Write-Host "This works: $env:MY_MAPPED_ENV_VAR"
  env:
    MY_MAPPED_ENV_VAR: $(mySecret)
```

The output from the second script is:

```
This works: ***
This does not work:
This works: ***
```

### Security Note

The `env` input sets an environment variable for the child process.

You can author your script to clear the environment variable after the value is retrieved. This will prevent further downstream processes from receiving the value in memory.

Otherwise when `$(macro)` syntax is used within your script, the value will be embedded in the temporary script that is written to disk.
