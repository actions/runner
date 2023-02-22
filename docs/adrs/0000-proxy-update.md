# ADR 000: Update Proxy Behavior of Self-Hosted Runners

**Date**: 2023-02-21

**Status**: Pending

## Context

Today, the different user-accessible building blocks of GitHub Actions implement proxy behaviour with significant functional differences.
Users could realistically run all of the below examples and they would reasonably expect that their proxy settings will have the same networking effects across all of them.

A user running `actions/actions-runner-controller`, which starts instances of `actions/runner`, that the runs node.js actions made with `actions/toolkit`
- ARC and its controller and listener pods in k8s will follow `golang` defaults for proxy behaviour
- The `runner` overrides the default proxy behaviour of C# and implements it [explicitly](https://github.com/actions/runner/blob/main/src/Runner.Sdk/RunnerWebProxy.cs), however currently differently from `toolkit`
- `toolkit` overrides the default proxy behaviour of node.js and implements it [explicitly](https://github.com/actions/toolkit/blob/main/packages/http-client/src/proxy.ts), however currently differently from `runner`


## Example 1 - ARC

A user wants to create a scaleset in ARC. They give following settings when creating an ARC Scale Set:
- `https_proxy=https://someproxy.company.com`
- `no_proxy=8.8.8.8,192.168.1.1/32`
The ENV variables are propagated through all actors, but:

- *ARC operators and listener pods* will: follow the proxy but bypass it for `8.8.8.8` and the CIDR block `192.168.1.1/32`
- *The runner* will: use a proxy and ignore these `no_proxy` settings (no IP support in `runner` for `no_proxy`)
- *A node.js GitHub Action in a job executed by the runner* will: bypass `8.8.8.8` but use proxy for the CIDR block `192.168.1.1/32`

## Example 2 - Self-Hosted runner

Given the following settings when creating an ARC Scale Set:
- `https_proxy=someproxy.company.com`

- *The runner* will: silently ignore `https_proxy` value because it doesn't have a protocol (missing `https://`)
- *A node.js GitHub Action in a job executed by the runner* will: throw an exception because proxy could not be parsed (missing `https://`)

## Decisions
## Consequences
