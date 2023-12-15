
# Actions Connection Check

## What is this check for?

Make sure the runner has access to actions service for GitHub.com or GitHub Enterprise Server

- For GitHub.com
  - The runner needs to access `https://api.github.com` for downloading actions.
  - The runner needs to access `https://codeload.github.com` for downloading actions tar.gz/zip.
  - The runner needs to access `https://vstoken.actions.githubusercontent.com/_apis/.../` for requesting an access token.
  - The runner needs to access `https://pipelines.actions.githubusercontent.com/_apis/.../` for receiving workflow jobs.
  ---
  **NOTE:** for the full list of domains that are required to be in the firewall allow list refer to the [GitHub self-hosted runners requirements documentation](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/about-self-hosted-runners#communication-between-self-hosted-runners-and-github).

  These can by tested by running the following `curl` commands from your self-hosted runner machine:

    ```
    curl -v https://api.github.com/zen
    curl -v https://codeload.github.com/_ping
    curl -v https://vstoken.actions.githubusercontent.com/_apis/health
    curl -v https://pipelines.actions.githubusercontent.com/_apis/health
    ```

- For GitHub Enterprise Server
  - The runner needs to access `https://[hostname]/api/v3` for downloading actions.
  - The runner needs to access `https://codeload.[hostname]/_ping` for downloading actions tar.gz/zip.
  - The runner needs to access `https://[hostname]/_services/vstoken/_apis/.../` for requesting an access token.
  - The runner needs to access `https://[hostname]/_services/pipelines/_apis/.../` for receiving workflow jobs.
  
  These can by tested by running the following `curl` commands from your self-hosted runner machine, replacing `[hostname]` with the hostname of your appliance, for instance `github.example.com`:

    ```
    curl -v https://[hostname]/api/v3/zen
    curl -v https://codeload.[hostname]/_ping
    curl -v https://[hostname]/_services/vstoken/_apis/health
    curl -v https://[hostname]/_services/pipelines/_apis/health
    ```

    A common cause of this these connectivity issues is if your to GitHub Enterprise Server appliance is using [the self-signed certificate that is enabled the first time](https://docs.github.com/en/enterprise-server/admin/configuration/configuring-network-settings/configuring-tls) your appliance is started. As self-signed certificates are not trusted by web browsers and Git clients, these clients (including the GitHub Actions runner) will report certificate warnings.
    
    We recommend [upload a certificate signed by a trusted authority](https://docs.github.com/en/enterprise-server/admin/configuration/configuring-network-settings/configuring-tls) to GitHub Enterprise Server, or enabling the built-in ][Let's Encrypt support](https://docs.github.com/en/enterprise-server/admin/configuration/configuring-network-settings/configuring-tls).


## What is checked?

- DNS lookup for api.github.com or myGHES.com using dotnet
- Ping api.github.com or myGHES.com using dotnet
- Make HTTP GET to https://api.github.com or https://myGHES.com/api/v3 using dotnet, check response headers contains `X-GitHub-Request-Id`
---
- DNS lookup for codeload.github.com or codeload.myGHES.com using dotnet
- Ping codeload.github.com or codeload.myGHES.com using dotnet
- Make HTTP GET to https://codeload.github.com/_ping or https://codeload.myGHES.com/_ping using dotnet, check response headers contains `X-GitHub-Request-Id`
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

Contact [GitHub Support](https://support.github.com) if you have further questuons, or log an issue at https://github.com/actions/runner if you think it's a runner issue.
