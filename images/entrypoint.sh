#!/bin/bash

# update ca certificates if they are injected with a volume mount
sudo update-ca-certificates

exec ./run.sh $*
