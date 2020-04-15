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

#---------------------------------------
# Stop and uninstall the service
#---------------------------------------
echo
echo "Uninstall the service ..."
pushd ./runner
./svc.sh stop
./svc.sh uninstall

base_api_url="https://api.github.com/orgs"
if [[ "$runner_scope" == *\/* ]]; then
    base_api_url="https://api.github.com/repos"
fi

#--------------------------------------
# Get id of runner to remove
#--------------------------------------
runner_id=$(curl -s -X GET ${base_api_url}/${runner_scope}/actions/runners  -H "accept: application/vnd.github.everest-preview+json" -H "authorization: token ${RUNNER_CFG_PAT}" | jq -M -j ".runners | .[] | [select(.name == \"${runner_name}\")] | .[0].id")

if [ -z "${runner_id}" ]; then 
    fatal "Could not find runner with name ${runner_name}"
fi 

echo "Removing id ${runner_id}"

#--------------------------------------
# Remove the runner
#--------------------------------------
curl -s -X DELETE ${base_api_url}/${runner_scope}/actions/runners/${runner_id} -H "authorization: token ${RUNNER_CFG_PAT}"

echo "Done."
