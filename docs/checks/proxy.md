## Proxy Related Issues

If you see the following in the runner check log file, it means your runner is configured to use a web proxy for any HTTP/HTTPS requests.
```
***************************************************************************************************************
****                                                                                                       ****
****     Runner is behind web proxy http://127.0.0.1:9090 
****                                                                                                       ****
***************************************************************************************************************
```
> You can learn more about runner proxy configuration at [here](https://docs.github.com/en/free-pro-team@latest/actions/hosting-your-own-runners/using-a-proxy-server-with-self-hosted-runners).


### Some lessons we learned in the past around how a proxy can cause the runner to not working properly

- Proxy block certain HTTP method, like it block all POST and PUT calls which the runner will use to upload logs.

- Proxy only allows requests with certain user-agent to pass through and the actions runner user-agent is not in the allow list.

- Proxy try to decrypt and exam HTTPS traffic for security purpose but cause the actions-runner fail to finish SSL handshake due to the leak of trusting proxy's CA.

### Solutions for these problems

The key is to figure out where is the problem, the proxy or the actions runner?

- Set environment variable `GITHUB_ACTIONS_RUNNER_HTTPTRACE=true` before start the runner, this will make the runner to dump all HTTPS requests' headers to runner diagnostic logs, check the logs to make sure all responses were come from GitHub or GitHub Enterprise Service.

- Use `CURL` and `PowerShellCore` to make the same request as the runner did to configure requests can go through proxy with `CURL` and `PowerShellCore`.

- Check with your network administrator and the owner of the proxy to see whether he have any suggestion.

- Contact GitHub customer support or log an issue at https://github.com/actions/runner

