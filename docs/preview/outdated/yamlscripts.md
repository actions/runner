# VSTS YAML simplified script syntax

The goal of this document is to define a simplified syntax for running scripts and command lines. Running a script should not require the formal declaration `- task: name@version, inputs...`.

## Proposed well-known tasks

- `script`
- `bash`
- `powershell`

## `script`

```yaml
- script: inline script goes here
  workingDirectory: $(system.defaultWorkingDirectory)
  failOnStderr: false
  env:
    name1: value1
    name2: value2
```

`script` inline script content. Uses cmd.exe on Windows, and sh on Linux.

`workingDirectory` defaults to `$(system.defaultWorkingDirectory)`.

`failOnStderr` defaults to `false`.

### Works on all OS

It is crucial that we have a well-known task (i.e. `script`) that works on both Windows and Linux.
Many popular CI tools work on Windows and Linux. For example: git, node, npm, tfx.

### Generate a temp script

We will always generate a temp script with the inline content - shell script on Linux and a .cmd
file on Windows.

On Windows, it is important to be aware that cmd.exe has two interpretation modes. Slightly different
rules are applied by the command line interpreter versus the script interpreter.

On a cmd.exe command line, `%` cannot be escaped. Whereas within a script it can be escaped by doubling
the character - e.g. `echo this %%is%% escaped`

Furthermore, on the command line non-existing variables are not replaced. Whereas in a script non-existing
variables are replaced with empty.

Example 1:
<br />In an interactive shell, `echo hello%20world` outputs `hello%20world`.
<br />In a script, `echo hello%20world` outputs `hello0world` (assuming arg 2 is not specified).

Example 2:
<br />In an interactive shell, `echo hello %nosuch% var` outputs `hello %nosuch% var`.
<br />In a script, `echo hello %nosuch% var` outputs `hello  var`.

A way customers might run into problems with the script interpreter, would be if they leverage VSTS
macros to inline a job variable into their script. The customer would not be able to escape the value,
since they are referencing the value indirectly rather than hardcoding the value. Likely places where
a customer would have a `%` in a variable, would be either in a password (secret variable) or a URL-encoded
data. Note, password should be mapped in via env anyway. And only a subset of URLs contain URL-encoded
data. So this limitation is probably OK.

The ability to specify an `env` mapping becomes crucial for Windows in order to workaround escaping
problems.

Furthermore, one additional limitation follows. Since the syntax for referencing environment variables
is different across Windows and Linux, script re-usability across platforms becomes somewhat more narrow.
In practice this may not present a significant problem.

Lastly, one additional concern remains. Since the command-line-interpreter versus script-interpreter
difference is subtle, we may run into customer confusion - "works on the command line". An alternative
approach would be to name the well-known task by the more generic name `command` and offer users additional
control whether to generate a script, or possibly even whether to exec the process directly instead of via
the shell. The alternative has drawbacks of it's own (more complicated controls, different on Windows).
Therefore, we think simply always generating a script is the better approach - and the name will be `script`
so it should be clear the task is generating a script.

### cmd.exe command line options

Wrap command with `"<FULL_PATH_TO_CMD.EXE>" /Q /D /E:ON /V:OFF /S /C "<TODO>"`
- `/Q` Turns echo off.
  - Not sure whether we should set /Q.
    <br />
    <br />
    Reasons to set:
    <br />A) Consistency with Linux.
    <br />B) Folks commonly turn off echo in their own scripts. More often than not?
    <br />
    <br />
    Reasons to not set:
    <br />A) When generating a temp script, script contents will not otherwise get traced to the build output log.
    <br />
    <br />
    Another option would be to set /Q and the handler can dump the script contents to the build output log. Yet another option would be to dump to build debug log.
- `/D` Disable execution of AutoRun commands from registry
  - The motivation is to prevent accidental interference. Disabling autorun commands should
    be OK since it doesn't make sense for a CI build to depend on the presence of auto-run commands (bizarre coupling).
- `/E:ON` Enable command extensions
  - Command extensions are enabled by default, unless disabled via registry.
- `/V:OFF` Disable delayed environment expansion.
  - Delayed environment expansion is disabled by default, unless enabled via registry.
- `/S` will cause first and last quote after /C to be stripped

### bash command line options

TODO

### Other considerations

- For scenario when generating a script on Windows, revisit details from previous conversation with Philip regarding
  bubbling error level. IIRC the feedback was to consider CALL and checking %ERRORLEVEL% at the end if we generate a script.
  This has to do with error level from nested calls not bubbling as exit code of the process. Which is inconsitent
  wrt exit codes from externals processes. Note, this also could be related to the difference between .cmd and .bat files... investigate.
- Do we need a way to specify script should create a .bat file instead of .cmd? Seems unlikely. However, could be accomplished
  by allowing script:bat/cmd instead of simply true/false.
- Do we need a way to allow the user to influence the command line args to cmd.exe? For instance, to set one of the
  encoding switches:
  <br />
  `/A` Causes the output of internal commands to a pipe or file to be ANSI
  <br />
  `/U` Causes the output of internal commands to a pipe or file to be Unicode
  <br />
  <br />
  Otherwise we could consider a special property for these particular settings.
- Need option to invoke `dash` on Linux? Or always prefer `dash`? It seems unlikely that the small increase in startup
  perf would matter for our scenario.
- Consider a property `runInShell` (defaults to true) to offer a simple way out of shell escaping challenges. Motivation
  is similar to `verbatim` functionality in task lib. Furthermore, would that mean Linux users should be able to specify argv rather
  than forced to specify the full line?
- Do we need a way to influence how the agent interprets stdout encoding?
- Will "script" commonly mislead customers to think they can specify a powershell script file path followed by
  args? Alternative would be something like "command" verbiage.
