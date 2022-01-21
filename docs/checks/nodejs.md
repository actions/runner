# Node.js Connection Check

## What is this check for?

Make sure the built-in node.js has access to GitHub.com or GitHub Enterprise Server.

The runner carries it's own copy of node.js executable under `<runner_root>/externals/node12/` and `<runner_root>/externals/node16/`.

All javascript base Actions will get executed by the built-in `node` at `<runner_root>/externals/node16/` by default..

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
