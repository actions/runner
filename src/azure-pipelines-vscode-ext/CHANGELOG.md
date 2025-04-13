### v0.3.1 (Preview)
- fix -q flag had no space between cli command and hypen
- rename command of v0.3.1 to `Copy Azure Pipeline Linter Task as Cli Command for use in CI or debugging`
- use common method to merge settings and input parameters

### v0.3.0 (Preview)
- fix auto complete regression for block mappings
- add auto completion tests for debugging purposes
- fix error message of Unrecognized function
- add `Copy Linter Task as Cli Command for use in CI or debugging` Command to get a Runner.Client CLI style command
- add `create-task-log-files` Setting to create an Output Log file in Outputs for new Tasks e.g. handy for Issue reports or debugging
- save new Tasks after providing inputs
- support stringList parameter type
- prefer multiple choice and plain text prompt over a YAML string input
  - Symbols like `*`, `&`, `'` and `"` are no longer interpreted 

### v0.2.13
- add more tests
- fix syntax error not reported outside of if
- prefer expression syntax error over named value errors
  - those are reported in not taken branches

### v0.2.12 (Preview)
- fix broken parameter passing in variable template

### v0.2.11 (Preview)
- add iif and trim docs
  - Now auto complete and hover should show correct content
- fix links to docs and variables text
- hide directive auto complete within one (block style)
- Report Expr Syntax Errors as Fatal again in if / each body
- clear errors if disable-auto-syntax-check is on

### v0.2.10
- Fix syntax validation showing old errors
  - Resolves an unhandled error while the extension want to dismiss the old error messages

### v0.2.9
- Fix file picker of v0.2.8 to be usable
- Fix variables in template parameter showing incorrect errors
- Fix variables overridden in nested scopes showing incorrect errors

### v0.2.8
- Suppress mapping schema errors while parsing if / each expressions
  - Show errors after evaluating the expressions
- Show errors of used if / each branches again
- Support native folder path in settings / prompt
- Allow to show a folder picker if pressing the folder icon button
- Ask to save the changed repositories
- Disable errors for autocomplete, semantic tokens and hover
- Add support of new sparse checkout fields
- Add support for iif and trim functions

### v0.2.7 (Preview)
- Show errors of used if / each branches again
- Support native folder path in settings / prompt
- Allow to show a folder picker if leaving the path empty
- Ask to save the changed repositories
- Disable errors for autocomplete, semantic tokens and hover

### v0.2.6 (Preview)
- Includes experimental bug fixes for dynamic each expressions https://github.com/ChristopherHX/runner.server/issues/278 and https://github.com/ChristopherHX/runner.server/issues/514

### v0.2.5 (Preview)
- speed up sematic highlighting, autocomplete and hover by patching YamlDotnet a bit

### v0.2.4
- release fixes of last preview

### v0.2.3 (Preview)
- fix degraded performance due to disabled new features added in the v0.2.x extension 
- fix Check Manually for Syntax Errors

### v0.2.2
- fix an issue that could cause the v0.2.x extension to hang forever

### v0.2.1
- fix loading web extension on vscode.dev
  - .net8.0 update produced pdb files that are banned, this change removes them from boot config

### v0.2.0
- experimental **slow** auto complete (opt int via azure-pipelines-vscode-ext.enable-auto-complete)
  - the upcoming Runner.Server extension would be faster by using the native dotnet builds via a language server
- experimental semantic highlighting of template and runtime expressions (opt in via azure-pipelines-vscode-ext.enable-semantic-highlighting)
  - currently could slow down syntax checking, this task and the syntax check task needs to be merged to provide both results via one round trip

### v0.1.2
- syntax check template parameter types for variable templates as well

### v0.1.1
- use proper schema for extendsTemplates
- allow validate/extend azure pipelines to pass with a warning on nested templates (regression v0.1.0)
- throttle parallel syntax check tasks while typing
- statically check typed parameters passed to templates, when they can be found
- full support of isSkippable

### v0.1.0
- validate steps pipeline runtime expressions of container and continueOnError
- support subtemplate syntax checks
- see preview changelog for details, a lot has changed since v0.0.17

### v0.0.23 (Preview)
- check for syntax errors by default as soon as the current file is detected as a pipeline
  - file type azure-pipelines is always checked
  - file type yaml once it is valid yaml syntax and contains some azure pipelines structure
  - can be disabled in settings

### v0.0.22 (Preview)
- add lost string value support for task steps like continueOnError, retryCount and timeoutInMinutes

### v0.0.21 (Preview)
- Add lost context access for pipeline.extends of v0.0.20

### v0.0.20 (Preview)
- Use a full azure pipelines schema generated from the official extension / yamlschema service

### v0.0.19 (Preview)
- Fix problem that caused the webworker to crash while using validate Syntax without having errors

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
