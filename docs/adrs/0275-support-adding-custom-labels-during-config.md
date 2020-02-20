# ADR 275: Support adding custom labels during runner config
**Date**: 2020-01-14

**Status**: Proposed

## Context

In the case of a single repo having multiple self-hosted runners, actions do not currently support any way of selecting which runner or kind of runner a job should run on, other than by architecture or OS.

Example use cases for this could be:

* Your project involves some machine learning models. You want to add a second runner with a GPU for testing your models, and to have your model tests job use this runner, but other jobs to use the runner without a GPU. 
* You want to use GitHub actions to deploy your app, and for security you use a self-hosted runner inside your isolated network. If you have a separate isolated network for each environment (e.g. testing, staging, production), you would need to run your deploy job on a different runner depending on which environment you are deploying to.
* Any other situation where you would like to select which self-hosted runner to use for a job based on the capabilities or characteristics of the runner.

GitHub actions already support selecting a job runner based on labels, and automatically give runners labels based on OS and arch, so a natural extension would be to allow operators of self-hosted runners to add their own labels, so that users can select job runners based on any operator-defined characteristic.

See Issue: https://github.com/actions/runner/issues/262

## Decision

This ADR proposes that we add a `--label` option to `config.sh`, which could be used to add custom additional labels to the configured runner.

For example, to add a single extra label the operator could run:
```bash
./config.sh --label my-extra-label
```
Which would add the label `my-extra-label` to the runner, and enable users to select the runner in their workflow using this label:
```yaml
runs-on: [self-hosted, my-extra-label]
```

To add multiple labels:
```bash
./config.sh --label my-extra-label --label yet-another-label
```
This would add both labels `my-extra-label` and `yet-another-label`.

## Consequences

The ability to add custom labels to a self-hosted runner would enable most scenarios where job runner selection based on runner capabilities or characteristics are required.

There could be unexpected behavior if an operator adds inappropriate labels, e.g. they add the label `linux` to a Windows runner, but this would be considered operator error.
