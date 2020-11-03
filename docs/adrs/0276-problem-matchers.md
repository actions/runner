# ADR 0276: Problem Matchers

**Date** 2019-06-05

**Status** Accepted

## Context

Compilation failures during a CI build should surface good error messages.

For example, the actual compile errors from the typescript compiler should bubble as issues in the UI. And not simply "tsc exited with exit code 1".

VSCode has an extensible model for solving this type of problem. VSCode allows users to configure which problems matchers to use, when scanning output. For example, a user can apply the `tsc` problem matcher to receive a rich error output experience in VSCode, when compiling their typescript project.

The problem-matcher concept fits well with "setup" actions. For example, the `setup-nodejs` action will download node.js, add it to the PATH, and register the `tsc` problem matcher. For the duration of the job, the `tsc` problem matcher will be applied against the output.

## Decision

### Registration

#### Using `##` command

`##[add-matcher]path-to-problem-matcher-config.json`

Using a `##` command allows for flexibility:
- Ad hoc scripts can register problem matchers
- Allows problem matchers to be conditionally registered

Note, if a matcher with the same name is registered a second time, it will clobber the first instance.

#### Unregister using `##` command

A way out for rare cases where scoping is a problem.

`##[remove-matcher]owner`

For this to be usable, the `owner` needs to be discoverable. Therefore, debug print the owner on registration.

### Single line matcher

Consider the output:

```
[...]

Build FAILED.

"C:\temp\problemmatcher\myproject\ConsoleApp1\ConsoleApp1.sln" (default target) (1) ->
"C:\temp\problemmatcher\myproject\ConsoleApp1\ConsoleApp1\ConsoleApp1.csproj" (default target) (2) ->
"C:\temp\problemmatcher\myproject\ConsoleApp1\ClassLibrary1\ClassLibrary1.csproj" (default target) (3) ->
(CoreCompile target) -> 
  Class1.cs(16,24): warning CS0612: 'ClassLibrary1.Helpers.MyHelper.Name' is obsolete [C:\temp\problemmatcher\myproject\ConsoleApp1\ClassLibrary1\ClassLibrary1.csproj]


"C:\temp\problemmatcher\myproject\ConsoleApp1\ConsoleApp1.sln" (default target) (1) ->
"C:\temp\problemmatcher\myproject\ConsoleApp1\ConsoleApp1\ConsoleApp1.csproj" (default target) (2) ->
"C:\temp\problemmatcher\myproject\ConsoleApp1\ClassLibrary1\ClassLibrary1.csproj" (default target) (3) ->
(CoreCompile target) -> 
  Helpers\MyHelper.cs(16,30): error CS1002: ; expected [C:\temp\problemmatcher\myproject\ConsoleApp1\ClassLibrary1\ClassLibrary1.csproj]

    1 Warning(s)
    1 Error(s)
```

The below match configuration uses a regular expression to discover problem lines. And the match groups are mapped into issue-properties.

```json
"owner": "msbuild",
"pattern": [
  {
    "regexp": "^\\s*([^:]+)\\((\\d+),(\\d+)\\): (error|warning) ([^:]+): (.*) \\[(.+)\\]$",
    "file": 1,
    "line": 2,
    "column": 3,
    "severity": 4,
    "code": 5,
    "message": 6,
    "fromPath": 7
  }
]
```

The above output and match configuration produces the following matches:

```
line:     Class1.cs(16,24): warning CS0612: 'ClassLibrary1.Helpers.MyHelper.Name' is obsolete [C:\myrepo\myproject\ConsoleApp1\ClassLibrary1\ClassLibrary1.csproj]
file:     Class1.cs
line:     16
column:   24
severity: warning
code:     CS0612
message:  'ClassLibrary1.Helpers.MyHelper.Name' is obsolete
fromPath: C:\myrepo\myproject\ConsoleApp1\ClassLibrary1\ClassLibrary1.csproj
```

```
line:     Helpers\MyHelper.cs(16,30): error CS1002: ; expected [C:\myrepo\myproject\ConsoleApp1\ClassLibrary1\ClassLibrary1.csproj]
file:     Helpers\MyHelper.cs
line:     16
column:   30
severity: error
code:     CS1002
message:  ; expected
fromPath: C:\myrepo\myproject\ConsoleApp1\ClassLibrary1\ClassLibrary1.csproj
```

Additionally the line will appear red in the web UI (prefix with `##[error]`).

Note, an error does not imply task failure. Exit codes communicate failure.

