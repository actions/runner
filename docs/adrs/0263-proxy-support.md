# ADR 263: Self Hosted Runner Proxies

**Date**: 2019-11-13

**Status**: Accepted

## Context

- Proxy support is required for some enterprises and organizations to start using their own self hosted runners
- While there is not a standard convention, many applications support setting proxies via the environmental variables `http_proxy`, `https_proxy`, `no_proxy`, such as curl, wget, perl, python, docker, git, R, ect
  - Some of these applications use `HTTPS_PROXY` versus `https_proxy`, but most understand or primarily support the lowercase variant

## Decision

We will update the Runner to use the conventional environment variables for proxies: `http_proxy`, `https_proxy` and `no_proxy` if they are set.
These are described in detail below:
- `https_proxy` a proxy URL for all https traffic. It may contain basic authentication credentials. For example:
  - http://proxy.com
  - http://127.0.0.1:8080
  - http://user:password@proxy.com
- `http_proxy` a proxy URL for all http traffic. It may contain basic authentication credentials. For example:
  - http://proxy.com
  - http://127.0.0.1:8080
  - http://user:password@proxy.com
- `no_proxy` a comma separated list of hosts that should not use the proxy. An optional port may be specified
  - `google.com`
  - `yahoo.com:443`
  - `google.com,bing.com`

We won't use `http_proxy` for https traffic when `https_proxy` is not set, this behavior lines up with any libcurl based tools (curl, git) and wget.
Otherwise action authors and workflow users need to adjust to differences between the runner proxy convention, and tools used by their actions and scripts.  

Example:  
  Customer set `http_proxy=http://127.0.0.1:8888` and configure the runner against `https://github.com/owner/repo`, with the `https_proxy` -> `http_proxy` fallback, the runner will connect to the server without any problem. However, if a user runs `git push` to `https://github.com/owner/repo`, `git` won't use the proxy since it requires `https_proxy` to be set for any https traffic.

> `golang`, `node.js` and other dev tools from the linux community use `http_proxy` for both http and https traffic based on my research.

A majority of our users are using Linux where these variables are commonly required to be set by various programs. By reading these values, we simplify the process for self hosted runners to set up proxy, and expose it in a way users are already familiar with.

A password provided for a proxy will be masked in the logs.

We will support the lowercase and uppercase variants, with lowercase taking priority if both are set.

### No Proxy Format

While exact implementations are different per application on handle `no_proxy` env, most applications accept a comma separated list of hosts. Some accept wildcard characters (*). We are going to do exact case-insensitive matches, and not support wildcards at this time.
For example:
- example.com will match example.com, foo.example.com, foo.bar.example.com
- foo.example.com will match bar.foo.example.com and foo.example.com

We will not support IP addresses for `no_proxy`, only hostnames.

## Consequences

1. Enterprises and organizations needing proxy support will be able to embrace self hosted runners
2. Users will need to set these environmental variables before configuring the runner in order to use a proxy when configuring
3. The runner will read from the environmental variables during config and runtime and use the provided proxy if it exists
4. Users may need to pass these environmental variables into other applications if they do not natively take these variables
5. Action authors may need to update their workflows to react to the these environment variables
6. We will document the way of setting environmental variables for runners using the environment variables and how the runner uses them
7. Like all other secrets, users will be able to relatively easily figure out proxy password if they can modify a workflow file running on a self hosted machine
