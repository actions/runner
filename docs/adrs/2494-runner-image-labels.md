# ADR 2494: Runner Image Tags & Labels

**Date**: 2023-03-17

**Status**: Proposed<!-- |Accepted|Rejected|Superceded|Deprecated -->

## Context

Following the [adoption of actions-runner-controller by GitHub](https://github.com/actions/actions-runner-controller/discussions/2072) and the introduction of the new runner scale set autoscaling mode, we needed to provide a basic runner image that could be used off the shelf without much friction.

The [current runner image](https://github.com/actions/runner/pkgs/container/actions-runner) is published to GHCR. Each release of this image is tagged with the runner version and the most recent release is also tagged with `latest`.

While the use of `latest` is common, we recommend that users pin to a specific version of the runner image for a predictable runtime and improved security posture. However, we still notice that a large number of end users are relying on the `latest` tag & raising issues when they encounter problems.

The removal of the `latest` tag is not easy because the [helm charts need to reference a tag](https://github.com/actions/actions-runner-controller/blob/master/charts/gha-runner-scale-set/values.yaml#L161) and it's unreasonable to release a new helm chart with every runner release.

Add to that, the community actions-runner-controller maintainers have issued a [deprecation notice](https://github.com/actions/actions-runner-controller/issues/2056) of the `latest` tag for the existing runner images (https://github.com/orgs/actions-runner-controller/packages)

## Decision

_**What** is the change being proposed? **How** will it be implemented?_

## Consequences

_What becomes easier or more difficult to do because of this change?_