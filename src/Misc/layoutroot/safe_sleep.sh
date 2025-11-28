#!/bin/bash

if command -v sleep > /dev/null 2>&1; then
    sleep "$1"
else
    SECONDS=0
    while [[ $SECONDS -lt $1 ]]; do
        :
    done
fi
