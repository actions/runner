user_id=`id -u`

if [ $user_id -eq 0 ]; then
    echo "Must not run with sudo"
    exit 1
fi

source ./env.sh

if [[ "$1" == "remove" ]]; then
    sudo ./bin/Agent.Listener $*
else
    # user_name=`id -nu $user_id`

    sudo ./bin/Agent.Listener configure $*
fi
