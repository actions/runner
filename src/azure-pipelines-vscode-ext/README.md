# Azure Pipelines Tools VSCode Extension

This is a minimal Azure Pipelines Extension, the first vscode Extension which can Validate and Expand Azure Pipeline YAML files locally without any REST service.

![Validate Azure Pipelines via ContextMenu](https://raw.githubusercontent.com/ChristopherHX/runner.server/main/docs/azure-pipelines/images/validate-azure-pipeline-via-contextmenu.gif)
![Expand Azure Pipelines via ContextMenu](https://raw.githubusercontent.com/ChristopherHX/runner.server/main/docs/azure-pipelines/images/expand-azure-pipeline-via-contextmenu.gif)

## Features

### Remote Template References

The `azure-pipelines-vscode-ext.repositories` settings maps the external Repositories to local or remote folders.

Syntax `[<owner>/]<repo>@<ref>=<uri>` per line. `<uri>` can be formed like `file:///<folder>` (raw file paths are not supported (yet?)), `vscode-vfs://github/<owner>/<repository>` and `vscode-vfs://azurerepos/<owner>/<project>/<repository>`

### Validate Azure Pipeline

`> Validate Azure Pipeline`

This command tries to evaluate your current open Azure Pipeline including templates and notifies you about the result.

_Once this extension has been activated by any command, you can validate your pipeline via a statusbar button with the same name on all yaml or azure-pipelines documents_

### Expand Azure Pipeline

`> Expand Azure Pipeline`

This command tries to evaluate your current open Azure Pipeline including templates and show the result in a new document, which you can save or validate via the official api.

### Azure Pipelines Linter Task

You can configure parameters, variables, repositories per task. You can define multiple tasks with different parameters and variables or filenames to catch errors on changing template files as early as possible.

`.vscode/tasks.json`
```jsonc
{
    "version": "2.0.0",
    "tasks": [
        {
            "type": "azure-pipelines-vscode-ext",
            "label": "test",
            "program": "${workspaceFolder}/azure-pipeline.yml",
            "repositories": {
                "myrepo@windows": "file:///C:/AzurePipelines/myrepo",
                "myrepo@unix": "file:///AzurePipelines/myrepo",
                "myrepo@github": "vscode-vfs://github/AzurePipelines/myrepo", // Only default branch, url doesn't accept readable ref
                "myrepo@azure": "vscode-vfs://azurerepos/AzurePipelines/myrepo/myrepo" // Only default branch, url doesn't accept readable ref
            },
            "parameters": {
                "booleanparam": true,
                "numberparam": 12,
                "stringparam": "Hello World",
                "objectparam": {
                    "booleanparam": true,
                    "numberparam": 12,
                    "stringparam": "Hello World",
                },
                "arrayparam": [
                    true,
                    12,
                    "Hello World"
                ]
            },
            "variables": {
                "system.debug": "true"
            },
            "preview": true, // Show a preview of the expanded yaml
            "watch": true    // Watch for yaml file changes
        },
        {
            "type": "azure-pipelines-vscode-ext",
            "label": "test2",
            "program": "${workspaceFolder}/azure-pipeline.yml",
            "repositories": {
                "myrepo@windows": "file:///C:/AzurePipelines/myrepo",
                "myrepo@unix": "file:///AzurePipelines/myrepo",
                "myrepo@github": "vscode-vfs://github/AzurePipelines/myrepo", // Only default branch, url doesn't accept readable ref
                "myrepo@azure": "vscode-vfs://azurerepos/AzurePipelines/myrepo/myrepo" // Only default branch, url doesn't accept readable ref
            },
            "parameters": {
                "booleanparam": true,
                "numberparam": 12,
                "stringparam": "Hello World",
                "objectparam": {
                    "booleanparam": true,
                    "numberparam": 12,
                    "stringparam": "Hello World",
                },
                "arrayparam": [
                    true,
                    12,
                    "Hello World"
                ]
            },
            "variables": {
                "system.debug": "true"
            },
            "watch": true    // Watch for yaml file changes
        }
    ]   
}
```
Sample Pipeline which dumps the parameters object (legacy parameters syntax)
```yaml
parameters:
  booleanparam:
  numberparam:
  stringparam:
  objectparam:
  arrayparam:
steps:
- script: echo '${{ converttojson(parameters) }}'
- script: echo '${{ converttojson(variables) }}'
```

### Azure Pipelines Debug Adapter

![Demo](https://raw.githubusercontent.com/ChristopherHX/runner.server/main/docs/azure-pipelines/images/demo.gif)

Sample Debugging configuration
`.vscode/launch.json`
```jsonc
{
    "type": "azure-pipelines-vscode-ext",
    "request": "launch",
    "name": "Test Pipeline (watch)",
    "program": "${workspaceFolder}/azure-pipeline.yml",
    "repositories": {
        "myrepo@windows": "file:///C:/AzurePipelines/myrepo",
        "myrepo@unix": "file:///AzurePipelines/myrepo",
        "myrepo@github": "vscode-vfs://github/AzurePipelines/myrepo", // Only default branch, url doesn't accept readable ref
        "myrepo@azure": "vscode-vfs://azurerepos/AzurePipelines/myrepo/myrepo" // Only default branch, url doesn't accept readable ref
    },
    "parameters": {
        "booleanparam": true,
        "numberparam": 12,
        "stringparam": "Hello World",
        "objectparam": {
            "booleanparam": true,
            "numberparam": 12,
            "stringparam": "Hello World",
        },
        "arrayparam": [
            true,
            12,
            "Hello World"
        ]
    },
    "variables": {
        "system.debug": "true"
    },
    "watch": true,
    "preview": true
}
```

Sample Pipeline which dumps the parameters object (legacy parameters syntax)
```yaml
parameters:
  booleanparam:
  numberparam:
  stringparam:
  objectparam:
  arrayparam:
steps:
- script: echo '${{ converttojson(parameters) }}'
- script: echo '${{ converttojson(variables) }}'
```

Output of the Sample Pipeline
```yaml
stages:
- stage: 
  jobs:
  - job: 
    steps:
    - task: CmdLine@2
      inputs:
        script: |-
          echo '{
            "booleanparam": true,
            "numberparam": 12,
            "stringparam": "Hello World",
            "objectparam": {
              "booleanparam": true,
              "numberparam": 12,
              "stringparam": "Hello World"
            },
            "arrayparam": [
              true,
              12,
              "Hello World"
            ]
          }'
    - task: CmdLine@2
      inputs:
        script: |-
          echo '{
            "system.debug": "true"
          }'
```

## Pros
- Make changes in multiple dependent template files and show a live preview on save
- Everything is done locally
- You can run template files with the same template engine locally using the Runner.Client tool using the official Azure Pipelines Agent
- Fast feedback
- Less trial and error commits
- You can help by reporting bugs
- It's fully Open Source under the MIT license

## Cons
- May contain different bugs than the Azure Pipelines Service
- You could self-host Azure Devops Server and commit your changes to your local system, may have license implications with more accurate results of the template engine
- May not have feature parity with Azure Pipelines
- Missing predefined Variables, feel free to add them manually as needed

## Available in the VSCode Marketplace

[Azure Pipelines Tools](https://marketplace.visualstudio.com/items?itemName=christopherhx.azure-pipelines-vscode-ext)

## Contributing

I'm happy to review Pull Requests to this repository, including Documentation / Readme updates or suggesting a new icon for the vscode extension.

### Running the Dev Extension

```sh
npm install
dotnet workload install wasm-tools
npm run build
```

- Run vscode target "Run azure-pipelines-vscode-ext Extension" to test it

### Nightly VSIX File
- https://christopherhx.github.io/runner.server/azure-pipelines-vscode-ext/azure-pipelines-vscode-ext.vsix
- https://christopherhx.github.io/runner.server/azure-pipelines-vscode-ext/azure-pipelines-vscode-ext-pre-release.vsix
  - same as first one, but marked as pre-release if you install it

## Changelog

### v0.0.13
- Only ask on the first run of a watch task for required inputs and repositories
- Allow using / passing undeclared parameters using old template syntax
- Resolve null error when assigning an expression to the variables key directly

### v0.0.12 (Prerelease)
- Update errors while typing (no longer required to save the change)
- Improve error reporting
- Validate syntax of runtime expressions before trying to run the final pipeline (`$[ ... ]`)
- Validate dependency tree of jobs before trying to run the final pipeline
- `stages.*.jobs.*.continueOnError` work with runtime expressions
- Resolve Installing the Azure Pipelines Tools Extension in a clean Desktop Session requires restart
  - by incuding a node version of the extension
- Error Highlighting of yaml syntax errors with range information
- Fire ondidclose for non watching tasks to make the task show as completed
- invalid stage and job is not reported as error
- Fix Azure Pipelines Tools won't work without open a folder

### v0.0.11

- Lazy load .net wasm Dependencies as soon as you really use the extension by a command / task or debug adapter
  - Previously it has loaded them as soon as the Extension has been activated
- Moved `> Validate Azure Pipeline` and `> Expand Azure Pipeline` to use auto generated watch Tasks
  - You would have to stop the Task from the Terminal pane to stop watching
- Add Context Menu Entries for `> Validate Azure Pipeline` and `> Expand Azure Pipeline` Commands to the editor of azure-pipelines and YAML files
- Add new Demos

### v0.0.10

- Added azure-pipelines-vscode-ext custom task type
- provide some per file diagonstics to vscode
  - Not all errors have attached file locations, those are not available yet as diagnostics
- replace fileid in errors with filename
- remove stacktrace from errors
- renamed settings and commands to use the extension id as prefix
- added simple extension icon to replace the default placeholder

### v0.0.9

- Fix an error of non boolean if results

### v0.0.8

- Fix an unhandled exception introduced in v0.0.7 while running the debugger
- Fix an error if the preview pane is closed for an extended amount of time

### v0.0.7

- Reopen the pipeline preview if closed, when updating a yaml file
- Fix handling of backslash in paths

### v0.0.6
- Fix handling of required parameters of type number
- Add an Azure Pipelines Debug Adapter for pipeline specfic configurations and watching for file changes