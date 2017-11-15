# YAML getting started - Authorization

## Create definition on push

The ability to create a definition on-push is currently only supported for Git repos in the
same VSTS team project as the build definition. The ability to create a definition from a push,
requires whoever pushes the commit to have authorization to create a definition.

For details about creating a definition on-push, refer [here](yamlgettingstarted-definition.md).

## Resources

Build definitions commonly refer to resources within VSTS that have security restrictions -
such as queues and endpoints. When a definition is created or updated, any resources
referenced by the YAML file are authorized for use, if the person creating or updating the
definition has use permission.

Definitions can be updated either by pushing a commit to the YAML file (VSTS only) or by
saving the definition using the web UI or REST API (VSTS and GitHub).

Only pushes to the default branch (specified on the definition), will cause the definition
to be updated. When the default branch is updated and the YAML file is changed, the person
who pushed the branch update is considered to be the person updating the definition
(regardless of commit author).

Similarly, the YAML file in the default branch is used to discover resources when saving
the definition using the web UI or REST API.

Note, resource authorization is currently append only. Removing a resource from the YAML
file will not remove it's authorized status. A future update will provide a way to
remove authorization for a resource from the web UI or REST API.
