#/bin/bash

set -e

#
# Removes a runner running as a service
# Must be run on the machine where the service is run
# See EXAMPLES below
# 
# Notes:
# PATS over envvars are more secure
# Should be used on VMs and not containers
# Works on OSX and Linux 
# Assumes x64 arch
#


valid_flag_pattern='(\ |^)-s\ |(\ |^)--scope\ |(\ |^)-[Ss]cope\ '
help_pattern='(\ |^)-h(\ |$)|(\ |^)--help(\ |$)|(\ |^)-[Hh]elp(\ |$)'
if [[ $* =~ $valid_flag_pattern || $* =~ $help_pattern ]]; then
while [ $# -ne 0 ]
do
    name="$1"
    case "$name" in
        -s|--scope|-[Ss]cope)
            shift
            runner_scope=$1
            ;;
        -n|--name|-[Nn]ame)
            shift
            runner_name=$1
            ;;
        *)
            echo "
 Examples:
 RUNNER_CFG_PAT=<yourPAT> ./remove-svc.sh myuser/myrepo
 RUNNER_CFG_PAT=<yourPAT> ./remove-svc.sh myorg

 Usage:
     export RUNNER_CFG_PAT=<yourPAT>
     ./remove-svc myuser/myrepo
     ./remove-svc myuser/myrepo runner-name
     ./remove-svc --scope myuser/myrepo --name runner-name

      scope required  repo (:owner/:repo) or org (:organization)
      name  optional  defaults to hostname.  name to uninstall and remove"
            exit 0
    esac
    shift
done
else
    # process indexed args for backwards compatibility
    runner_scope=${1}
    runner_name=${2:-$(hostname)}
fi

# apply defaults
runner_name=${runner_name:-$(hostname)}

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
./config.sh remove --token $REMOVE_TOKEN
