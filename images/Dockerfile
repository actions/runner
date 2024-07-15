# Source: https://github.com/dotnet/dotnet-docker
FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-jammy as build

ARG TARGETOS
ARG TARGETARCH
ARG RUNNER_VERSION
ARG RUNNER_CONTAINER_HOOKS_VERSION=0.6.0
ARG DOCKER_VERSION=25.0.5
ARG BUILDX_VERSION=0.13.2

WORKDIR /actions-runner

RUN apt update -y && apt install -y --no-install-recommends curl unzip

RUN export RUNNER_ARCH=${TARGETARCH} \
    && if [ "$RUNNER_ARCH" = "amd64" ]; then export RUNNER_ARCH=x64 ; fi \
    && curl -fsSLo runner.tar.gz https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-${TARGETOS}-${RUNNER_ARCH}-${RUNNER_VERSION}.tar.gz \
    && tar xzf ./runner.tar.gz \
    && rm runner.tar.gz

RUN curl -fsSLo runner-container-hooks.zip https://github.com/actions/runner-container-hooks/releases/download/v${RUNNER_CONTAINER_HOOKS_VERSION}/actions-runner-hooks-k8s-${RUNNER_CONTAINER_HOOKS_VERSION}.zip \
    && unzip ./runner-container-hooks.zip -d ./k8s \
    && rm runner-container-hooks.zip

WORKDIR /docker
RUN export RUNNER_ARCH=${TARGETARCH} \
    && if [ "$RUNNER_ARCH" = "amd64" ]; then export DOCKER_ARCH=x86_64 ; fi \
    && if [ "$RUNNER_ARCH" = "arm64" ]; then export DOCKER_ARCH=aarch64 ; fi \
    && curl -fsSLo docker.tgz https://download.docker.com/${TARGETOS}/static/stable/${DOCKER_ARCH}/docker-${DOCKER_VERSION}.tgz \
    && tar zxvf docker.tgz \
    && rm -rf docker.tgz \
    # Get CLI plugins
    && rm -rf plugins \
    && mkdir -p plugins \
    # docker-buildx
    && curl -fsSLo plugins/docker-buildx "https://github.com/docker/buildx/releases/download/v${BUILDX_VERSION}/buildx-v${BUILDX_VERSION}.linux-${TARGETARCH}"

# Configure git-core/ppa based on guidance here: https://git-scm.com/download/linux
# The second `apt update` isn't strictly necessary, but let's leave it here to force the
# image build to break if the ppa is suddenly unavailable.
WORKDIR /ppa
RUN apt update -y \
    && apt install -y --no-install-recommends sudo lsb-release gpg-agent software-properties-common \
    && add-apt-repository ppa:git-core/ppa \
    && apt update -y

FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-jammy

ENV DEBIAN_FRONTEND=noninteractive
ENV RUNNER_MANUALLY_TRAP_SIG=1
ENV ACTIONS_RUNNER_PRINT_LOG_TO_STDOUT=1
ENV ImageOS=ubuntu22

# Copy PPAs from build stage
COPY --from=build /etc/apt/sources.list.d/*.list /etc/apt/sources.list.d/
COPY --from=build /etc/apt/trusted.gpg.d/*.gpg /etc/apt/trusted.gpg.d/

# Copy Docker files from build stage
COPY --from=build --chown=root:root --chmod=0755 /docker/docker/* /usr/bin/
COPY --from=build --chown=root:root --chmod=0755 /docker/plugins/* /usr/local/lib/docker/cli-plugins/

# Install a sane set of base utilities
RUN apt update -y \
    && apt install -y --no-install-recommends \
         sudo lsb-release \
         curl git jq unzip \
    && rm -rf /var/lib/apt/lists/*

# Create the actions runner user and home directory
RUN adduser --home /home/runner --disabled-password --gecos "" --uid 1001 runner \
    && groupadd docker --gid 123 \
    && usermod -aG sudo runner \
    && usermod -aG docker runner \
    && echo "%sudo   ALL=(ALL:ALL) NOPASSWD:ALL" > /etc/sudoers \
    && echo "Defaults env_keep += \"DEBIAN_FRONTEND\"" >> /etc/sudoers

WORKDIR /home/runner

# Copy everything from /actions-runner in the build stage as our runner home
COPY --chown=runner:docker --from=build /actions-runner .

USER runner
