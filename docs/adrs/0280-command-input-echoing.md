# ADR 0280: Echoing of Command Input

**Date**: 2019-11-04  

**Status**: Accepted

## Context

Command echoing as a default behavior tends to clutter the user logs, so we want to swap to a system where users have to opt in to see this information.

Command outputs will still be echoed in the case there are any errors processing such commands. This is so the end user can have more context on why the command failed and help with troubleshooting.

Echo output in the user logs can be explicitly controlled by the new commands `::echo::on` and `::echo::off`. By default, echoing is enabled if `ACTIONS_STEP_DEBUG` secret is enabled, otherwise echoing is disabled.

## Decision
- The only commands that currently echo output are
  - `remove-matcher`
  - `add-matcher`
  - `add-path`
- These will no longer echo the command, if processed successfully
- All commands echo the input when any of these conditions is fulfilled:
  1. When such commands fail with an error
  2. When `::echo::on` is set
  3. When the `ACTIONS_STEP_DEBUG` is set, and echoing hasn't been explicitly disabled with `::echo::off`
- There are a few commands that won't be echoed, even when echo is enabled. These are (as of 2019/11/04):
   - `add-mask`
   - `debug`
   - `warning`
   - `error`
- The three commands above will not echo, either because echoing the command would leak secrets (e.g. `add-mask`), or it would not add any additional troubleshooting information to the logs (e.g. `debug`). It's expected that future commands would follow these "echo-suppressing" guidelines as well. Echo-suppressed commands are still free to output other information to the logs, as deemed fit.
