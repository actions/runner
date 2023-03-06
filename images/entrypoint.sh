#!/bin/bash

# update ca certificates in case they are injected with a volume mount
function updateCerts() {
    if [[ ! -z "$(which update-ca-certificates)" ]]; then
        echo "Running sudo update-ca-certificates"
        sudo update-ca-certificates
    elif [[ ! -z "$(which update-ca-trust)" ]]; then
        echo "Running sudo update-ca-trust"
        sudo update-ca-trust
    fi
}

if [[ ! -z $RUNNER_UPDATE_CA ]]; then
    updateCerts
fi

exec ./run.sh $*
