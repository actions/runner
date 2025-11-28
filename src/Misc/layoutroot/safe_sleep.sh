#!/bin/bash

DURATION=$1

if [ -z "$DURATION" ]; then
  exit 1
fi

if command -v sleep &> /dev/null; then
    sleep "$DURATION"
else
    SECONDS=0
    while [[ $SECONDS -lt $DURATION ]]; do
        :
    done
fi