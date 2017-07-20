# How Proxy Works in Agent and Task Execution

## Goals

  - Support agent configure and connect to VSTS/TFS behind web proxy
  - Support get source in Build job and download artifact in Release job works behind web proxy
  - Expose proxy agent configuration in vsts-task-lib for task author

## Configuration

Documentation for configuring agent to follow web proxy can be found [here](https://www.visualstudio.com/en-us/docs/build/actions/agents/v2-windows#how-do-i-configure-the-agent-to-work-through-a-web-proxy-and-connect-to-team-services).  
In short:  
  - Create a `.proxy` file under agent root to specify proxy url.  
    Ex:
    ```
    http://127.0.0.1:8888
    ```
  - For authenticate proxy set environment variables `VSTS_HTTP_PROXY_USERNAME` and `VSTS_HTTP_PROXY_PASSWORD` for proxy credential before start agent process.
  - Create a `.proxybypass` file under agent root to specify proxy bypass Url's Regex (ECMAScript syntax).  
    Ex:
    ```
    github\.com
    bitbucket\.com
    ```

## How agent handle proxy within a Build/Release job

After configuring proxy for agent, agent infrastructure will start talk to VSTS/TFS service through the web proxy specified in the `.proxy` file.  

Since the code for `Get Source` step in build job and `Download Artifact` step in release job are also bake into agent, those steps will also follow the agent proxy configuration from `.proxy` file.  

Agent will expose proxy configuration via environment variables for every task execution, task author need to use `vsts-task-lib` methods to retrieve back proxy configuration and handle proxy with their task.

## Get proxy configuration by using [VSTS-Task-Lib](https://github.com/Microsoft/vsts-task-lib) method

Please reference [VSTS-Task-Lib doc](https://github.com/Microsoft/vsts-task-lib/blob/master/node/docs/proxy.md) for detail