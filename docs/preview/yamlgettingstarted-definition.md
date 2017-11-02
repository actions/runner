# YAML getting started - Create a definition (internal only, public preview soon)

## When using a Git repo in your VSTS team project

Push a YAML file `.vsts-ci.yml` to the default branch (typically master) in the root
of your repository. A definition `<REPO_NAME>/<REPO_NAME> CI` will be created with a
continuous integration trigger for the default branch.

Note, the definition will only be created if whoever pushes the branch update has
permission to create a definition.

Alternatively a new YAML definition can be created from the web UI. You can choose
a YAML file located anywhere within your repo.

## When using a Git repo in GitHub

A new YAML definition can be created from the web UI. You can choose a YAML file
located anywhere within your repo.

## Hello world

```yaml
queue: Hosted VS2017
steps:
- script: echo hello world
```

## Authorization details

For details about authorization, refer [here](yamlgettingstarted-authz.md).
