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



#---------------------------------------
# Stop and uninstall the service
#---------------------------------------
echo
echo "Uninstall the service ..."
./svc.sh stop
./svc.sh uninstall

#--------------------------------------
# Get a removal token
#--------------------------------------
echo
echo "Generating a removal registration token..."

# if the scope has a slash, it's an repo runner
base_api_url="https://api.github.com/orgs"
if [[ "$runner_scope" == *\/* ]]; then
    base_api_url="https://api.github.com/repos"
fi

export REMOVE_TOKEN=$(curl -s -X POST ${base_api_url}/${runner_scope}/actions/runners/remove-token -H "accept: application/vnd.github.everest-preview+json" -H "authorization: token ${RUNNER_CFG_PAT}" | jq -r '.token')

if [ -z "$REMOVE_TOKEN" ]; then fatal "Failed to get a token"; fi 

#--------------------------------------
# Remove the runner
#--------------------------------------
# DELETE /orgs/:organization/actions/runners/:runner_id
curl -s -X DELETE ${base_api_url}/${runner_scope}/actions/runners/remove-token -H "authorization: token ${REMOVE_TOKEN}"

echo "Done."
