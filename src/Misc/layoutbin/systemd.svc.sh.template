#!/bin/bash

SVC_NAME="{{SvcNameVar}}"
SVC_NAME=${SVC_NAME// /_}
SVC_DESCRIPTION="{{SvcDescription}}"

SVC_CMD=$1
arg_2=${2}

RUNNER_ROOT=`pwd`

UNIT_PATH=/etc/systemd/system/${SVC_NAME}
TEMPLATE_PATH=$GITHUB_ACTIONS_RUNNER_SERVICE_TEMPLATE
IS_CUSTOM_TEMPLATE=0
if [[ -z $TEMPLATE_PATH ]]; then
    TEMPLATE_PATH=./bin/actions.runner.service.template
else
    IS_CUSTOM_TEMPLATE=1
fi
TEMP_PATH=./bin/actions.runner.service.temp
CONFIG_PATH=.service

user_id=`id -u`

# systemctl must run as sudo
# this script is a convenience wrapper around systemctl
if [ $user_id -ne 0 ]; then
    echo "Must run as sudo"
    exit 1
fi

function failed()
{
   local error=${1:-Undefined error}
   echo "Failed: $error" >&2
   exit 1
}

if [ ! -f "${TEMPLATE_PATH}" ]; then
    if [[ $IS_CUSTOM_TEMPLATE = 0 ]]; then
        failed "Must run from runner root or install is corrupt"
    else
        failed "Service file at '$GITHUB_ACTIONS_RUNNER_SERVICE_TEMPLATE' using GITHUB_ACTIONS_RUNNER_SERVICE_TEMPLATE env variable is not found"
    fi
fi

#check if we run as root
if [[ $(id -u) != "0" ]]; then
    echo "Failed: This script requires to run with sudo." >&2
    exit 1
fi

function install()
{
    echo "Creating launch runner in ${UNIT_PATH}"
    if [ -f "${UNIT_PATH}" ]; then
        failed "error: exists ${UNIT_PATH}"
    fi

    if [ -f "${TEMP_PATH}" ]; then
      rm "${TEMP_PATH}" || failed "failed to delete ${TEMP_PATH}"
    fi

    # can optionally use username supplied
    run_as_user=${arg_2:-$SUDO_USER}
    echo "Run as user: ${run_as_user}"

    run_as_uid=$(id -u ${run_as_user}) || failed "User does not exist"
    echo "Run as uid: ${run_as_uid}"

    run_as_gid=$(id -g ${run_as_user}) || failed "Group not available"
    echo "gid: ${run_as_gid}"

    sed "s/{{User}}/${run_as_user}/g; s/{{Description}}/$(echo ${SVC_DESCRIPTION} | sed -e 's/[\/&]/\\&/g')/g; s/{{RunnerRoot}}/$(echo ${RUNNER_ROOT} | sed -e 's/[\/&]/\\&/g')/g;" "${TEMPLATE_PATH}" > "${TEMP_PATH}" || failed "failed to create replacement temp file"
    mv "${TEMP_PATH}" "${UNIT_PATH}" || failed "failed to copy unit file"

    # Recent Fedora based Linux (CentOS/Redhat) has SELinux enabled by default
    # We need to restore security context on the unit file we added otherwise SystemD have no access to it.
    command -v getenforce > /dev/null
    if [ $? -eq 0 ]
    then
        selinuxEnabled=$(getenforce)
        if [[ $selinuxEnabled == "Enforcing" ]]
        then
            # SELinux is enabled, we will need to Restore SELinux Context for the service file
            restorecon -r -v "${UNIT_PATH}" || failed "failed to restore SELinux context on ${UNIT_PATH}"
        fi
    fi
    
    # unit file should not be executable and world writable
    chmod 664 "${UNIT_PATH}" || failed "failed to set permissions on ${UNIT_PATH}"
    systemctl daemon-reload || failed "failed to reload daemons"
    
    # Since we started with sudo, runsvc.sh will be owned by root. Change this to current login user.    
    cp ./bin/runsvc.sh ./runsvc.sh || failed "failed to copy runsvc.sh"
    chown ${run_as_uid}:${run_as_gid} ./runsvc.sh || failed "failed to set owner for runsvc.sh"
    chmod 755 ./runsvc.sh || failed "failed to set permission for runsvc.sh"

    systemctl enable ${SVC_NAME} || failed "failed to enable ${SVC_NAME}"

    echo "${SVC_NAME}" > ${CONFIG_PATH} || failed "failed to create .service file"
    chown ${run_as_uid}:${run_as_gid} ${CONFIG_PATH} || failed "failed to set permission for ${CONFIG_PATH}"
}

function start()
{
    systemctl start ${SVC_NAME} || failed "failed to start ${SVC_NAME}"
    status    
}

function stop()
{
    systemctl stop ${SVC_NAME} || failed "failed to stop ${SVC_NAME}"    
    status
}

function uninstall()
{
    if service_exists; then
        stop
        systemctl disable ${SVC_NAME} || failed "failed to disable ${SVC_NAME}"
        rm "${UNIT_PATH}" || failed "failed to delete ${UNIT_PATH}"
    else
        echo "Service ${SVC_NAME} is not installed"
    fi    
    if [ -f "${CONFIG_PATH}" ]; then
      rm "${CONFIG_PATH}" || failed "failed to delete ${CONFIG_PATH}"
    fi
    systemctl daemon-reload || failed "failed to reload daemons"
}

function service_exists() {
    if [ -f "${UNIT_PATH}" ]; then
        return 0
    else
        return 1
    fi
}

function status()
{
    if service_exists; then
        echo
        echo "${UNIT_PATH}"
    else
        echo
        echo "not installed"
        echo
        exit 1
    fi

    systemctl --no-pager status ${SVC_NAME}
}

function usage()
{
    echo
    echo Usage:
    echo "./svc.sh [install, start, stop, status, uninstall]"
    echo "Commands:"
    echo "   install [user]: Install runner service as Root or specified user."
    echo "   start: Manually start the runner service."
    echo "   stop: Manually stop the runner service."
    echo "   status: Display status of runner service."
    echo "   uninstall: Uninstall runner service."
    echo
}

case $SVC_CMD in
   "install") install;;
   "status") status;;
   "uninstall") uninstall;;
   "start") start;;
   "stop") stop;;
   "status") status;;
   *) usage;;
esac

exit 0
