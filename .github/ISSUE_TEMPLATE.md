## Have you tried trouble shooting?
[Trouble shooting doc](https://www.visualstudio.com/en-us/docs/build/troubleshooting)

## Agent Version and Platform
Version of your agent? 2.102.0/2.100.1/...

OS of the machine running the agent? OSX/Windows/Linux/...

## VSTS Type and Version
VisualStudio.com or On-Prem TFS?

If On-Prem TFS, which release? 2015 RTM/QU1/QU2/...

If VisualStudio.com, what is your account name? http://account.visualstudio.com

## What's not working?
Please include error messages and screenshots.

## Agent and Worker's Diagnostic Logs
Logs are located in the agent's `_diag` folder. The agent logs are prefixed with `Agent_` and the worker logs are prefixed with `Worker_`. All sensitive information should already be masked out, please double check before pasting here.

## Related Repositories
Please ensure you are logging issues to the correct repository in order to get the best support.

- [Tasks Repository](https://github.com/Microsoft/vsts-tasks) - contains all of the inbox tasks we ship with VSTS/TFS. If you are having issues with tasks in Build/Release jobs (e.g. unreasonable task failure) please log an issue [here](https://github.com/Microsoft/vsts-tasks/issues).
- [Hosted Agent Image Repository](https://github.com/Microsoft/vsts-image-generation) - contains the VM image used in the VSTS Hosted Agent Pool. If you are having Build/Release failures that seems like they are related to software installed on the Hosted Agent (e.g. the DotnetSDK is missing or the AzureSDK is not on the latest version) please log an issue [here](https://github.com/Microsoft/vsts-image-generation/issues).

If you are hitting a generic issue about VSTS/TFS, please report it to the [Developer Community](https://developercommunity.visualstudio.com/spaces/21/index.html)
