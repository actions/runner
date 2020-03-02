# ADR 354: Expose runner machine info

**Date**: 2020-03-02

**Status**: Pending

## Context

- Provide an mechanism in the runner to include extra information in `Set up job` step's log.
  Ex: Include OS/Software info from Hosted image.

## Decision

The runner will looking for a file `.extra_setup_info` under runner's root directory, the file could be a JSON with a simple schema.
```json
[
  {
    "group": "OS Detail",
    "detail": "........"
  },
  {
    "group": "Software Detail",
    "detail": "........"
  }
]
```
The runner will use `##[group]` and `##[endgroup]` to fold all detail info into an expandable group.

Both [virtual-environments](https://github.com/actions/virtual-environments) and self-hosted runner can leverage this mechanism to add extra logging info to `Set up job` step's log.

## Consequences

1. Change the runner to best effort read/parse `.extra_setup_info` file under runner root directory.
2. [virtual-environments](https://github.com/actions/virtual-environments) generate the file during image generation.
3. Change MMS provisioner to properly copy the file to runner root directory at runtime.
