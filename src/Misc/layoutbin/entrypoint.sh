#!/bin/bash

set -euo pipefail

#
# Variables:
#   RUNNER_SCOPE:       [Req] repo (owner/repo) or org (organization)
#   RUNNER_PAT:         [Req] A PAT token with at least admin scope
#   RUNNER_LABELS:      [Opt] Comma delimited list of initial labels for the runner
#   GITHUB_API_URL:     [Opt] Defaults to https://api.github.com.  
#                             Use https://YOURGHESHOSTNAME/api/v3 for GHES
#   GITHUB_SERVER_URL:  [Opt] Detaults to https://github.com

function fatal() {
   echo "error: $1" >&2
   exit 1
}

[ -n "${RUNNER_PAT:-""}" ] || fatal "RUNNER_PAT variable must be set"
[ -n "${RUNNER_SCOPE:-""}" ] || fatal "RUNNER_SCOPE variable must be set"

# Use container id to gen unique runner name
CONTAINER_ID=$(cat /proc/self/cgroup | head -n 1 | tr '/' '\n' | tail -1 | cut -c1-12)
RUNNER_NAME="alpine-${CONTAINER_ID}"

# if the scope has a slash, it's a repo runner
orgs_or_repos="orgs"
if [[ "$RUNNER_SCOPE" == *\/* ]]; then
    orgs_or_repos="repos"
fi

RUNNER_REG_URL="${GITHUB_SERVER_URL:=https://github.com}/${RUNNER_SCOPE}"

echo "Runner Name      : ${RUNNER_NAME}"
echo "Registration URL : ${RUNNER_REG_URL}"
echo "GitHub API URL   : ${GITHUB_API_URL:=https://api.github.com}"
echo "Runner Labels    : ${RUNNER_LABELS:=""}"

# TODO: if api url is not default, validate it ends in /api/v3

RUNNER_LABELS_ARG=""
if [ -n "${RUNNER_LABELS}" ]; then
    RUNNER_LABELS_ARG="--labels \"${RUNNER_LABELS}\""
fi

# Watch for EXIT signal to be able to shut down gracefully
# Remove runner upon receiving an EXIT signal
function remove_runner {
    echo "\nCaught EXIT signal. Removing runner and exiting.\n"
    RUNNER_REM_TOKEN=$(curl --data "" -H "Authorization: Bearer $RUNNER_PAT" ${GITHUB_API_URL}/${orgs_or_repos}/${RUNNER_SCOPE}/actions/runners/remove-token | jq -r '.token')
    ./config.sh remove --token $RUNNER_REM_TOKEN
    exit $?
}

trap remove_runner EXIT

# Generate registration token
RUNNER_REG_TOKEN=$(curl -s -X POST ${GITHUB_API_URL}/${orgs_or_repos}/${RUNNER_SCOPE}/actions/runners/registration-token -H "authorization: token $RUNNER_PAT" -H "accept: application/vnd.github.everest-preview+json" | jq -r '.token')

# Create the runner and configure it
./config.sh --unattended --name $RUNNER_NAME --url $RUNNER_REG_URL --token $RUNNER_REG_TOKEN $RUNNER_LABELS_ARG --replace

# Run it
./bin/runsvc.sh