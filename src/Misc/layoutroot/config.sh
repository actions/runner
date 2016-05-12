user_id=`id -u`

if [ $user_id -eq 0 ]; then
    echo "Must not run with sudo"
    exit 1
fi

# user_name=`id -nu $user_id`

sudo ./bin/Agent.Listener configure $*