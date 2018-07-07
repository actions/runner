# YAML getting started - Accessing variables from scripts

Pipeline variables are passed along to scripts, as process environment variables.

The variable name is transformed to upper case and the characters \".\" and \" " are replaced with \"_\".

Example for macOS and Linux:

```yaml
queue: Hosted Linux Preview
steps:
- script: |
    cd $AGENT_HOMEDIRECTORY
    ls
```

Example for Windows:

```yaml
queue: Hosted VS2017
steps:
- script: |
    cd %AGENT_HOMEDIRECTORY%
    dir
```

For a full list of variables, you can dump the environment variables from a script.

Example for macOS and Linux:

```yaml
queue: Hosted Linux Preview
steps:
- script: printenv | sort
```

Example for Windows:

```yaml
queue: Hosted VS2017
steps:
- script: set
```
