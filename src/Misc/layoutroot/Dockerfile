# Alpine container with OS dependencies for dotnet core
# https://hub.docker.com/_/microsoft-dotnet-core-runtime-deps/
FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine

# src$ ./dev.sh container Release alpine-x64
# _layout$ docker build -t actions-runner .
#
# Spin a runner up
# export RUNNER_REG_URL=https://github.com/YOURORG/YOURREPO
# export RUNNER_PAT_TOKEN
# docker run --env RUNNER_SCOPE=$RUNNER_SCOPE --env RUNNER_PAT=$RUNNER_PAT --env RUNNER_LABELS=$RUNNER_LABELS --network host --rm -it actions-runner
#
# interactive shell
# docker run --network host --rm -it --entrypoint /bin/bash actions-runner

RUN apk update
RUN apk upgrade
RUN apk add bash
RUN apk add jq
RUN apk add curl

# Directory for runner to operate in
RUN mkdir /actions-runner
WORKDIR /actions-runner
COPY . /actions-runner

# Allow runner to run as root
ENV RUNNER_ALLOW_RUNASROOT=1
ENV NODE12_ACTION_RUNTIME_PATH=/actions-runner/externals/node12_alpine/bin/node

ENTRYPOINT ["./bin/entrypoint.sh"]