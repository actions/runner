# Alpine container with OS dependencies for dotnet core
# https://hub.docker.com/_/microsoft-dotnet-core-runtime-deps/
FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine

# src$ ./dev.sh layout Release alpine-x64
# _layout$ docker build -t actions-runner .
# docker run --network host --rm -it actions-runner /bin/bash 

RUN apk update
RUN apk upgrade
RUN apk add bash

# Directory for runner to operate in
RUN mkdir /actions-runner
WORKDIR /actions-runner
COPY . /actions-runner

# Allow runner to run as root
ENV RUNNER_ALLOW_RUNASROOT=1

# ENTRYPOINT ["./bin/entrypoint.sh"]