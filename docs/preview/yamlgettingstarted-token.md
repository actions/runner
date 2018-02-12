# YAML getting started - Allow scripts to access OAuth token

The OAuth token to communicate back to VSTS is available as a secret variable within a YAML build. The token can be use to authenticate to the [VSTS REST API](https://www.visualstudio.com/en-us/integrate/api/overview).

You can map the variable into the environment block for your script, or pass it via an input.

For example:

```yaml
steps:
- powershell: |
    $url = "$($env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI)$env:SYSTEM_TEAMPROJECTID/_apis/build/definitions/$($env:SYSTEM_DEFINITIONID)?api-version=2.0"
    Write-Host "URL: $url"
    $definition = Invoke-RestMethod -Uri $url -Headers @{
      Authorization = "Bearer $env:TOKEN"
    }
    Write-Host "Definition = $($definition | ConvertTo-Json -Depth 100)"
  env:
    TOKEN: $(system.accesstoken)
```
