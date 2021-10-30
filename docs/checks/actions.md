
# Actions Connection Check

## What is this check for?

Make sure the runner has access to actions service for GitHub.com or GitHub Enterprise Server

- For GitHub.com
  - The runner needs to access https://api.github.com for downloading actions.
  - The runner needs to access https://vstoken.actions.githubusercontent.com/_apis/.../ for requesting an access token.
  - The runner needs to access https://pipelines.actions.githubusercontent.com/_apis/.../ for receiving workflow jobs.
- For GitHub Enterprise Server
  - The runner needs to access https://myGHES.com/api/v3 for downloading actions.
  - The runner needs to access https://myGHES.com/_services/vstoken/_apis/.../ for requesting an access token.
  - The runner needs to access https://myGHES.com/_services/pipelines/_apis/.../ for receiving workflow jobs.

## What is checked?

- DNS lookup for api.github.com or myGHES.com using dotnet
- Ping api.github.com or myGHES.com using dotnet
- Make HTTP GET to https://api.github.com or https://myGHES.com/api/v3 using dotnet, check response headers contains `X-GitHub-Request-Id`
---
- DNS lookup for vstoken.actions.githubusercontent.com using dotnet
- Ping vstoken.actions.githubusercontent.com using dotnet
- Make HTTP GET to https://vstoken.actions.githubusercontent.com/_apis/health or https://myGHES.com/_services/vstoken/_apis/health using dotnet, check response headers contains `x-vss-e2eid`
---
- DNS lookup for pipelines.actions.githubusercontent.com using dotnet
- Ping pipelines.actions.githubusercontent.com using dotnet
- Make HTTP GET to https://pipelines.actions.githubusercontent.com/_apis/health or https://myGHES.com/_services/pipelines/_apis/health using dotnet, check response headers contains `x-vss-e2eid`
- Make HTTP POST to https://pipelines.actions.githubusercontent.com/_apis/health or https://myGHES.com/_services/pipelines/_apis/health using dotnet, check response headers contains `x-vss-e2eid`

## How to fix the issue?

### 1. Check the common network issue
  
  > Please check the [network doc](./network.md)

### 2. SSL certificate related issue

  If you are seeing `System.Net.Http.HttpRequestException: The SSL connection could not be established, see inner exception.` in the log, it means the runner can't connect to Actions service due to SSL handshake failure.
  > Please check the [SSL cert doc](./sslcert.md)
  
## Still not working?

Contact GitHub customer service or log an issue at https://github.com/actions/runner if you think it's a runner issue.
