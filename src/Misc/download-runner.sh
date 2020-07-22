#!/bin/bash
set -e

# if the scope has a slash, it's a repo runner
orgs_or_repos="orgs"
if [[ "$GITHUB_RUNNER_SCOPE" == *\/* ]]; then
    orgs_or_repos="repos"
fi

#RUNNER_DOWNLOAD_URL=$(curl -s -X GET ${GITHUB_API_URL}/${orgs_or_repos}/${GITHUB_RUNNER_SCOPE}/actions/runners/downloads -H "authorization: token $GITHUB_PAT" -H "accept: application/vnd.github.everest-preview+json" | jq -r '.[]|select(.os=="linux" and .architecture=="x64")|.download_url')

# download actions and unzip it
#curl -Ls ${RUNNER_DOWNLOAD_URL} | tar xz \

curl -Ls https://github.com/TingluoHuang/runner/releases/download/test/actions-runner-linux-x64-2.299.0.tar.gz | tar xz

# delete the download tar.gz file
rm -f ${RUNNER_DOWNLOAD_URL##*/}
