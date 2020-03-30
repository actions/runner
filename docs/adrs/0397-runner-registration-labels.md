# ADR 0397: Support adding custom labels during runner config
**Date**: 2020-03-30

**Status**: Proposed

## Context

Since configuring self-hosted runners is commonly automated via scripts, the labels need to be able to be created during configuration.  The runner currently registers the built-in labels (os, arch) during registration but does not accept labels via command line args to extend the set registered.

See Issue: https://github.com/actions/runner/issues/262

This is another version of [ADR275](https://github.com/actions/runner/pull/275)

## Decision

This ADR proposes that we add a `--labels` option to `config`, which could be used to add custom additional labels to the configured runner.

For example, to add a single extra label the operator could run:
```bash
./config.sh --labels mylabel
```
> Note: the current runner command line parsing and envvar override algorithm only supports a single argument (key).

This would add the label `mylabel` to the runner, and enable users to select the runner in their workflow using this label:
```yaml
runs-on: [self-hosted, mylabel]
```

To add multiple labels the operator could run:
```bash
./config.sh --labels mylabel,anotherlabel
```
> Note: the current runner command line parsing and envvar override algorithm only supports a single argument (key).

This would add the label `mylabel` and `anotherlabel` to the runner, and enable users to select the runner in their workflow using this label:
```yaml
runs-on: [self-hosted, mylabel, anotherlabel]
```

It would not be possible to remove labels from an existing runner using `config.sh`, instead labels would have to be removed using the GitHub UI.

## Overriding built-in labels

Note that it is possible to register "built-in" hosted labels like `ubuntu-latest` and is not considered an error.  This is an effective way for the org / runner admin to dictate by policy through registration that this set of runners will be used without having to edit all the workflow files now and in the future.

If a self-hosted runner built-in label such as os or arch, in the case that it is valid (`linux` on a linux os), it will have no effect.  If it's invalid (supplying `windows` on a linux machine) it will error.

## Consequences

The ability to add custom labels to a self-hosted runner would enable most scenarios where job runner selection based on runner capabilities or characteristics are required.
