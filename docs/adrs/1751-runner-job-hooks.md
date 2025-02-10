# ADR: Notification Hooks for Runners

## Context

This ADR details the design changes for supporting custom configurable hooks for on various runner events. This has been a long requested user feature [here](https://github.com/actions/runner/issues/1543), [here](https://github.com/actions/runner/issues/699) and [here](https://github.com/actions/runner/issues/1116) for users to have more information on runner observability, and for the ability to run cleanup and teardown jobs. 

This feature is mainly intended for self hosted runner administrators.

**What we hope to solve with this feature**
1. A runner admininstrator is able to add custom scripts to cleanup their runner environment at the start or end of a job
2. A runner admininstrator is able to add custom scripts to help setup their runner environment at the beginning of a job, for reasons like [caching](https://github.com/actions/runner/issues/1543#issuecomment-1050346279)
3. A runner administrator is able to grab custom telemetry of jobs running on their self hosted runner

**What we don't think this will solve**
- Policy features that require certain steps run at the beginning or end of all jobs
  - This would be better solved to in a central place in settings, rather then decentralized on each runner. 
  - The Proposed `Notification Hooks for Runners` is limited to self hosted runners, we don't beileve Policy features should be
- Reuse scenarios between jobs are covered by [composite actions](https://docs.github.com/en/actions/creating-actions/creating-a-composite-action) and [resuable workflows](https://docs.github.com/en/actions/using-workflows/reusing-workflows)
- Security applications, security should be handled on the policy side on the server, not decentralized on each runner

## Hooks
- We will expose 2 variables that users can set to enable hooks  
  - `ACTIONS_RUNNER_HOOK_JOB_STARTED`
  - `ACTIONS_RUNNER_HOOK_JOB_COMPLETED`

You can set these variables to the **absolute** path of a `.sh` or `.ps1` file.

We will execute `pwsh` (fallback to `powershell`) or `bash` (fallback to `sh`) as appropriate.
- `.sh` files will execute with the args `-e {pathtofile}`
- `.ps1` files will execute with the args `-command \". '{pathtofile}'\"`

We will **not** set the [standard flags we typically set](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions#jobsjob_idstepsshell) for `runs` commands. So, if you want to set `pipefail` on `bash` for example, you will need to do that in your script.

### UI
We want to ensure the experience for users invoking workflows is good, if hooks take too long, you may feel your job is delayed or broken. So, much like `Set Up Job`, we will generate two new steps automatically in your job, one for each configured hook: 
- `Set up runner`  
- `Complete runner` 

These steps will contain all of the output from invoking your hook, so you will have visibility into the runtime. We will also provide information on the path to the hook, and what shell we are invoking it as, much like we do for `run: ` steps.

### Contexts
When running your hooks, some context on your job may be helpful.
- The scripts will have access to the standard [default environment variables](https://docs.github.com/en/actions/learn-github-actions/environment-variables#default-environment-variables)
  - Some of these variables are step specific like `GITHUB_ACTION`, in which case they will not be set
- You can pull the full webhook event payload from `GITHUB_EVENT_PATH`

### Commands
Should we expose [Commands](https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions) and [Environment Files](https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#environment-files)

**Yes**. Imagine a scenario where a runner administrator is deprecating a runner pool, and they need to [warn users](https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#setting-a-warning-message) to swap to a different pool, we should support them in doing this. However, there are some limitations:
- [save-state](https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#sending-values-to-the-pre-and-post-actions) will **not** be supported, these are not traditional steps with pre and post actions
- [set-output](https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#using-workflow-commands-to-access-toolkit-functions) will **not** be supported, there is no `id` as this is not a traditional step


### Environment Files
We will also enable [Environment Files](https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#environment-files) to support setup scenarios for the runner environment.

While a self hosted runner admin can [set env variables](https://docs.github.com/en/actions/hosting-your-own-runners/using-a-proxy-server-with-self-hosted-runners#using-a-env-file-to-set-the-proxy-configuration), these apply to all jobs. By enabling the ability to `add a path` and `set an env` we give runner admins the ability to do this dynamically based on the [workflows environment variables](https://docs.github.com/en/actions/learn-github-actions/environment-variables#default-environment-variables) to empower setup scenarios.


### Exit codes
These are **synchronous** hooks, so they will block job execution while they are being run. Exit code 0 will indicate a successful run of the hook and we will proceed with the job, any other exit code will fail the job with an appropriate annotation.
- There will be no support for `continue-on-error`

## Key Decisions
- We will expose 2 variables that users can set to enable hooks  
  - `ACTIONS_RUNNER_HOOK_JOB_STARTED`
  - `ACTIONS_RUNNER_HOOK_JOB_COMPLETED`
- Users can set these variables to the path of a `.sh` or `.ps1` file, which we will execute when Jobs are started or completed.
  - Output from these will be added to a new step at the start/end of a job named `Set up runner` or `Complete runner`. 
    - These steps will only be generated on runs with these hooks
- These hooks `always()` execute if the env variable is set
- These files will execute as the Runner user, outside of any container specification on the job
- These are **synchronous** hooks
  - Runner admins can execute a background process for async hooks if they want
  - We will fail the job and halt execution on any exit code that is not 0. The Runner admin is responsible for returning the correct exit code and ensuring resilency. 
    - This includes that the runner user needs access to the file in the env and the file must exist
    - There will be no `continue-on-error` type option on launch
    - There will be no `timeout` option on launch

## Consequences
- Runner admins have the ability to tie into the runner job execution to publish their own telemetry or perform their own cleanup or setup
- New steps will be added to the UI showcasing the output of these hooks
