#!/bin/bash

SECONDS=0

# Bash 4.0 and above supports read.
if [[ -n "$BASH_VERSINFO" && "${BASH_VERSINFO[0]}" -ge 4 ]]; then
    echo foo
    # Bash has no sleep builtin, but read with a timeout can behave in same way.
    read -rt "$1" <> <(:) || :
fi
# Fallback to busy wait.
while [[ $SECONDS -lt $1 ]]; do
    echo bar
    :
done
