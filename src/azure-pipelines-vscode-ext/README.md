# Azure Pipelines VSCode Extension

This is a minimal Azure Pipelines Extension, the first vscode Extension which can Validate and Expand Azure Pipeline YAML files locally without any REST service.

![Demo](https://github.com/ChristopherHX/runner.server/blob/main/docs/azure-pipelines/images/demo.gif?raw=true)

## Features

### Remote Template References

The `azure-pipelines.repositories` settings maps the external Repositories to local or remote folders.

Syntax `[<owner>/]<repo>@<ref>=<uri>` per line. `<uri>` can be formed like `file:///<folder>` (raw file paths are not supported (yet?)), `vscode-vfs://github/<owner>/<repository>` and `vscode-vfs://azurerepos/<owner>/<project>/<repository>`

### Validate Azure Pipeline

`> Validate Azure Pipeline`

This command tries to evaluate your current open Azure Pipeline including templates and notifies you about the result.

_Once this extension has been activated by any command, you can validate your pipeline via a statusbar button with the same name on all yaml or azure-pipelines documents_

### Expand Azure Pipeline

`> Expand Azure Pipeline`

This command tries to evaluate your current open Azure Pipeline including templates and show the result in a new document, which you can save or validate via the official api.

### Azure Pipelines Debug Adapter

Sample Debugging configuration
```jsonc
{
    "type": "azure-pipelines-vscode-ext",
    "request": "launch",
    "name": "Test Pipeline (watch)",
    "program": "${workspaceFolder}/azure-pipeline.yml",
    "repositories": {},
    "parameters": {},
    "variables": {},
    "watch": true,
    "preview": true
}
```

## Pros
- Make changes in multiple dependent template files and show a live preview on save
- Everything is done locally
- Whole template engine is open source
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

## Running the Extension

```sh
npm install
dotnet workload install wasm-tools
npm run build
```

- Run vscode target "Run azure-pipelines-vscode-ext Extension" to test it