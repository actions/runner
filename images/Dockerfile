ARG BUILD_FROM=mcr.microsoft.com/dotnet/runtime-deps:6.0-jammy
ARG DOCKER_VERSION=27.1.1-cli
ARG RUNNER_CONTAINER_HOOKS_VERSION=0.6.1

##############################################################################
# Create a versioned alias for docker:cli
# Dockerfile syntax does not allow variables in the COPY source, but this alias
# works perfectly.
FROM docker:${DOCKER_VERSION} AS docker-cli

##############################################################################
# This build stage has the potential to create a mess, so we isolate it and copy
# only the desired files into our final stage.
FROM $BUILD_FROM AS build

ARG TARGETOS
ARG TARGETARCH
ARG RUNNER_VERSION
ARG RUNNER_CONTAINER_HOOKS_VERSION

WORKDIR /actions-runner

# Install support packages
# 'gpg-agent' and 'software-properties-common' are needed for 'add-apt-repository'
RUN <<EOF
#!/bin/bash
set -e
apt-get update
apt-get install -y --no-install-recommends curl unzip
apt-get install -y --no-install-recommends gpg-agent software-properties-common
EOF

# Configure git-core/ppa based on guidance here: https://git-scm.com/download/linux
# We will copy the .gpg and .list files generated here into the final image.
RUN add-apt-repository ppa:git-core/ppa

# Install the GitHub Actions Runner
# Valid RUNNER_ARCH selections are x64 and arm64 while TARGETARCH selections are
# amd64 and arm64 respectively.
RUN <<EOF
#!/bin/bash
set -eu
case ${TARGETARCH} in
  amd64) RUNNER_ARCH=x64 ;;
  *) RUNNER_ARCH=${TARGETARCH} ;;
esac
curl -fsSL https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-${TARGETOS}-${RUNNER_ARCH}-${RUNNER_VERSION}.tar.gz | tar xzf -
EOF

# Install container hooks
RUN <<EOF
#!/bin/bash
set -eu
curl -fsSLo runner-container-hooks.zip https://github.com/actions/runner-container-hooks/releases/download/v${RUNNER_CONTAINER_HOOKS_VERSION}/actions-runner-hooks-k8s-${RUNNER_CONTAINER_HOOKS_VERSION}.zip
unzip -q -d k8s runner-container-hooks.zip
rm -f runner-container-hooks.zip
EOF

##############################################################################
# This stage builds the main runner image. Much of the content is copied from
# our build stage.
FROM $BUILD_FROM

ENV DEBIAN_FRONTEND=noninteractive \
    RUNNER_MANUALLY_TRAP_SIG=1 \
    ACTIONS_RUNNER_PRINT_LOG_TO_STDOUT=1 \
    ImageOS=ubuntu22

# Copy PPA configurations from build stage
COPY --from=build /etc/apt/sources.list.d/*.list /etc/apt/sources.list.d/
COPY --from=build /etc/apt/trusted.gpg.d/*.gpg /etc/apt/trusted.gpg.d/

# Install docker and cli-plugins as a copy from docker:cli image
COPY --from=docker-cli --chown=root:root --chmod=0555 /usr/local/bin/docker /usr/bin/docker
COPY --from=docker-cli --chown=root:root --chmod=0555 /usr/local/libexec/docker/cli-plugins/docker-buildx /usr/local/libexec/docker/cli-plugins/docker-buildx

# Install a sane set of base utilities
# 'gpg-agent' and 'software-properties-common' are needed for 'add-apt-repository'
RUN <<EOF
#!/bin/bash
set -eu

apt-get update

apt-get install -y --no-install-recommends curl git jq sudo unzip
apt-get install -y --no-install-recommends gpg-agent software-properties-common

# Cleanup apt caches and lists
apt-get clean all
rm -rf /var/lib/apt/lists/* /var/cache/apt/*
EOF

# Create the actions runner user and home directory
RUN <<EOF
#!/bin/bash
set -eu
adduser --home /home/runner --disabled-password --gecos "" --uid 1001 runner
groupadd docker --gid 123
usermod -aG sudo runner
usermod -aG docker runner
cat >/etc/sudoers.d/runner <<_SUDOERS_
%sudo   ALL=(ALL:ALL) NOPASSWD:ALL
Defaults env_keep += "DEBIAN_FRONTEND"
_SUDOERS_
EOF

# Copy everything from /actions-runner in the build stage as our runner home
WORKDIR /home/runner
COPY --chown=runner:docker --from=build /actions-runner .
USER runner
