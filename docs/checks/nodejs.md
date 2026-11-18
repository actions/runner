# Node.js Connection Check

## What is this check for?

Make sure the built-in node.js has access to GitHub.com or GitHub Enterprise Server.

The runner carries its own Node.js 24 executable under `<runner_root>/externals/node24/`. Node.js 20 is no longer bundled.

All JavaScript actions run on the bundled Node.js 24 runtime. Action metadata using `node12`, `node16`, or `node20` is still accepted, but these actions run on Node.js 24. The `FORCE_JAVASCRIPT_ACTIONS_TO_NODE24` and `ACTIONS_ALLOW_USE_UNSECURE_NODE_VERSION` migration settings no longer affect runtime selection.

The runner's own internal operations (for example `hashFiles()` and this check) also use Node.js 24. The temporary `ACTIONS_RUNNER_FORCED_INTERNAL_NODE_VERSION=node20` fallback has been removed; this environment variable no longer changes the internal runtime.

> Not the `node` from `$PATH`

## What is checked?

- Make HTTPS GET to https://api.github.com or https://myGHES.com/api/v3 using node.js, make sure it gets 200 response code.

## How to fix the issue?

### 1. Check the common network issue
  
  > Please check the [network doc](./network.md)

### 2. SSL certificate related issue

  If you are seeing `Https request failed due to SSL cert issue` in the log, it means the `node.js` can't connect to the GitHub server due to SSL handshake failure.
  > Please check the [SSL cert doc](./sslcert.md)
  
## Still not working?

Contact GitHub customer service or log an issue at https://github.com/actions/runner if you think it's a runner issue.
