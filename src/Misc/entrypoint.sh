#!/bin/bash

set -euo pipefail

function fatal() {
   echo "error: $1" >&2
   exit 1
}

[ -n "${GITHUB_PAT:-""}" ] || fatal "GITHUB_PAT variable must be set"
[ -n "${GITHUB_RUNNER_SCOPE:-""}" ] || fatal "GITHUB_RUNNER_SCOPE variable must be set"

# Use container id to gen unique runner name
CONTAINER_ID=$(cat /proc/self/cgroup | head -n 1 | tr '/' '\n' | tail -1 | cut -c1-12)
RUNNER_NAME="actions-runner-k8s-${CONTAINER_ID}"

# if the scope has a slash, it's a repo runner
orgs_or_repos="orgs"
if [[ "$GITHUB_RUNNER_SCOPE" == *\/* ]]; then
    orgs_or_repos="repos"
fi

RUNNER_REG_URL="${GITHUB_SERVER_URL:=https://github.com}/${GITHUB_RUNNER_SCOPE}"

echo "Runner Name      : ${RUNNER_NAME}"
echo "Registration URL : ${RUNNER_REG_URL}"
echo "GitHub API URL   : ${GITHUB_API_URL:=https://api.github.com}"
echo "Runner Labels    : ${RUNNER_LABELS:=""}"

# TODO: if api url is not default, validate it ends in /api/v3

RUNNER_LABELS_ARG=""
if [ -n "${RUNNER_LABELS}" ]; then
    RUNNER_LABELS_ARG="--labels ${RUNNER_LABELS}"
fi

if [ -n "${K8S_HOST_IP}" ]; then
    export http_proxy=http://$K8S_HOST_IP:9090
fi

curl -v -s -X POST ${GITHUB_API_URL}/${orgs_or_repos}/${GITHUB_RUNNER_SCOPE}/actions/runners/registration-token -H "authorization: token $GITHUB_PAT" -H "accept: application/vnd.github.everest-preview+json"

# Generate registration token
RUNNER_REG_TOKEN=$(curl -s -X POST ${GITHUB_API_URL}/${orgs_or_repos}/${GITHUB_RUNNER_SCOPE}/actions/runners/registration-token -H "authorization: token $GITHUB_PAT" -H "accept: application/vnd.github.everest-preview+json" | jq -r '.token')

# Create the runner and configure it
./config.sh --unattended --name $RUNNER_NAME --url $RUNNER_REG_URL --token $RUNNER_REG_TOKEN $RUNNER_LABELS_ARG --replace --ephemeral

# while (! docker version ); do
#   # Docker takes a few seconds to initialize
#   echo "Waiting for Docker to launch..."
#   sleep 1
# done

# Run it
./bin/runsvc.sh interactive

# export http_proxy=""
# dockerdpid=$(kubectl exec $K8S_POD_NAME --container docker-host -- pidof dockerd)
# kubectl exec $K8S_POD_NAME --container docker-host -- kill -SIGINT $dockerdpid