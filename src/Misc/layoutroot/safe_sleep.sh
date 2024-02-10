#!/bin/bash

if [ -x "$(command -v sleep)" ]; then
  sleep $1
  exit 0
fi
if [ -x "$(command -v ping)" ]; then
  ping -c $1 127.0.0.1 > /dev/null
  exit 0
fi

SECONDS=0
while [[ $SECONDS != $1 ]]; do
    :
done
