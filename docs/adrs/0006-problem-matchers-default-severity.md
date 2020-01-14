# ADR 0006: Problem matcher default severity

**Date**: 2019-11-12

**Status**: Accepted

## Context

Problem matchers are unable to interpret severity strings other than `warning` and `error`. The `severity` match group expects `warning` or `error` (case insensitive).

However some tools indicate error/warning in different ways. For example `flake8` uses codes like `E100`, `W200`, and `F300` (error, warning, fatal, respectively).

## Decision

Add a property `severity`, sibling to `owner`, which identifies the default severity for the problem matcher.

The proposed solution is consistent with how VSCode solves the problem.

Two problem matchers are registered - one for warnings and one for errors.

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

## Consequences

- Update runner to support the new property
- Update problem matcher docs