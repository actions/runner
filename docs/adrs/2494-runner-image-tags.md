# ADR 2494: Runner Image Tags

**Date**: 2023-03-17

**Status**: Accepted<!-- |Accepted|Rejected|Superceded|Deprecated -->

## Context

Following the [adoption of actions-runner-controller by GitHub](https://github.com/actions/actions-runner-controller/discussions/2072) and the introduction of the new runner scale set autoscaling mode, we needed to provide a basic runner image that could be used off the shelf without much friction.

The [current runner image](https://github.com/actions/runner/pkgs/container/actions-runner) is published to GHCR. Each release of this image is tagged with the runner version and the most recent release is also tagged with `latest`.

While the use of `latest` is common practice, we recommend that users pin a specific version of the runner image for a predictable runtime and improved security posture. However, we still notice that a large number of end users are relying on the `latest` tag & raising issues when they encounter problems.

Add to that, the community actions-runner-controller maintainers have issued a [deprecation notice](https://github.com/actions/actions-runner-controller/issues/2056) of the `latest` tag for the existing runner images (https://github.com/orgs/actions-runner-controller/packages).

## Decision

Proceed with Option 2, keeping the `latest` tag and adding the `NOTES.txt` file to our helm charts with the notice.

### Option 1: Remove the `latest` tag

By removing the `latest` tag, we have to proceed with either of these options:

1. Remove the runner image reference in the `values.yaml` provided with the `gha-runner-scale-set` helm chart and mark these fields as required so that users have to explicitly specify a runner image and a specific tag. This will obviously introduce more  friction for users who want to start using actions-runner-controller for the first time.

```yaml
  spec:
    containers:
    - name: runner
      image: ""
      tag: ""
      command: ["/home/runner/run.sh"]
```

1. Pin a specific runner image tag in the `values.yaml` provided with the `gha-runner-scale-set` helm chart. This will reduce friction for users who want to start using actions-runner-controller for the first time but will require us to update the `values.yaml` with every new runner release.

```yaml
  spec:
    containers:
    - name: runner
      image: "ghcr.io/actions/actions-runner"
      tag: "v2.300.0"
      command: ["/home/runner/run.sh"]
```

### Option 2: Keep the `latest` tag

Keeping the `latest` tag is also a reasonable option especially if we don't expect to make any breaking changes to the runner image. We could enhance this by adding a [NOTES.txt](https://helm.sh/docs/chart_template_guide/notes_files/) to the helm chart which will be displayed to the user after a successful helm install/upgrade. This will help users understand the implications of using the `latest` tag and how to pin a specific version of the runner image.

The runner image release workflow will need to be updated so that the image is pushed to GHCR and tagged only when the runner rollout has reached all scale units.

## Consequences

Proceeding with **option 1** means:

1. We will enhance the runtime predictability and security posture of our end users
1. We will have to update the `values.yaml` with every new runner release (that can be automated)
1. We will introduce friction for users who want to start using actions-runner-controller for the first time

Proceeding with **option 2** means:

1. We will have to continue to maintain the `latest` tag
1. We will assume that end users will be able to handle the implications of using the `latest` tag
1. Runner image release workflow needs to be updated 