- Due to tools commonly writing to stderr, generate a .cmd script on Windows instead of .ps1. From the parent process, we can't distinguish write-error from a downstream process writing non-error text to stderr (downstream processes inherit streams by default). Therefore, if we generated a .ps1 instead, customers would expect write-error to fail by default which we can't do, due to the stderr problem. Alternative would be to set the error action preference to stop, but that doesn't make sense for the "script" task since it runs bash on Linux.

## `bash`

```yaml
- bash: inline script
  workingDirectory: $(system.defaultWorkingDirectory)
  failOnStderr: false
```

`bash` runs inline script using bash from the PATH

`workingDirectory` defaults to `$(system.defaultWorkingDirectory)`.

`ignoreExitCode` defaults to `false`.

`failOnStderr` defaults to `false`.

### Notes

Always gens a script in agent.tempdirectory.

Specify noprofile, etc...

Works on Windows too if bash is in the PATH. Check other well-known locations for sh.exe?

Does +x need to be set? Will this "just work" for scripts in the repo?

## `powershell`

```yaml
- powershell: inline script
  workingDirectory: $(system.defaultWorkingDirectory)
  errorActionPreference: stop
  failOnStderr: false
```

`powershell` runs inline script using powershell from the PATH or well-known location.

`workingDirectory` defaults to `$(system.defaultWorkingDirectory)`.

`errorActionPreference` defaults to `stop`.

`ignoreLASTEXITCODE` defaults to `false`.

`failOnStderr` defaults to `false`.

### Notes

- Try PATH, fallback to full desktop.
- Always gens a script in agent.tempdirectory.
- Specify noprofile, etc...
- Should we add `ignoreLASTEXITCODE: false`?
  <br />
  <br />
  Reasons to add:
  <br />A) Consider scenario where the customer runs msbuild and it returns 1. Doesn't bubble as it naturally does in cmd.exe.
  <br />
  <br />
  Reasons to not add:
  <br />A) Different from powershell behavior.
  <br />B) Exit or return or break or continue are
  strange... can prevent checking $LASTEXITCODE... investigate.

## Limitations of combining tool+args into a single input

The proposed well-known tasks above take the full command line as one input. The proposed pattern
differs from existing tasks. Today the existing tasks all specify two inputs - i.e. an
input for tool or script-path and a separate input for args. Furthermore, existing script tasks (Batch/Shell/PowerShell) use `filePath` inputs to specify the script-path. Multiple implications follow from
the proposed pattern change; specific scenarios are discussed further below.

For reference, see followng summary of relevant command/script task inputs today:
- Command Line
  - (string) Tool
  - (string) Args
- Batch Script
  - (filePath) Script
  - (string) Args
- Shell Script
  - (filePath) Script
  - (string) Args
- PowerShell Script
  - (filePath) Script
  - (string) Args

### Limitations for `command`

For the proposed well-known task "command", combining tool+args into a single input doesn't impose
much limitation.

Note, the "Command Line" task today uses string for the Tool input. Since a goal of the task is to
enable running shell built-in commands, filePath cannot be used.

The only limitation imposed by combining tool+args into a single input, is the definition author
will be responsible for accurately quoting the "tool" portion of the command line.

### Limitations for `bash`/`powershell` using primary Git repo

This analysis applies to the proposed well-known tasks `bash`/`powershell` under two scenarios:
1. Today, when the build is using a Git repo, and the script is in the repo.
2. In the future when sync'ing multiple repos is supported, the script is in the "primary repo",
   and the primary repo is a Git repo.

The existing script tasks use a filePath input type. So a relative path to a script in a Git repo today,
is rooted against the repo directory. For example, `foo.sh` is rooted as `$(system.defaultWorkingDirectory)/foo.sh`.

By combining script+args into a single input, some limitations are imposed:
1. With a single combined script+args input, the shell will resolve relative script paths against the
   working directory. The working directory will be defaulted to `system.defaultWorkingDirectory` so
   relative paths will often work the same.
   <br />
   <br />
   However, bash and powershell require at least one slash in unrooted script paths. This is a security measure
   to prevent a file in the current directory from hijacking a command in the PATH. So
   `foo.sh` will not work, but `./foo.sh` and `subdir/foo.sh` will. (TODO: CONFIRM subdir/foo.sh WORKS IN BASH)  

2. Relative script paths now tied to working directory input. (Limitation in some scenarios, advantage in others).

3. The definition author will be responsible for accurately quoting the "script" portion of the command line.

### Limitations for `bash`/`powershell` using secondary Git repo

In anticipation of multiple repos, we have discussed the idea of bringing additional functionality
to filePath inputs. One idea is to introduce an elegant syntax `path/to/file@repo` to resolve a path
against a secondary repo.

By combining script+args into a single input, we lose the ability to leverage the future elegant
syntax. The future elegant syntax will only work with filePath inputs.

Two options exist for specifying scripts in secondary Git repos:
1. Root the script against the secondary repo directory. For example, `$(repos.myFancyRepo.directory)/foo.sh`
2. Set the workingDirectory so the shell will resolve the script path. For example,
```yaml
- bash: ./foo.sh
  workingDirectory: $(repos.myFancyRepo.directory)
```

### Limitations for `bash`/`powershell` using TFVC/SVN repo

Server-path to local-path resolution is complicated for TFVC/SVN due to mappings. For TFVC, the agent
calls `tf resolvePath` to map filePath inputs.

By combining script+args into a single input, we lose the ability to leverage functionality
of filePath inputs to deal with the problem.

However, we can solve the problem by adding support for inline expressions. For example, something like:
```yaml
- bash: $(=resolvePath('myFancyRepo', '$/teamProject/subdir/foo.sh'))
```
