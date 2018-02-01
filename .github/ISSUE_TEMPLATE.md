## Have you try trouble shooting doc?
Link to trouble shooting doc: https://www.visualstudio.com/en-us/docs/build/troubleshooting

## Agent version and platform
Version of your agent? 2.102.0/2.100.1...

OS of the machine running the agent? OSX/Windows/Linux...

## VSTS type and version
VisualStudio.com or On-Prem TFS?

If On-Prem TFS, which release, 2015 RTM/QU1/QU2?

If VisualStudio.com, what is your account name? http://account.visualstudio.com

## What's not working?
Please include error messages and screenshots

## Agent and Worker's diag log
Logs are located at the `_diag` under agent root folder, agent log prefix with `Agent_`, worker log prefix with `Worker_`. all sensitive information should already be masked out, please double check before paste here. 

## Relative repositories
You might want to report the issue you have to different repository for the right support.

- [Tasks Repository](https://github.com/Microsoft/vsts-tasks)  
  The repository contains all the inbox task we ship to VSTS/TFS.  
  If you are having issue with task in Build/Release job, like unrasonable task failure, please log issue there.
- [Hosted Agent Image Repository](https://github.com/Microsoft/vsts-image-generation)  
  The repository is for the VM image used in VSTS Hosted Agent Pool.  
  If you are having Build/Release failure that seems like related to software installed on the Hosted Agent, like DotnetSDK is missing or AzureSDK is not on latest version, please log issue there.

If you are hitting genaric issue about VSTS/TFS, please report at [Developer Community](https://developercommunity.visualstudio.com/spaces/21/index.html)
