#/bin/bash

set -e

#
# Removes a runner running as a service
# Must be run on the machine where the service is run
#
# Examples:
# RUNNER_CFG_PAT=<yourPAT> ./remove-svc.sh myuser/myrepo
# RUNNER_CFG_PAT=<yourPAT> ./remove-svc.sh myorg
#
# Usage:
#     export RUNNER_CFG_PAT=<yourPAT>
#     ./remove-svc scope name
#
#      scope required  repo (:owner/:repo) or org (:organization)
#      name  optional  defaults to hostname.  name to uninstall and remove
# 
# Notes:
# PATS over envvars are more secure
# Should be used on VMs and not containers
# Works on OSX and Linux 
# Assumes x64 arch
#

runner_scope=${1}
runner_name=${2:-$(hostname)}

echo "Uninstalling runner ${runner_name} @ ${runner_scope}"
sudo echo

function fatal()
{
   echo "error: $1" >&2
   exit 1
}

if [ -z "${runner_scope}" ]; then fatal "supply scope as argument 1"; fi
if [ -z "${RUNNER_CFG_PAT}" ]; then fatal "RUNNER_CFG_PAT must be set before calling"; fi

which curl || fatal "curl required.  Please install in PATH with apt-get, brew, etc"
which jq || fatal "jq required.  Please install in PATH with apt-get, brew, etc"

runner_plat=linux
[ ! -z "$(which sw_vers)" ] && runner_plat=osx;

#--------------------------------------
# Get a remove token
#--------------------------------------
echo
echo "Generating a removal token..."

# if the scope has a slash, it's an repo runner
base_api_url="https://api.github.com/orgs"
if [[ "$runner_scope" == *\/* ]]; then
    base_api_url="https://api.github.com/repos"
fi

export REMOVE_TOKEN=$(curl -s -X POST ${base_api_url}/${runner_scope}/actions/runners/remove-token -H "accept: application/vnd.github.everest-preview+json" -H "authorization: token ${RUNNER_CFG_PAT}" | jq -r '.token')

if [ -z "$REMOVE_TOKEN" ]; then fatal "Failed to get a token"; fi 

#---------------------------------------
# Stop and uninstall the service
#---------------------------------------
echo
echo "Uninstall the service ..."
pushd ./runner
prefix=""
if [ "${runner_plat}" == "linux" ]; then 
    prefix="sudo "
fi 
${prefix}./svc.sh stop
${prefix}./svc.sh uninstall
${prefix}./config.sh remove --token $REMOVE_TOKEN
