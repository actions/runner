# Using VSTS Agent behind Proxy

## Key Points
  - Create .proxy file with proxy url under agent root directory.  
  - If using an authenticated proxy, set authenticate proxy credential through environment variable   
    `VSTS_HTTP_PROXY_USERNAME` and `VSTS_HTTP_PROXY_PASSWORD`  

## Steps
  1. Create .proxy file with your proxy server url under agent root directory.  
  
  ```bash
  echo http://proxyserver:8888 > .proxy
  ```  
  
  If your proxy doesn't require authentication or the default network credential of the current vsts agent run as user is able to authenticate with proxy, then your agent proxy configure has finished. Configure and run agent as normal.  
  
  *note: For back compat reason, we will fallback to read proxy url from envrionment variable VSTS_HTTP_PROXY.*
  
  2. If your proxy requires additionally authentication, you will need to provide that credential to vsts agent through environment variables. We will treate the proxy credential as sensitive information and mask it in any job logs or agent diag logs.  
  
  **Set following environment variables before configure and run vsts agent.**  
  ###Windows  
  ```batch
  set VSTS_HTTP_PROXY_USERNAME=proxyuser
  set VSTS_HTTP_PROXY_PASSWORD=proxypassword
  ```  
   
  ###Unix and OSX  
  ```bash
  export VSTS_HTTP_PROXY_USERNAME=proxyuser
  export VSTS_HTTP_PROXY_PASSWORD=proxypassword
  ```  
  
  *If your agent is running as service on Unix or OSX, you will need to add following section to .env file under agent root directory, then execute ./env.sh to update service envrionment variable.*
  ```
  VSTS_HTTP_PROXY_USERNAME=proxyuser
  VSTS_HTTP_PROXY_PASSWORD=proxypassword
  ```
  [Details here](nixsvc.md#setting-the-environment)
  
## Limitations  
  - Only agent infustructure itself has proxy support, which means the agent is able to run a Build/Release job behind proxy. However, you still have to setup proxy config for each individual tool that agent invoke during a Build/Release job.  
    Ex, 
      - proxy config for git.
      - proxy config for any tasks that make REST call. (We will add built-in proxy support to task lib.)
  
  
  
