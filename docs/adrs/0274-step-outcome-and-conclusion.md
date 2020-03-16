# ADR 0274: Step outcome and conclusion

**Date**: 2020-01-13

**Status**: Accepted

## Context

This ADR proposes adding `steps.<id>.outcome` and `steps.<id>.conclusion` to the steps context.

This allows downstream a step to run based on whether a previous step succeeded or failed.

Reminder, currently the steps contains `steps.<id>.outputs`.

## Decision

For steps that have completed, populate `steps.<id>.outcome` and `steps.<id>.conclusion` with one of the following values:

- `success`
- `failure`
- `cancelled`
- `skipped`

When a continue-on-error step fails, the outcome will be `failure` even though the final conclusion is `success`.

### Example

```yaml
steps:

  - id: experimental
    continue-on-error: true
    run: ./build.sh experimental

  - if: ${{ steps.experimental.outcome == 'success' }}
    run: ./publish.sh experimental
```

### Terminology

The runs API uses the term `conclusion`.

Therefore we use a different term `outcome` for the value prior to continue-on-error.

The following is a snippet from the runs API response payload:

```json
      "steps": [
        {
          "name": "Set up job",
          "status": "completed",
          "conclusion": "success",
          "number": 1,
          "started_at": "2020-01-09T11:06:16.000-05:00",
          "completed_at": "2020-01-09T11:06:18.000-05:00"
        },
```

## Consequences

- Update runner
- Update [docs](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/contexts-and-expression-syntax-for-github-actions#steps-context)