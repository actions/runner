## Common Network Related Issues

### Some lessons we learned in the past around things that can cause the runner to not working properly

- Bug in the Dotnet framework that cause actions runner can't make Http request in a certain network environment.

- Proxy/Firewall block certain HTTP method, like it block all POST and PUT calls which the runner will use to upload logs.

- Proxy/Firewall only allows requests with certain user-agent to pass through and the actions runner user-agent is not in the allow list.

- Proxy try to decrypt and exam HTTPS traffic for security purpose but cause the actions-runner fail to finish SSL handshake due to the leak of trusting proxy's CA.

- Firewall rules that block action runner from access certain hosts, ex: `*.github.com`, `*.actions.githubusercontent.com`, etc.


### Identify and solve these problems

The key is to figure out where is the problem, the network environment, or the actions runner?

Use a 3rd party tool to make the same requests as the runner did would be a good start point.

- Use `nslookup` to check DNS
- Use `ping` to check Ping
- Use `curl -v` to check the network stack, good for verifying default certificate/proxy settings.
- Use `Invoke-WebRequest` from `pwsh` (`PowerShell Core`) to check the dotnet network stack, good for verifying bugs in the dotnet framework.

If the 3rd party tool is also experiencing the same error as the runner does, then you might want to contact your network administrator for help.

Otherwise, contact GitHub customer support or log an issue at https://github.com/actions/runner