Note, strip color codes when evaluating regular expressions.

### Multi-line matcher

Consider the below output from ESLint in stylish mode. The file name is printed once, yet multiple error lines are printed.

```
test.js
  1:0   error  Missing "use strict" statement                 strict
  5:10  error  'addOne' is defined but never used             no-unused-vars
✖ 2 problems (2 errors, 0 warnings)
```

The below match configuration uses multiple regular expressions, for the multiple lines.

And the last pattern of a multiline matcher can specify the `loop` property. This allows multiple errors to be discovered.

```json
"owner": "eslint-stylish",
"pattern": [
  {
    "regexp": "^([^\\s].*)$",
    "file": 1
  },
  {
    "regexp": "^\\s+(\\d+):(\\d+)\\s+(error|warning|info)\\s+(.*)\\s\\s+(.*)$",
    "line": 1,
    "column": 2,
    "severity": 3,
    "message": 4,
    "code": 5,
    "loop": true
  }
]
```

The above output and match configuration produces two matches:

```
line:     1:0   error  Missing "use strict" statement                 strict
file:     test.js
line:     1
column:   0
severity: error
message:  Missing "use strict" statement
code:     strict
```

```
line:     5:10  error  'addOne' is defined but never used             no-unused-vars
file:     test.js
line:     5
column:   10
severity: error
message:  'addOne' is defined but never used
code:     no-unused-vars
```

Note, in the above example only the error line will appear red in the web UI. The \"file\" line will not appear red.

### Other details

#### Configuration `owner`

Can be used to stomp over or remove.

#### Rooting the file

The goal of the file information is to provide a hyperlink in the UI.

Solving this problem means:
- Rooting the file when unrooted:
  - Use the `fromPath` if specified (assume file path)
  - Use the `github.workspace` (where the repo is cloned on disk)
- Match against a repository to determine the relative path within the repo

This is a place where we diverge from VSCode. VSCode task configurations are specific to the local workspace (workspace root is known or can be specified). We're solving a more generic problem, so we need more information - specifically the `fromPath` property - in order to accurately root the path.

In order to avoid creating inaccurate hyperlinks on the error issues, the agent will verify the file exists and is in the main repository. Otherwise omit the file property from the error issue and debug trace what happened.

#### Supported severity levels

Ordinal ignore case:

- `warning`
- `error`

Coalesce empty with \"error\". For any other values, omit logging an issue and debug trace what happened.

#### Default severity level

Problem matchers are unable to interpret severity strings other than `warning` and `error`. The `severity` match group expects `warning` or `error` (case insensitive).

However some tools indicate error/warning in different ways. For example `flake8` uses codes like `E100`, `W200`, and `F300` (error, warning, fatal, respectively).

Therefore, allow a property `severity`, sibling to `owner`, which identifies the default severity for the problem matcher. This allows two problem matchers to be registered - one for warnings and one for errors.

For example, given the following `flake8` output:

```
./bootcamp/settings.py:156:80: E501 line too long (94 > 79 characters)
./bootcamp/settings.py:165:5: F403 'from local_settings import *' used; unable to detect undefined names
```

Two problem matchers can be used:

```json
{
    "problemMatcher": [
        {
            "owner": "flake8",
            "pattern": [
                {
                    "regexp": "^(.+):(\\d+):(\\d+): ([EF]\\d+) (.+)$",
                    "file": 1,
                    "line": 2,
                    "column": 3,
                    "code": 4,
                    "message": 5
                }
            ]
        },
        {
            "owner": "flake8-warnings",
            "severity": "warning",
            "pattern": [
                {
                    "regexp": "^(.+):(\\d+):(\\d+): (W\\d+) (.+)$",
                    "file": 1,
                    "line": 2,
                    "column": 3,
                    "code": 4,
                    "message": 5
                }
            ]
        }
    ]
}
```

#### Mitigate regular expression denial of service (ReDos)

If a matcher exceeds a 1 second timeout when processing a line, retry up to two three times total.
After three unsuccessful attempts, warn and eject the matcher. The matcher will not run again for the duration of the job.

### Where we diverge from VSCode

- We added the `fromPath` concept for rooting paths. This is done differently in VSCode, since a task is the scope (root path well known). For us, the job is the scope.
- VSCode allows additional activation info background tasks that are always running (recompile on files changed). They allow regular expressions to define when the matcher scope begins and ends. This is an interesting concept that we could leverage to help solve our scoping problem.

## Consequences

- Setup actions should register problem matchers
