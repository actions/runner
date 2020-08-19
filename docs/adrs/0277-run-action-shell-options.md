# ADR 0277: Run action shell option

**Date** 2019-07-09

**Status** Accepted

## Context
run-actions run scripts using a platform specific shell:
`bash -eo pipefail` on non-windows, and `cmd.exe /c /d /s` on windows

The `shell` option overwrites this to allow different flags or completely different shells/interpreters

A small example is:
```yml
jobs:
  bash-job:
    actions:
    - run: echo "Hello"
      shell: bash
  python-job:
    actions:
    - run: print("Hello")
      shell: python {0}
```

## Decision

___

### Shell option
The keyword being used is `shell` 

`shell` can be either:

1. Builtins / Explicitly supported keywords. It is useful to support at least `cmd`, and `powershell` on Windows. Because `cmd my_cmd_script` and `powershell my_ps1_script` are not valid the same way many Linux/cross-platform interpreters are, e.g. `bash myscript` or `python myscript`. Those tools (and potentially others) also require the correct file extension to run, or must be run in a particular way to get the exit codes consistently, so we must have first class knowledge about them. We provide default templates for these keywords as follows:
    - `cmd`: Default is: `%ComSpec% /D /E:ON /V:OFF /S /C "CALL "{0}""` where the script name is automatically appended with `.cmd` and substituted for `{0}`
        - Note this is equivalent to the default Windows behavior if no shell option is given
    - `pwsh`: Default is: `pwsh -command "& '{0}'"` where the script is automatically appended with `.ps1`
    - `powershell`: Default is: `powershell -command "& '{0}'"` where the script is automatically appended with `.ps1`
    - `bash`: Uses `bash --noprofile --norc -eo pipefail {0}`
        - The default behavior on non-Windows if no shell is given is to attempt this first
    - `sh`: Uses `sh -e {0}`
        - This is the default behavior on non-Windows if no shell is given, AND `bash` (see above) was not located on the PATH
    - `python`: `python {0}`
    - **NOTE**: The exact command ran may vary by machine. We only provide default arguments and command format for the listed shell. While the above behavior is expected on hosted machines, private runners may vary. For example, `sh` (or other commands) may actually be a link to `/bin/dash`, `/bin/bash`, or other

1. A template string: `command [...options] {0} [...more_options]`
    - As above, the file name of the temporary script will be templated in. This gives users more control to have options at any location relative to the script path
    - The first whitespace-delimited word of the string will be interpreted as the command
    - e.g. `python {0} arg1 arg2` or similar can be used if passing args is needed. Some shells will require other options after the filename for various reasons

Note that (1) simply provides defaults that are executed with the same mechanism as (2). That is:
- A temporary script file is generated, and the path to that file is templated into the string at `{0}`
- The first word of the formatted string is assumed to be a command, and we attempt to locate its full path
- The fully qualified path to the command, plus the remaining arguments, is executed
    - e.g. `shell: bash` expands to `/bin/bash --noprofile --norc -eo pipefail /runner/_layout/_work/_temp/f8d4fb2b-19d9-47e6-a786-4cc538d52761.sh` on my private runner

At this time, **THE LIST OF WELL-KNOWN SHELL OPTIONS IS**:
- cmd - Windows (hosted vs2017, vs2019) only
- powershell - Windows (hosted vs2017, vs2019) only
- sh - All hosted platforms
- pwsh - All hosted platforms
- bash - All hosted platforms
- python - All hosted platforms. Can use setup-python to configure which python will be used
___

### Containers
For container jobs, `shell` should just work the same as above, transparently. We will simply `exec` the command in the job container, passing the same arguments in

___

### Exit codes / Error action preference

For builtin shells, we provide defaults that make the most sense for CI, running within Actions, and being executed by our runner

bash/sh:
- Fail-fast behavior using `set -e o pipefail` is the default for `bash` and `shell` builtins, and by default when no option is given on non-Windows platforms
- Users can opt out of fail-fast and take full control easily by providing a template string to the shell options, eg: `bash {0}`.
- sh-like shells exit with the exit code of the last command executed in a script, and is our default behavior. Thus the runner reports the status of the step as fail/succeed based on this exit code

powershell/pwsh
- Fail-fast behavior when possible. For `pwsh` and `powershell` builtins, we will prepend `$ErrorActionPreference = 'stop'` to script contents
- We append `if ((Test-Path -LiteralPath variable:\LASTEXITCODE)) { exit $LASTEXITCODE }` to powershell scripts to get Action statuses to reflect the script's last exit code
- Users can always opt out by not using the builtins, and providing a shell option like: `pwsh -File {0}`, or `powershell -Command "& '{0}'"`, depending on need

cmd
- There doesn't seem to be a way to fully opt in to fail-fast behavior other than writing your script to check each error code and respond accordingly, so we can't actually provide that behavior by default, it will be completely up to the user to write this behavior into their script
- cmd.exe will exit (return the error code to the runner) with the errorlevel of the last program it executed. This is internally consistent with the previous default behavior (sh, pwsh) and is the cmd.exe default, so we keep that behavior

## Consequences
Valid `shell` options will depend on the hosted images. We will need to maintain tight image compat

First class support for a shell will require a major version schema change to modify. We cannot remove or modify the behavior of a well-known supported option, However, adding first class support for new shells is backwards compatible. For instance, we can add a well-known `python` option, because non-well-known options would have always needed to include `{0}`, e.g. `python {0}`
