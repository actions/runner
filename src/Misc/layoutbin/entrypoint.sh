#!/bin/ash

# # Remove runner upon receiving an EXIT signal
# function remove_runner {
#     echo "\nCaught EXIT signal. Removing runner and exiting.\n"
#     REMOVE_TOKEN=$(curl --data "" -H "Authorization: Bearer $TOKEN" https://api.github.com/repos/$GITHUB_REPO/actions/runners/remove-token | jq -r '.token')
#     ./config.sh remove --token $REMOVE_TOKEN
#     exit $?
# }

# # Watch for EXIT signal to be able to shut down gracefully
# trap remove_runner EXIT

# # Generate
# CONFIG_TOKEN=$(curl --data "" --header "Authorization: Bearer $TOKEN" https://api.github.com/repos/$GITHUB_REPO/actions/runners/registration-token | jq -r '.token')

# # Create the runner and configure it
# ./config.sh --url https://github.com/$GITHUB_REPO --token $CONFIG_TOKEN --unattended --replace

# # Run it
# ./bin/runsvc.sh