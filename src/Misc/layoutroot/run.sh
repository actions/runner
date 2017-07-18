#!/bin/bash

# Validate not sudo
user_id=`id -u`
if [ $user_id -eq 0 ]; then
    echo "Must not run interactively with sudo"
    exit 1
fi

# Get the agent root directory - https://stackoverflow.com/questions/59895/getting-the-source-directory-of-a-bash-script-from-within
SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
agent_directory=$DIR

# Validate the agent is configured
if [ ! -f $agent_directory/.agent ] && [[ "$1" != "--yaml" ]] ; then
    echo "Must configure first. Run ./config.sh"
    exit 1
fi

# Run
$agent_directory/bin/Agent.Listener run $*
