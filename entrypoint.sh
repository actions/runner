#!/bin/sh

if [ -n "${ADDITIONAL_PACKAGES}" ]; then
    TO_BE_INSTALLED=$(echo ${ADDITIONAL_PACKAGES} | tr "," " " )
    echo "Installing additional packages: ${TO_BE_INSTALLED}"
    sudo apt-get update && sudo apt-get install -y ${TO_BE_INSTALLED} && sudo apt-get clean
fi

registration_url="https://github.com/${GITHUB_OWNER}"
if [ -z "${GITHUB_REPOSITORY}" ]; then
    token_url="https://api.github.com/orgs/${GITHUB_OWNER}/actions/runners/registration-token"
else
    token_url="https://api.github.com/repos/${GITHUB_OWNER}/${GITHUB_REPOSITORY}/actions/runners/registration-token"
    registration_url="${registration_url}/${GITHUB_REPOSITORY}"
fi

echo "Requesting token at '${token_url}'"

payload=$(curl -sX POST -H "Authorization: token ${GITHUB_PAT}" ${token_url})
export RUNNER_TOKEN=$(echo $payload | jq .token --raw-output)

if [ -z "${RUNNER_NAME}" ]; then
    RUNNER_NAME=$(hostname)
fi

./config.sh \
    --name "${RUNNER_NAME}" \
    --token "${RUNNER_TOKEN}" \
    --url "${registration_url}" \
    --work "${RUNNER_WORKDIR}" \
    --labels "${RUNNER_LABELS}" \
    --unattended \
    --replace

remove() {
    payload=$(curl -sX POST -H "Authorization: token ${GITHUB_PAT}" ${token_url%/registration-token}/remove-token)
    export REMOVE_TOKEN=$(echo $payload | jq .token --raw-output)

    ./config.sh remove --unattended --token "${REMOVE_TOKEN}"
}

trap 'remove; exit 130' INT
trap 'remove; exit 143' TERM

./runsvc.sh "$*" &

wait $!
