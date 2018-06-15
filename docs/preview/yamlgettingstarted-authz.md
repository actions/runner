# YAML getting started - Authorization

## Create definition on push

The ability to create a definition on push, is currently only supported for Git repositories in VSTS.

The build definition will be created in the same VSTS team project. Whoever pushes the commit must
be authorized to create a definition.

For details about creating a definition on-push, refer [here](yamlgettingstarted-definition.md).

## Resource authorization (queues, endpoints, secure files)

When a definition is created or updated, any resources referenced by the YAML file are authorized
for use. Authorization is performed based whether the person creating or updating the definition
has use permission for the resource.

The YAML file in the default branch (specified on the definition) is used to discover resources
when saving the definition using the web UI or REST API.

## Resource authorization on push

The ability to authorize resources on push, is currently only supported for Git repos in VSTS.

This feature is only supported for pushes to the file `.vsts-ci.yml` in the root of the repo.
A push to the file will cause the definition to be updated. When the YAML file is changed, the
person who pushed the branch update is considered to be the person updating the definition
(regardless of commit author).

Note, resource authorization is currently append only. Removing a resource from the YAML
file will not remove it's authorized status. A future update will provide a way to
remove authorization for a resource from the web UI or REST API.

## Having trouble with resource authorization?

We have a better experience in the web coming in the next couple sprints.

A workaround that covers most cases is, navigate to the build definition in the web, switch the
default branch to your branch that includes the service endpoint reference, save, revert back to
your desired settings, save again.  The act of saving the definition loads the file (from the
default branch) and authorizes discovered resources.
