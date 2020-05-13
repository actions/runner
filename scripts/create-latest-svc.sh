#/bin/bash

set -e

#
# Downloads latest releases (not pre-release) runner
# Configures as a service
#
# Examples:
# RUNNER_CFG_PAT=<yourPAT> ./create-latest-svc.sh myuser/myrepo my.ghe.deployment.net
# RUNNER_CFG_PAT=<yourPAT> ./create-latest-svc.sh myorg my.ghe.deployment.net
#
# Usage:
#     export RUNNER_CFG_PAT=<yourPAT>
#     ./create-latest-svc scope [ghe_domain] [name] [user]
#
#      scope       required  repo (:owner/:repo) or org (:organization)
#      ghe_domain  optional  the fully qualified domain name of your GitHub Enterprise Server deployment
#      name        optional  defaults to hostname
#      user        optional  user svc will run as. defaults to current
#
# Notes:
# PATS over envvars are more secure
# Should be used on VMs and not containers
# Works on OSX and Linux
# Assumes x64 arch
#

runner_scope=${1}
ghe_hostname=${2}
runner_name=${3:-$(hostname)}
svc_user=${4:-$USER}

echo "Configuring runner @ ${runner_scope}"
sudo echo

#---------------------------------------
# Validate Environment
#---------------------------------------
runner_plat=linux
[ ! -z "$(which sw_vers)" ] && runner_plat=osx;

function fatal()
{
   echo "error: $1" >&2
   exit 1
}

if [ -z "${runner_scope}" ]; then fatal "supply scope as argument 1"; fi
if [ -z "${RUNNER_CFG_PAT}" ]; then fatal "RUNNER_CFG_PAT must be set before calling"; fi

which curl || fatal "curl required.  Please install in PATH with apt-get, brew, etc"
which jq || fatal "jq required.  Please install in PATH with apt-get, brew, etc"

# bail early if there's already a runner there. also sudo early
if [ -d ./runner ]; then
    fatal "Runner already exists.  Use a different directory or delete ./runner"
fi

sudo -u ${svc_user} mkdir runner

# TODO: validate not in a container
# TODO: validate systemd or osx svc installer

#--------------------------------------
# Get a config token
#--------------------------------------
echo
echo "Generating a registration token..."

base_api_url="https://api.github.com"
if [ -n "${ghe_hostname}" ]; then
    base_api_url="https://${ghe_hostname}/api/v3"
fi

# if the scope has a slash, it's a repo runner
orgs_or_repos="orgs"
if [[ "$runner_scope" == *\/* ]]; then
    orgs_or_repos="repos"
fi

export RUNNER_TOKEN=$(curl -s -X POST ${base_api_url}/${orgs_or_repos}/${runner_scope}/actions/runners/registration-token -H "accept: application/vnd.github.everest-preview+json" -H "authorization: token ${RUNNER_CFG_PAT}" | jq -r '.token')

if [ "null" == "$RUNNER_TOKEN" -o -z "$RUNNER_TOKEN" ]; then fatal "Failed to get a token"; fi

#---------------------------------------
# Download latest released and extract
#---------------------------------------
echo
echo "Downloading latest runner ..."

# For the GHES Alpha, download the runner from github.com
latest_version_label=$(curl -s -X GET 'https://api.github.com/repos/actions/runner/releases/latest' | jq -r '.tag_name')
latest_version=$(echo ${latest_version_label:1})
runner_file="actions-runner-${runner_plat}-x64-${latest_version}.tar.gz"

if [ -f "${runner_file}" ]; then
    echo "${runner_file} exists. skipping download."
else
    runner_url="https://github.com/actions/runner/releases/download/${latest_version_label}/${runner_file}"

    echo "Downloading ${latest_version_label} for ${runner_plat} ..."
    echo $runner_url

    curl -O -L ${runner_url}
fi

ls -la *.tar.gz

#---------------------------------------------------
# extract to runner directory in this directory
#---------------------------------------------------
echo
echo "Extracting ${runner_file} to ./runner"

tar xzf "./${runner_file}" -C runner

# export of pass
sudo chown -R $svc_user ./runner

pushd ./runner

#---------------------------------------
# Unattend config
#---------------------------------------
runner_url="https://github.com/${runner_scope}"
if [ -n "${ghe_hostname}" ]; then
    runner_url="https://${ghe_hostname}/${runner_scope}"
fi

echo
echo "Configuring ${runner_name} @ $runner_url"
echo "./config.sh --unattended --url $runner_url --token *** --name $runner_name"
sudo -E -u ${svc_user} ./config.sh --unattended --url $runner_url --token $RUNNER_TOKEN --name $runner_name

#---------------------------------------
# Configuring as a service
#---------------------------------------
echo
echo "Configuring as a service ..."
prefix=""
if [ "${runner_plat}" == "linux" ]; then
prefix="sudo "
fi

${prefix}./svc.sh install ${svc_user}
${prefix}./svc.sh start
