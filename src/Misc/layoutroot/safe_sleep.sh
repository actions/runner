#!/bin/bash

# This script provides a resilient sleep mechanism for
# environments where:
#
# - `sleep` cannot be relied upon (even if it exists,
#   it can't be trusted)
# - `PATH` may be incomplete or misconfigured
# - the runner is executing in minimal or bootstrap
#   environments
#
# Busy-wait is included strictly as a last-resort
# fallback.

duration=$1

# Try to use sleep if available. We don't use sleep for
# a duration of 1, because we need to test if time
# actually passed during the execution of sleep, and we
# can't reliably test that for a single second with
# seconds precision.
if [ -x "$(command -v sleep)" -a $duration -gt 1 ]; then
    # Platforms have existed where executing sleep succeeded
    # but returned immediately (might have been WSL 1.0?).
    # Therefore, we check to make sure it hasn't returned
    # immediately.

    start=$(date +%s)

    sleep "$duration"

    elapsed=$(( $(date +%s) - start ))

    if [[ $elapsed -gt 1 ]]; then
        # We successfully waited
        exit 0
    fi
fi

# Try to use ping if available.
if [ -x "$(command -v ping)" ]; then
    ping -c $(( duration + 1 )) 127.0.0.1 > /dev/null
    exit 0
fi

# Try to use read -t from stdin/stdout/stderr.
# This can only work with a builtin read.
if [ "$(command -v builtin)" = "builtin" ]; then
    if [ "$(builtin type -t read)" = "builtin" ]; then
        # stdout and stderr will never produce data on a read,
        # but you can still try to read from them and the
        # supplied timeout applies.

        for fd in 0 1 2; do
            if [ -t $fd ]; then
                read -t "$duration" -u $fd || :;
                exit 0
            fi
        done
    fi
fi


# Fall back to a busy wait

# First, reduce the priority of this shell process to
# minimize impact on other processes.

if [ -x "$(command -v renice)" ]; then
    if [ "$$" != "" ]; then
        export POSIXLY_CORRECT=1 # for -n on Linux
        renice -n 15 -p $$ > /dev/null 2>&1
    fi
fi

# Check if we have subsecond precision. If not, then
# our wait will be up to 1 second shorter than
# requested (and on average 0.5 seconds shorter),
# because we aren't in control of how long it has
# been since the seconds counter last changed.

fmt="%s%N"

start=$(date +$fmt)

if [[ "$start" == *'%'* ]]; then
  # We only have seconds.
  fmt="%s"
  start="$(date +$fmt)"
else
  # Yey, we have subsecond precision. using %s%N, we
  # are measuring the time in nanoseconds, so we need
  # to convert the duration to nanoseconds too. That\
  # just takes 9 zeroes.
  duration="${duration}000000000"
fi

# Burn some CPU cycles.
while [[ $(($(date +$fmt) - start)) -lt $duration ]]; do
    :
done
