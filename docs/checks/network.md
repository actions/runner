## Common Network Related Issues

### Common things that can cause the runner to not working properly

- A bug in the runner or the dotnet framework that causes the actions runner to be unable to make Http requests in a certain network environment.

- A Proxy or Firewall may block certain HTTP method, such as blocking all POST and PUT calls which the runner will use to upload logs.

- A Proxy or Firewall may only allows requests with certain user-agent to pass through and the actions runner user-agent is not in the allow list.

- A Proxy try to decrypt and exam HTTPS traffic for security purpose but cause the actions-runner to fail to finish SSL handshake due to the lack of trusting proxy's CA.

- The SSL handshake may fail if the client and server do not support the same TLS version, or the same cipher suites.

- A Proxy may try to modify the HTTPS request (like add or change some http headers) and causes the request become incompatible with the Actions Service (ASP.NetCore), Ex: [Nginx](https://github.com/dotnet/aspnetcore/issues/17081)

- Firewall rules that block action runner from accessing [certain hosts](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/about-self-hosted-runners#communication-between-self-hosted-runners-and-github), ex: `*.github.com`, `*.actions.githubusercontent.com`, etc


### Identify and solve these problems

The key is to figure out where is the problem, the network environment, or the actions runner?

Use a 3rd party tool to make the same requests as the runner did would be a good start point.

- Use `nslookup` to check DNS
- Use `ping` to check Ping
- Use `traceroute`, `tracepath`, or `tracert` to check the network route between the runner and the Actions service
- Use `curl -v` to check the network stack, good for verifying default certificate/proxy settings.
- Use `Invoke-WebRequest` from `pwsh` (`PowerShell Core`) to check the dotnet network stack, good for verifying bugs in the dotnet framework.

If the 3rd party tool is also experiencing the same error as the runner does, then you might want to contact your network administrator for help.

Otherwise, contact GitHub customer support or log an issue at https://github.com/actions/runner

### Troubleshooting: Why can't I configure a runner?

If you are having trouble connecting, try these steps:

1. Validate you can reach our endpoints from your web browser. If not, double check your local network connection
    - For hosted Github:
      - https://api.github.com/
      - https://vstoken.actions.githubusercontent.com/_apis/health
      - https://pipelines.actions.githubusercontent.com/_apis/health
      - https://results-receiver.actions.githubusercontent.com/health
    - For GHES/GHAE
      - https://myGHES.com/_services/vstoken/_apis/health
      - https://myGHES.com/_services/pipelines/_apis/health
      - https://myGHES.com/api/v3
2. Validate you can reach those endpoints in powershell core
    - The runner runs on .net core, lets validate the local settings for that stack
    - Open up `pwsh`
    - Run the command using the urls above `Invoke-WebRequest {url}`
3. If not, get a packet trace using a tool like wireshark and start looking at the TLS handshake.
    - If you see a Client Hello followed by a Server RST:
      - You may need to configure your TLS settings to use the correct version
        - You should support TLS version 1.2 or later
      - You may need to configure your TLS settings to have up to date cipher suites, this may be solved by system updates and patches.
        - Most notably, on windows server 2012 make sure [the tls cipher suite update](https://support.microsoft.com/en-us/topic/update-adds-new-tls-cipher-suites-and-changes-cipher-suite-priorities-in-windows-8-1-and-windows-server-2012-r2-8e395e43-c8ef-27d8-b60c-0fc57d526d94) is installed
      - Your firewall, proxy or network configuration may be blocking the connection
      - You will want to reach out to whoever is in charge of your network with these pcap files to further troubleshoot
    - If you see a failure later in the handshake:
      - Try the fix in the [SSLCert Fix](./sslcert.md)
