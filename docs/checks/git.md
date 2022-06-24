# Git Connection Check

## What is this check for?

Make sure `git` can access GitHub.com or your GitHub Enterprise Server.


## What is checked?

The test is done by executing
```bash
# For GitHub.com
git ls-remote --exit-code https://github.com/actions/checkout HEAD

# For GitHub Enterprise Server
git ls-remote --exit-code https://ghes.me/actions/checkout HEAD
```

The test also set environment variable `GIT_TRACE=1` and `GIT_CURL_VERBOSE=1` before running `git ls-remote`, this will make `git` to produce debug log for better debug any potential issues.

## How to fix the issue?

### 1. fatal: unable to access 'https://github.com/actions/checkout/': The requested URL returned error: 400

Check your global/system git config for any unexpected auth headers

For example:

```
$ git config --global --list | grep extraheader
http.extraheader=AUTHORIZATION: malformed_auth_header

$ git config --system --list | grep extraheader
```

The following command can be used to remove the above value: `git config --global --unset http.extraheader`

### 2. Check the common network issue
  
  > Please check the [network doc](./network.md)

### 3. SSL certificate related issue

  If you are seeing `SSL Certificate problem:` in the log, it means the `git` can't connect to the GitHub server due to SSL handshake failure.
  > Please check the [SSL cert doc](./sslcert.md)
  
## Still not working?

Contact GitHub customer service or log an issue at https://github.com/actions/runner if you think it's a runner issue.
