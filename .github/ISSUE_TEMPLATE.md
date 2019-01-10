## Having issue with YAML?
Please log an issue at [Azure-Pipelines-YAML](https://github.com/Microsoft/azure-pipelines-yaml). Over there we discuss YAML templates, samples for Azure Pipelines, and designs for upcoming YAML features. Also a place for the community to share best practices, ideas, and so on. File suggestions and issues here if they're specific to YAML pipelines.

## Having issue with Tasks?
Log an issue at [Azure-Pipelines-Tasks](https://github.com/Microsoft/azure-pipelines-tasks). It contains all of the in-box tasks we ship with Azure-Pipelines/VSTS/TFS. If you're having issues with tasks in Build/Release jobs (e.g. unreasonable task failure) please log an issue there.

## Having issue with software on Hosted Agent?
Log an issue at [Hosted Agent Image Repository](https://github.com/Microsoft/vsts-image-generation). It contains the VM image used in the Azure Pipelines Hosted Agent Pool. If you're having Build/Release failures that seems like they are related to software installed on the Hosted Agent (e.g. the `dotnet` SDK is missing or the Azure SDK is not on the latest version) please log an issue there.

## Having generic issue with Azure-Pipelines/VSTS/TFS?
Please report it on [Developer Community](https://developercommunity.visualstudio.com/spaces/21/index.html)

## Have you tried troubleshooting?
[Troubleshooting doc](https://www.visualstudio.com/en-us/docs/build/troubleshooting)

## Agent Version and Platform
Version of your agent? 2.144.0/2.144.1/...

OS of the machine running the agent? OSX/Windows/Linux/...

## Azure DevOps Type and Version
dev.azure.com (formerly visualstudio.com) or on-premises TFS/Azure DevOps Server?

If on-premises, which release? 2015.0, 2017.1, 2019 RC2, etc.

If dev.azure.com, what is your organization name? https://dev.azure.com/{organization} or https://{organization}.visualstudio.com

## What's not working?
Please include error messages and screenshots.

## Agent and Worker's Diagnostic Logs
Logs are located in the agent's `_diag` folder. The agent logs are prefixed with `Agent_` and the worker logs are prefixed with `Worker_`. All sensitive information should already be masked out, but please double-check before pasting here.
