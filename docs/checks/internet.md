# Internet Connection Check

## What is this check for?

Make sure runner have access to https://api.github.com

The runner needs to access to https://api.github.com to download any actions from the marketplace.

Even the runner is configured to GitHub Enterprise Server, the runner can still download actions from GitHub.com with [GitHub Connect](https://docs.github.com/en/enterprise-server@2.22/admin/github-actions/enabling-automatic-access-to-githubcom-actions-using-github-connect)


## What is checked?

- DNS lookup for api.github.com using dotnet
- Ping api.github.com using dotnet
- Make HTTP GET to https://api.github.com using dotnet, check response headers contains `X-GitHub-Request-Id` 

## How to fix the issue?

- Check common issue caused by [proxy](./proxy.md)
- Use `nslookup` to check `api.github.com`
- Use `ping` to check `api.github.com`
- Use `curl -v` to check `https://api.github.com`
- Use `Invoke-WebRequest` from `pwsh` to check `https://api.github.com`

Contact network administrator if everything points to the network.  

Contact GitHub customer service or log an issue at https://github.com/actions/runner if you think it's a runner issue.