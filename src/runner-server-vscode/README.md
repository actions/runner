# Runner.Server for VSCode (Native)

This is experimental

## Usage

Create a `.actrc` file

### No runner with labels found

Append a line like `-P ubuntu-latest=ubuntu:latest` to `.actrc` where `ubuntu-latest` is the set of missing labels in any order joined by `,` and `ubuntu:latest`  is a docker image or `-self-hosted` (redirect labels to default self-hosted labels).

### Define variables

Append a line like `--var-file vars.yml` to `.actrc`

Now just create `vars.yml` like
```yaml
MY_VAR: TESTVAL
MY_MULTI_LINE: |
  Hello
  World
MY_MULTI_LINE2: "Hello\nWorld"
```

### Run workflows / jobs

Click the Codelens over your yaml workflow files to run them.

You find the spawned workflows and jobs in the Explorer ab under WORKFLOW VIEW.

### Nightly VSIX File
- https://christopherhx.github.io/runner.server/runner-server-vscode/runner-server-vscode.vsix
- https://christopherhx.github.io/runner.server/runner-server-vscode/runner-server-vscode-pre-release.vsix
  - same as first one, but marked as pre-release if you install it
