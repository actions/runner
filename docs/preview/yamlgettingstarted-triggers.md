# YAML getting started - YAML triggers

Continuous integration builds are on by default for all branches.

## Simple CI trigger syntax

A simple list of branches can be specified in the file, to control which branches trigger a CI build.

When updates are pushed to a branch, the YAML file in that branch is used to evaluate the branch filters.

For example, a simple list of inclusive branch filters may look like:

```yaml
trigger:
- master
- releases/*
```

## Full CI trigger syntax

For more control, an alternative trigger syntax is available:

```yaml
trigger:
  branches:
    include: [string]
    exclude: [string]
  paths:
    include: [string]
    exclude: [string]
```

For example:

```yaml
trigger:
  branches:
    include:
    - master
    - releases/*
    exclude:
    - releases/old*
```


Note, path filters are only supported for Git repositories in VSTS.

## CI is opt-out

Continuous integration builds can be turned off by specifying `trigger: none`

Optionally, the triggers can be managed from the web definition editor, on the Triggers tab.
