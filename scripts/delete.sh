#/bin/bash

set -e

#
# Force deletes a runner from the service
# The caller should have already ensured the runner is gone and/or stopped
#
# Examples:
# RUNNER_CFG_PAT=<yourPAT> ./delete.sh myuser/myrepo myname
# RUNNER_CFG_PAT=<yourPAT> ./delete.sh myorg
#
# Usage:
#     export RUNNER_CFG_PAT=<yourPAT>
#     ./delete.sh scope name
#
#      scope required  repo (:owner/:repo) or org (:organization)
#      name  optional  defaults to hostname.  name to delete
# 
# Notes:
# PATS over envvars are more secure
# Works on OSX and Linux 
# Assumes x64 arch
#

runner_scope=${1}
runner_name=${2}

echo "Deleting runner ${runner_name} @ ${runner_scope}"

function fatal()
{
   echo "error: $1" >&2
   exit 1
}

if [ -z "${runner_scope}" ]; then fatal "supply scope as argument 1"; fi
if [ -z "${runner_name}" ]; then fatal "supply name as argument 2"; fi
if [ -z "${RUNNER_CFG_PAT}" ]; then fatal "RUNNER_CFG_PAT must be set before calling"; fi

which curl || fatal "curl required.  Please install in PATH with apt-get, brew, etc"
which jq || fatal "jq required.  Please install in PATH with apt-get, brew, etc"

base_api_url="https://api.github.com/orgs"
if [[ "$runner_scope" == *\/* ]]; then
    base_api_url="https://api.github.com/repos"
fi


#--------------------------------------
# Ensure offline
#--------------------------------------
runner_status=$(curl -s -X GET ${base_api_url}/${runner_scope}/actions/runners?per_page=100  -H "accept: application/vnd.github.everest-preview+json" -H "authorization: token ${RUNNER_CFG_PAT}" \
        | jq -M -j ".runners | .[] | [select(.name == \"${runner_name}\")] | .[0].status")

if [ -z "${runner_status}" ]; then 
    fatal "Could not find runner with name ${runner_name}"
fi

echo "Status: ${runner_status}"

if [ "${runner_status}" != "offline" ]; then 
    fatal "Runner should be offline before removing"
fi

#--------------------------------------
# Get id of runner to remove
#--------------------------------------
runner_id=$(curl -s -X GET ${base_api_url}/${runner_scope}/actions/runners?per_page=100  -H "accept: application/vnd.github.everest-preview+json" -H "authorization: token ${RUNNER_CFG_PAT}" \
        | jq -M -j ".runners | .[] | [select(.name == \"${runner_name}\")] | .[0].id")

if [ -z "${runner_id}" ]; then 
    fatal "Could not find runner with name ${runner_name}"
fi 

echo "Removing id ${runner_id}"

#--------------------------------------
# Remove the runner
#--------------------------------------
curl -s -X DELETE ${base_api_url}/${runner_scope}/actions/runners/${runner_id} -H "authorization: token ${RUNNER_CFG_PAT}"

echo "Done."
