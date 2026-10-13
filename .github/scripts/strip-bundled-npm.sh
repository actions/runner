#!/usr/bin/env bash
# Fork-only: remove the npm and corepack bundled with Node from a runner layout.
# The runner only executes externals/node*/bin/node; npm and corepack are unused and pin
# vulnerable dependencies (tar, minimatch, undici, ...) that would otherwise ship in every
# package. Lives outside src/ so upstream merges stay clean.
#
# Usage: strip-bundled-npm.sh <layout-dir> <runtime>
set -euo pipefail
layout="$1"
runtime="$2"

shopt -s nullglob
nodes=("$layout"/externals/node*/)
if [ ${#nodes[@]} -eq 0 ]; then
  echo "::error::No bundled Node found in $layout/externals"
  exit 1
fi
for d in "${nodes[@]}"; do
  rm -rf "${d}lib/node_modules/npm" "${d}lib/node_modules/corepack" \
    "${d}bin/npm" "${d}bin/npx" "${d}bin/corepack"
done
leftover="$(find "$layout/externals" \( -path '*/node_modules/npm' -o -path '*/node_modules/corepack' \
  -o -path '*/bin/npm' -o -path '*/bin/npx' -o -path '*/bin/corepack' \) -print -quit)"
if [ -n "$leftover" ]; then
  echo "::error::npm or corepack is still bundled in $layout/externals: $leftover"
  exit 1
fi
for d in "${nodes[@]}"; do
  if [ ! -x "${d}bin/node" ]; then
    echo "::error::${d}bin/node is missing"
    exit 1
  fi
  # Alpine builds need musl, and linux-arm (armv7l) cannot run on the arm64 build host;
  # the package tests cover those.
  case "$d" in *_alpine/) continue ;; esac
  if [ "$runtime" != linux-arm ]; then
    version="$("${d}bin/node" --version)"
    echo "${d}bin/node $version"
  fi
done
