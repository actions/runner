### v0.0.18 (Preview)
- Fix problem loading the Extension in VSCode Web Editor due to blocked `.dat` extension
- Provide a Standalone Syntax Checker Mode, that does not depend on variables / parameters

### v0.0.17
- Update Schema (#361)
- add correct string representation of expr results (#359)

### v0.0.16
- fix steps.*.continueOnError / allowed boolean values 
- make condition stricter
  - no longer accept `$[ ]` for conditions
  - check step conditions
- fix invalid parameter passing to legacy templates using a parameter mapping or omit it
  - all scalars should be strings like for job/stage templateContext

### v0.0.15
- fix variable templates using expressions in it's parameters
- fix empty dependsOn e.g. empty string is not a missing dependency

### v0.0.14
- Variables defined in referenced templates are now available for template expansion
- Fixed each and if expressions broke resources.containers validation
- [dotnet/runtime/eng/pipelines/runtime.yml](https://github.com/dotnet/runtime/blob/d79bb01433401a144816a386c5411ac4c08b6187/eng/pipelines/runtime.yml) can now be expanded without generating an invalid pipeline
- Fixed relative paths didn't work for variable templates

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
