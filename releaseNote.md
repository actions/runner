## Changes
- Artifacts v1 support, v2 works for ca. 6 month or longer
- Cancel reusable workflows and matrix construction
- Reference reusable workflows relative to current branch
- Fix cancelling matrix creation
- Implement concurrency support (#14)
- Implement permissions support if configured as github app (#13)
- Provide recursive needs ctx in job-if, parity with github
- Implement Endpoint for runner diagnostic logs
- Implement Endpoint to create and get Timelines
- Fix inputs ctx in job-if errored out
- Remove jobname from server github context, parity with github
- Implement basic check_runs on github via github app
- Fix exception while logging matrix evaluation
- Refactor secret passing of workflow_call
- No commit status / check_run for localcheckout and events other than
"push", "pull_request", "pull_request_target"
- Only opened, synchronize, synchronized, reopened actions of PR's creating a status
- github app installation token for reading ( reusable ) workflows
- added default types for pull_request and pull_request_target event
- adjust needs ctx creation to not access null Outputs github-act-runner
- adjust needs ctx creation, to keep failures even if later job succeeds
- fix race condition, where getmessage takes to long to abort
    GetMessage is now synchronized per session, to avoid loosing a job
- Return all artifacts, if the attempt of this run is unknown
    Used by the new workflow summary
- Persist workflow logs in the database
- Send the job property in timeline events
- Cleanup concurrency groups, if they are empty
- Always show the full job name in Runner.Client
    added temporary in memory jobs, which are cleaned up later
- Refactor: bundle workflow_call parameter in a class
- Move workflow_call workflow logging into it's own job logs
- Default types filter for all pull_request events
- Pseudo job for workflow_call, now always visible in webui
- skipping an called workflow, now fails the workflow in certain conditions
- New webui with grouping of jobs and workflow runs
    Resolves #5
- Refactor persisting timeline logs
- Fix db disposed while running multiple workflows
- Add 60s timeout for starting runners and server
- Fix handling errors in workflow outputs / job name
- Always add matrix dummy job to list
- Add open state of job steps to the UI
- Remove linefeeds from livelogs of actions/runner
- Fix issues while rerunning partial workflows
- Runner: Fix escape '\' before escaping '"'
- Added alpine linux support, the dotnet tool should now run on alpine
- Update to System.Commandline 2 beta 2
- Static github.token now available to workflow_call
- Mount docker pipe for windows container
- Fix access of actions artifacts azure app service
- Improve some type assert error messages
- accept scalars for branches(-ignore), paths(-ignore), types
- Use head branch for pull_requests by default
- Adjust max-parallel to match more with github
- Add github.repositoryUrl
- Try to avoid the 100s timeout on node download
- Fix missing caller job name in callable matrix job
- Send actions sha to actions/runner


- Add custom properties to github context via config GitHubContext
- Add config option AllowPrivateActionAccess for,
allowing accessing private repos for reusable workflows and actions
- Add repositoryUrl to github context
- Fix evaluating defaults of reusable workflows
- Fix respect permission of calling workflow only allow downgrade
- Only query one commit object from commits api on github
- Fix entity framework migration errors
always use static sqlite source in this case
- Include workflow ref, sha and result in the workflow run list
- Allow overriding github.ref from Runner.Client
- Allow overriding github.sha from Runner.Client
- Allow overriding github.repository from Runner.Client
- Correctly set sha for most events on reruns
not only for push and pull_request
- Fix pull request merge sha location in the pull_request event payload
- Allow to run an workflow with an empty event payload via Runner.Client
- Save workflow result in the database
- Fix selecting jobs inside of callable workflows concat jobids with `/`
- Show output name of workflow_call output prior execution
- Fix also compare workflow_call ref against sha to use cached workflow
- Runner access token now expires 10min after timeout, to give the job some time to finish
- Remove runner sessionid from log
- Rework parsing webhook event and retrieve a sha from server if necessary
- Remove old schedule interface of Runner.Client

## Security
- Mask secrets of workflow_call trace logs

## Known Issues

- **TODO** Manage Verbosity in more levels ideas are welcome, please open a discussion or issue

## Windows x64
We recommend configuring the runner in a root folder of the Windows drive (e.g. "C:\actions-runner"). This will help avoid issues related to service identity folder permissions and long file path restrictions on Windows.

The following snipped needs to be run on `powershell`:
``` powershell
# Create a folder under the drive root
mkdir \actions-runner ; cd \actions-runner
# Download the latest runner package
Invoke-WebRequest -Uri https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-win-x64-<RUNNER_VERSION>.zip -OutFile actions-runner-win-x64-<RUNNER_VERSION>.zip
# Extract the installer
Add-Type -AssemblyName System.IO.Compression.FileSystem ;
[System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD\actions-runner-win-x64-<RUNNER_VERSION>.zip", "$PWD")
```

## OSX

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz
```

## Linux x64

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz
```

## Linux arm64

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz
```

## Linux arm

``` bash
# Create a folder
mkdir actions-runner && cd actions-runner
# Download the latest runner package
curl -O -L https://github.com/ChristopherHX/runner.server/releases/download/v<RUNNER_VERSION>/actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz
# Extract the installer
tar xzf ./actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz
```

## Using your self hosted runner
For additional details about configuring, running, or shutting down the runner please check out our [product docs.](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/adding-self-hosted-runners)

## SHA-256 Checksums

The SHA-256 checksums for the packages included in this build are shown below:

- actions-runner-win-x64-<RUNNER_VERSION>.zip <!-- BEGIN SHA win-x64 --><WIN_X64_SHA><!-- END SHA win-x64 -->
- actions-runner-osx-x64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA osx-x64 --><OSX_X64_SHA><!-- END SHA osx-x64 -->
- actions-runner-linux-x64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-x64 --><LINUX_X64_SHA><!-- END SHA linux-x64 -->
- actions-runner-linux-arm64-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-arm64 --><LINUX_ARM64_SHA><!-- END SHA linux-arm64 -->
- actions-runner-linux-arm-<RUNNER_VERSION>.tar.gz <!-- BEGIN SHA linux-arm --><LINUX_ARM_SHA><!-- END SHA linux-arm -->

- actions-runner-win-x64-<RUNNER_VERSION>-noexternals.zip <!-- BEGIN SHA win-x64_noexternals --><WIN_X64_SHA_NOEXTERNALS><!-- END SHA win-x64_noexternals -->
- actions-runner-osx-x64-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA osx-x64_noexternals --><OSX_X64_SHA_NOEXTERNALS><!-- END SHA osx-x64_noexternals -->
- actions-runner-linux-x64-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA linux-x64_noexternals --><LINUX_X64_SHA_NOEXTERNALS><!-- END SHA linux-x64_noexternals -->
- actions-runner-linux-arm64-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA linux-arm64_noexternals --><LINUX_ARM64_SHA_NOEXTERNALS><!-- END SHA linux-arm64_noexternals -->
- actions-runner-linux-arm-<RUNNER_VERSION>-noexternals.tar.gz <!-- BEGIN SHA linux-arm_noexternals --><LINUX_ARM_SHA_NOEXTERNALS><!-- END SHA linux-arm_noexternals -->

- actions-runner-win-x64-<RUNNER_VERSION>-noruntime.zip <!-- BEGIN SHA win-x64_noruntime --><WIN_X64_SHA_NORUNTIME><!-- END SHA win-x64_noruntime -->
- actions-runner-osx-x64-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA osx-x64_noruntime --><OSX_X64_SHA_NORUNTIME><!-- END SHA osx-x64_noruntime -->
- actions-runner-linux-x64-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA linux-x64_noruntime --><LINUX_X64_SHA_NORUNTIME><!-- END SHA linux-x64_noruntime -->
- actions-runner-linux-arm64-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA linux-arm64_noruntime --><LINUX_ARM64_SHA_NORUNTIME><!-- END SHA linux-arm64_noruntime -->
- actions-runner-linux-arm-<RUNNER_VERSION>-noruntime.tar.gz <!-- BEGIN SHA linux-arm_noruntime --><LINUX_ARM_SHA_NORUNTIME><!-- END SHA linux-arm_noruntime -->

- actions-runner-win-x64-<RUNNER_VERSION>-noruntime-noexternals.zip <!-- BEGIN SHA win-x64_noruntime_noexternals --><WIN_X64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA win-x64_noruntime_noexternals -->
- actions-runner-osx-x64-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA osx-x64_noruntime_noexternals --><OSX_X64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA osx-x64_noruntime_noexternals -->
- actions-runner-linux-x64-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA linux-x64_noruntime_noexternals --><LINUX_X64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA linux-x64_noruntime_noexternals -->
- actions-runner-linux-arm64-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA linux-arm64_noruntime_noexternals --><LINUX_ARM64_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA linux-arm64_noruntime_noexternals -->
- actions-runner-linux-arm-<RUNNER_VERSION>-noruntime-noexternals.tar.gz <!-- BEGIN SHA linux-arm_noruntime_noexternals --><LINUX_ARM_SHA_NORUNTIME_NOEXTERNALS><!-- END SHA linux-arm_noruntime_noexternals -->
