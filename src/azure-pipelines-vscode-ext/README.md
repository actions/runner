# Azure Pipelines Tools for VSCode

The first VSCode Extension which can Validate and Expand Azure Pipeline YAML files locally without any REST service.

#### Live syntax checking for pipelines
![Live syntax checking for pipelines](https://raw.githubusercontent.com/wiki/ChristopherHX/runner.server/live-syntax-check.gif)

#### Live preview of the expanded / rendered pipeline
![Live preview of the expanded / rendered pipeline](https://raw.githubusercontent.com/wiki/ChristopherHX/runner.server/live-preview.gif)

#### Experimental autocompletion

**slow ( disabled by default )**

You can try this out by enabling setting `azure-pipelines-vscode-ext.enable-auto-complete` (no restart needed), feedback wanted.

![experimental autocompletion](https://raw.githubusercontent.com/wiki/ChristopherHX/runner.server/auto-completion.gif)

#### Experimental semantic highlighting of template and runtime expressions

**potentially slow ( disabled by default )**

You can try this out by enabling setting `azure-pipelines-vscode-ext.enable-semantic-highlighting` (no restart needed), feedback wanted.

![experimental highlighting](https://raw.githubusercontent.com/wiki/ChristopherHX/runner.server/semantic-highlighting.gif)

## Features

### Remote Template References

The `azure-pipelines-vscode-ext.repositories` settings maps the external Repositories to local or remote folders.

Syntax `[<owner>/]<repo>@<ref>=<uri>` per line. `<uri>` can be formed like `file:///<folder>`, native folder path (since v0.2.7), `vscode-vfs://github/<owner>/<repository>` and `vscode-vfs://azurerepos/<owner>/<project>/<repository>`

### Check Syntax Azure Pipeline

`> Check Syntax Azure Pipeline`

This command explicitly checks for syntax errors in the yaml structure and expression syntaxes.

Referenced templates are read to apply the correct schema for their parameters, if this fails this check is disabled.

These are necessary but not sufficient checks for a successful Validation of an Azure Pipeline.

### Validate Azure Pipeline

`> Validate Azure Pipeline`

**Use this command on your entrypoint Pipeline, otherwise parameters may be missing**

This command tries to evaluate your current open Azure Pipeline including templates and notifies you about the result.

### Expand Azure Pipeline

`> Expand Azure Pipeline`

**Use this command on your entrypoint Pipeline, otherwise parameters may be missing**

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
Sample Pipeline which dumps the parameters object
```yaml
parameters:
- name: booleanparam
  type: boolean
- name: numberparam
  type: number
- name: stringparam
  type: string
- name: objectparam
  type: object
- name: arrayparam
  type: object
steps:
- script: echo '${{ converttojson(parameters) }}'
- script: echo '${{ converttojson(variables) }}'
```

Sample output for task with label test

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
            "myvar": "testx",
            "system.debug": "true"
          }'
```

### Run the Linter Task using a custom keybind

Follow https://code.visualstudio.com/docs/configure/keybindings#_advanced-customization then use this as example
```json
[
    {
        "key": "ctrl+cmd+g",
        "command": "workbench.action.tasks.runTask",
        "args": "Expand Pipeline demo2/pipeline.yml"
    }
]
```

`Expand Pipeline demo2/pipeline.yml` is an example label of the task in your `.vscode/tasks.json` file.

## Pro
- Make changes in multiple dependent template files and show a live preview
- Everything is done locally and works offline
- You can run template files with the same template engine locally via the [Runner.Client and Server tool](https://github.com/ChristopherHX/runner.server) using the official Azure Pipelines Agent
  - `Runner.Client azexpand -W azure-pipeline.yml` works like Validate Azure Pipeline by only checking the return value to be zero
  - `Runner.Client azexpand -q -W azure-pipeline.yml > final.yml` works like Expand Azure Pipeline, but directly writes the expanded file to disk
  - `Runner.Client azpipelines -W azure-pipeline.yml` to test Azure Pipelines locally via the official agent
    - `--interactive` and `--watch` allow to iterate even faster by keeping the local server and agent running
- Less trial and error commits
- Works side by side with the official Azure Pipelines VSCode extension

## Contra
- May contain different bugs than the Azure Pipelines Service
- You can self-host Azure Devops Server and commit your changes to your local system with more accurate results of the template engine
- Missing predefined Variables, feel free to add them manually as needed
- Unsupported legacy syntax causes errors
  - phases
  - queue

## Available in both VSCode Marketplace and Open VSX Registry

- [VSCode Marketplace](https://marketplace.visualstudio.com/items?itemName=christopherhx.azure-pipelines-vscode-ext)
- [Open VSX Registry](https://open-vsx.org/extension/christopherhx/azure-pipelines-vscode-ext)

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
