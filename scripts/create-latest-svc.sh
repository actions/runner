#!/usr/bin/env bash
set -Eeo pipefail

is_user_root () { [ "${EUID:-$(id -u)}" -eq 0 ]; }
if is_user_root;then echo -e "Please do not run as root or with sudo";exit 1;fi

# Notes:
# PATS over envvars are more secure
# Downloads latest runner release (not pre-release)
# Configures it as a service more secure
# Should be used on VMs and not containers
# Works on OSX and Linux
# Assumes x64 arch (support arm64)
# See EXAMPLES below

flags_found=false

while getopts 's:g:n:r:u:l:df' opt; do
    flags_found=true

    case $opt in
        s)
            runner_scope=$OPTARG
        ;;
        g)
            ghe_hostname=$OPTARG
        ;;
        n)
            runner_name=$OPTARG
        ;;
        r)
            runner_group=$OPTARG
        ;;
        u)
            svc_user=$OPTARG
        ;;
        l)
            labels=$OPTARG
        ;;
        f)
            replace='true'
        ;;
        d)
            disableupdate='true'
        ;;
        *)
            echo "
Runner Service Installer
Examples:
RUNNER_CFG_PAT=<yourPAT> ./create-latest-svc.sh myuser/myrepo my.ghe.deployment.net
RUNNER_CFG_PAT=<yourPAT> ./create-latest-svc.sh -s myorg -u user_name -l label1,label2
Usage:
    export RUNNER_CFG_PAT=<yourPAT>
    ./create-latest-svc scope [ghe_domain] [name] [user] [labels]
    -s          required  scope: repo (:owner/:repo) or org (:organization)
    -g          optional  ghe_hostname: the fully qualified domain name of your GitHub Enterprise Server deployment
    -n          optional  name of the runner, defaults to hostname
    -r          optional  name of the runner group to add the runner to, defaults to the Default group
    -u          optional  user svc will run as, defaults to current
    -l          optional  list of labels (split by comma) applied on the runner
    -d          optional  allow runner to remain on the current version for one month after the release of a newer version
    -f          optional  replace any existing runner with the same name"
        exit 0
        ;;
    esac
done

shift "$((OPTIND - 1))"

if ! "$flags_found"; then
    runner_scope=${1}
    ghe_hostname=${2}
    runner_name=${3:-$(hostname)}
    svc_user=${4:-$USER}
    labels=${5}
    runner_group=${6}
fi

# apply defaults
runner_name=${runner_name:-$(hostname)}
svc_user=${svc_user:-$USER}

echo "Configuring runner @ ${runner_scope}"
sudo echo

#---------------------------------------
# Validate Environment
#---------------------------------------
runner_plat=linux
[ ! -z "$(which sw_vers)" ] && runner_plat=osx;

runner_arch=x64
[ ! -z "$(arch | grep arm64)" ] && runner_arch=arm64

fatal () {
    echo "error: $1" >&2
    exit 1
}

if [ -z "${runner_scope}" ]; then fatal "supply scope as argument 1"; fi
if [ -z "${RUNNER_CFG_PAT}" ]; then fatal "RUNNER_CFG_PAT must be set before calling"; fi

which curl || fatal "curl required.  Please install in PATH with apt-get, brew, etc"
which jq || fatal "jq required.  Please install in PATH with apt-get, brew, etc"

svc_user_home="$(awk -v usr=${svc_user} -F ':' '$1 == usr {print $6}' /etc/passwd)"
# bail early if there's already a runner there. also sudo early
if sudo test -d ${svc_user_home}/runner; then
    fatal "Runner already exists.  Use a different directory or delete ${svc_user_home}/runner"
fi

sudo -u ${svc_user} mkdir ${svc_user_home}/runner

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
if [[ "$runner_scope" =~ / ]]; then
    orgs_or_repos="repos"
fi

export RUNNER_TOKEN=$(curl -fsSL -X POST ${base_api_url}/${orgs_or_repos}/${runner_scope}/actions/runners/registration-token -H "accept: application/vnd.github.everest-preview+json" -H "authorization: token ${RUNNER_CFG_PAT}" | jq -r '.token')

if [ "null" = "$RUNNER_TOKEN" -o -z "$RUNNER_TOKEN" ]; then fatal "Failed to get a token"; fi

#---------------------------------------
# Download latest released and extract
#---------------------------------------
echo
echo "Downloading latest runner ..."

# For the GHES Alpha, download the runner from github.com
latest_version_label=$(curl -fsSL -X GET 'https://api.github.com/repos/actions/runner/releases/latest' | jq -r '.tag_name')
latest_version=$(echo ${latest_version_label:1})
runner_file="actions-runner-${runner_plat}-${runner_arch}-${latest_version}.tar.gz"

if [ -f "/tmp/${runner_file}" ]; then
    echo "/tmp/${runner_file} exists. skipping download."
else
    runner_url="https://github.com/actions/runner/releases/download/${latest_version_label}/${runner_file}"

    echo "Downloading ${latest_version_label} for ${runner_plat} ..."
    echo $runner_url

    curl -o /tmp/${runner_file} -fsSL ${runner_url}
fi

ls -la /tmp/${runner_file}

#---------------------------------------------------
# extract to runner directory in this directory
#---------------------------------------------------
echo
echo "Extracting ${runner_file} to ${svc_user_home}/runner"

sudo tar xzf "/tmp/${runner_file}" -C ${svc_user_home}/runner

# export of pass
sudo chown -R ${svc_user}:${svc_user} ${svc_user_home}/runner

#---------------------------------------
# Unattended config
#---------------------------------------
runner_url="https://github.com/${runner_scope}"
if [ -n "${ghe_hostname}" ]; then
    runner_url="https://${ghe_hostname}/${runner_scope}"
fi

echo
echo "Configuring ${runner_name} @ $runner_url"
echo "sudo RUNNER_TOKEN=\${RUNNER_TOKEN} -i -u ${svc_user} bash -c \"cd ${svc_user_home}/runner && ./config.sh --unattended --url $runner_url --token \${RUNNER_TOKEN} ${replace:+--replace} --name $runner_name ${labels:+--labels $labels} ${runner_group:+--runnergroup "$runner_group"} ${disableupdate:+--disableupdate}\""
sudo RUNNER_TOKEN=${RUNNER_TOKEN} -i -u ${svc_user} bash -c "cd ${svc_user_home}/runner && ./config.sh --unattended --url $runner_url --token ${RUNNER_TOKEN} ${replace:+--replace} --name $runner_name ${labels:+--labels $labels} ${runner_group:+--runnergroup "$runner_group"} ${disableupdate:+--disableupdate}"

#---------------------------------------
# Configuring as a service
#---------------------------------------
echo
echo "Configuring as a service ..."
prefix=""
if [ "${runner_plat}" == "linux" ]; then
    sudo -i -u ${svc_user} bash -c "cd ${svc_user_home}/runner && ./svc.sh install ${svc_user}"
    sudo ${svc_user_home}/runner/svc.sh start
else
    ./svc.sh install ${svc_user}
    ./svc.sh start
fi
