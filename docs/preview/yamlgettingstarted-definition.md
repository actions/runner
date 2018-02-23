# YAML getting started - Create a definition

## When using a Git repo in your VSTS team project

Push a YAML file `.vsts-ci.yml` to the root directory of your repository. A definition
`<REPO_NAME>/<REPO_NAME> CI` will be created, and a CI build will be triggered.

In March when the YAML trigger feature rolls out, newly created definitions will default
to CI builds for all branches. Definitions created prior to that, default to a centrally
managed trigger on the web definition, for the default branch only.

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

## Trigger details

For details about triggers, refer [here](yamlgettingstarted-triggers.md).
