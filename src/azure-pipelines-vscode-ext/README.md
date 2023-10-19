# Azure Pipelines VSCode Extension

This is a minimal Azure Pipelines Extension

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

## Running the Extension

```sh
npm install
dotnet workload install wasm-tools
npm run build
```

- Run vscode target "Run azure-pipelines-vscode-ext Extension" to test it