user_id=`id -u`

# we want to snapshot the environment of the config user
if [ $user_id -eq 0 ]; then
    echo "Must not run with sudo"
    exit 1
fi

source ./env.sh

if [[ "$1" == "remove" ]]; then
    ./bin/Agent.Listener $*
else
    # user_name=`id -nu $user_id`

    ./bin/Agent.Listener configure $*
fi
