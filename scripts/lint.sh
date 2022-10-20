#!/bin/bash

docker run \
  -e RUN_LOCAL=true \
  --env-file ".github/super-linter.env" \
  -v "${PWD}":/tmp/lint \
  github/super-linter
