#!/bin/bash
echo "Test-0"

set -euo pipefail

echo "Test-1"
function fatal() {
   echo "error: $1" >&2
   exit 1
}

echo "Test-2"
[ -n "${K8S_POD_NAME:-""}" ] || fatal "K8S_POD_NAME variable must be set"

echo "Test-3"
# echo $http_proxy
# # unset http_proxy
# # unset https_proxy
# export http_proxy=
# export HTTP_PROXY=

echo "Test-4"
kubectl -v9 get pod

echo "Test-5"
echo $K8S_POD_NAME
timestamp=$(date +%s)
echo $timestamp

kubectl annotate pods $K8S_POD_NAME JOBSTART=$timestamp

echo "Test-5"
