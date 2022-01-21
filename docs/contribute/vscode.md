# Development Life Cycle using VS Code:

These examples use VS Code, but the idea should be similar across all IDEs as long as you attach to the same processes in the right folder.
## Configure

To successfully start the runner, you need to register it using a repository and a runner registration token.
Run `Configure` first to build the source code and set up the runner in `_layout`.
Once it's done creating `_layout`, it asks for the url of your repository and your token in the terminal.

Check [Quickstart](../contribute.md#quickstart-run-a-job-from-a-real-repository) if you don't know how to get this token.

## Debugging

Debugging the full lifecycle of a job can be tricky, because there are multiple processes involved.
If you're using macOS with Apple M1 chip (arm64 architecture), you need to uncomment all "targetArchitecture": "x86_64" lines in `.vscode/launch.json`.
All the configs below can be found in `.vscode/launch.json`.

## Debug the Listener

```json
{
    "name": "Run [build]",
    "type": "coreclr",
    "request": "launch",
    "preLaunchTask": "build runner layout",  // use the config called "Run" to launch without rebuild
    "program": "${workspaceFolder}/_layout/bin/Runner.Listener",
    "args": [
        "run" // run without args to print usage
    ],
    "cwd": "${workspaceFolder}/src",
    "console": "integratedTerminal",
    "requireExactSource": false,
    //"targetArchitecture": "x86_64"        // uncomment if you're using macOS M1
}
```

If you launch `Run` or `Run [build]`, it starts a process called `Runner.Listener`.
This process will receive any job queued on this repository if the job runs on matching labels (e.g `runs-on: self-hosted`).
Once a job is received, a `Runner.Listener` starts a new process of `Runner.Worker`.
Since this is a diferent process, you can't use the same debugger session debug it.
Instead, a parallel debugging session has to be started, using a different launch config.
Luckily, VS Code supports multiple parallel debugging sessions.

## Debug the Worker

Because the worker process is usually started by the listener instead of an IDE, debugging it from start to finish can be tricky.
For this reason, `Runner.Worker` can be configured to wait for a debugger to be attached before it begins any actual work.

Set the environment variable `GITHUB_ACTIONS_RUNNER_ATTACH_DEBUGGER` to `true` or `1` to enable this wait.
All worker processes now will wait 20 seconds before they start working on their task.

This gives enough time to attach a debugger by running `Debug Worker`.
If for some reason you have multiple workers running, run the launch config `Attach` instead.
Select `Runner.Worker` from the running processes when VS Code prompts for it.

