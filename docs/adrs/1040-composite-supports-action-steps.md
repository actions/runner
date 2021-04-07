**Date**: 2021-04-06

**Status**: Accepted

## Context

We will add support for actions to be referenced, with composite actions.

## Decision

TODO:
  - Decide recursion limit

## Consequences

TODO: Move these details into a tracking issue instead

- Launch
  - New feature flag
  - Move Action policy enforcement from workflow run, to resolve actions endpoint 
- Runner
  - New feature flag
  - Download all nested actions
    - Decide recursion limit
    - src/Runner.Worker/ActionManager.cs
    - Precursor: Remove feature flag DistributedTask.NewActionMetadata
  - Update schema to support `uses` within a composite step
    - src/Runner.Worker/action_yaml.json
    - src/Sdk/DTPipelines/Pipelines/ObjectTemplating/PipelineTemplateConverter.cs
    - Mimic server validation, generate IDs, etc
  - Handle inputs/outputs between nested layers
    - Update server to generate context names when empty
  - Fix issue with `hashFiles` within a composite action
    - https://github.com/actions/runner/issues/991
  - Support container-actions and actions with pre-step and post-step
    - Investigate how to deal with pre-steps. Today, nested steps are created just-in-time. However, pre-steps are generally executed before regular steps. Container actions implicitly have a pre-step to pull or build the image
    - Investigate whether we copy the continue-on-error setting to the pre/post steps
    - Investigate CompositeActionHandler.cs especially wrt clearing the output scopes
    - Investigate whether we need additional validation to restrict contexts available to pre-step condition and post-step condition
  - Testing
    - Support all types of action manifests: Node.js, Dockerfile, `docker://`, composite, no manifest only Dockerfile
    - Support all types of action references: `docker://`, `./`, owner/repo@ref
    - Support for actions with pre-step and post-step