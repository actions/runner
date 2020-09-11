#!/bin/bash

set -euo pipefail

EVENT=$1
TIMESTAMP=$2

echo $EVENT
echo $TIMESTAMP

function fatal() {
   echo "error: $1" >&2
   exit 1
}

[ -n "${K8S_POD_NAME:-""}" ] || fatal "K8S_POD_NAME variable must be set"
echo $K8S_POD_NAME

kubectl get pod

kubectl annotate pods $K8S_POD_NAME $EVENT=$TIMESTAMP

echo "DONE"
