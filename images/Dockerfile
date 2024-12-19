# Source: https://github.com/dotnet/dotnet-docker
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-noble as build

ARG BUILDX_VERSION=0.19.1
ARG DOCKER_VERSION=27.3.1
ARG GITHUB_SHA
ARG NODE_MAJOR=20
ARG NUGET_TOKEN
ARG RUNNER_CONTAINER_HOOKS_VERSION=0.6.1
ARG RUNNER_VERSION
ARG TARGETARCH
ARG TARGETOS

ENV DOCKER_CLI_EXPERIMENTAL=enabled
ENV GITHUB_SHA="${GITHUB_SHA}"
ENV RUNNER_NAME=""
ENV RUNNER_TOKEN=""

### START CUSTOM SETUP ###

WORKDIR /tmp

# Update the base image
RUN --mount=type=cache,target=/var/cache/apt \
  sudo apt-get update \
  && sudo apt-get upgrade -y

# Install Base packages and dependencies
RUN --mount=type=cache,target=/var/cache/apt \
  sudo apt-get install -y \
  apt-transport-https \
  build-essential \
  ca-certificates \
  curl \
  dos2unix \
  gettext \
  gnupg \
  jq \
  libbz2-dev \
  libcurl4-openssl-dev \
  libffi-dev \
  liblzma-dev \
  libncursesw5-dev \
  libreadline-dev \
  libsqlite3-dev \
  libssl-dev \
  libssl-dev \
  libu2f-udev \
  libxml2-dev \
  libxmlsec1-dev \
  llvm \
  make \
  python3-pip \
  rsync \
  software-properties-common \
  sshpass \
  tk-dev \
  vim \
  wget \
  xvfb \
  xz-utils \
  zlib1g-dev

# Install Java 17
RUN --mount=type=cache,target=/var/cache/apt \
  sudo apt-get install -y openjdk-17-jdk

# Install Docker and remove Docker-Compose
RUN curl -fL https://download.docker.com/linux/static/stable/x86_64/docker-${DOCKER_VERSION}.tgz | tar zxvf - \
  && sudo install -o root -g root -m 755 docker/* /usr/bin/ \
  && sudo mkdir -p /usr/local/lib/docker/cli-plugins \
  && sudo curl -fLo /usr/local/lib/docker/cli-plugins/docker-buildx "https://github.com/docker/buildx/releases/download/v${BUILDX_VERSION}/buildx-v${BUILDX_VERSION}.linux-amd64" \
  && sudo chmod +x /usr/local/lib/docker/cli-plugins/docker-buildx \
  && rm -rf docker/ \
  && sudo rm /usr/libexec/docker/cli-plugins/docker-compose

# Install `yq`
RUN sudo wget -qO /usr/local/bin/yq https://github.com/mikefarah/yq/releases/latest/download/yq_linux_amd64 \
  && sudo chmod a+x /usr/local/bin/yq

# Install Helm
RUN --mount=type=cache,target=/var/cache/apt \
  curl https://baltocdn.com/helm/signing.asc | gpg --dearmor | sudo tee /usr/share/keyrings/helm.gpg > /dev/null \
  && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/helm.gpg] https://baltocdn.com/helm/stable/debian/ all main" | sudo tee /etc/apt/sources.list.d/helm-stable-debian.list \
  && sudo apt-get update \
  && sudo apt-get install -y helm \
  && helm version

# Install Nodejs and CDK
RUN --mount=type=cache,target=/var/cache/apt \
  sudo mkdir -p /etc/apt/keyrings \
  && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | sudo gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
  && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_${NODE_MAJOR}.x nodistro main" | sudo tee /etc/apt/sources.list.d/nodesource.list \
  && sudo apt-get update \
  && sudo apt-get install nodejs -y \
  && sudo npm install -g npm \
  && sudo npm install -g aws-cdk \
  && sudo rm -fr /runnertmp/externals*/node1*

# Manually update the IP package
RUN sudo mkdir /nodetmp \
  && cd /nodetmp \
  && sudo npm install ip \
  && sudo rm -fr /runnertmp/externals/node20/lib/node_modules/npm/node_modules/ip \
  && sudo mv node_modules/ip /runnertmp/externals/node20/lib/node_modules/npm/node_modules/ \
  && cd \
  && sudo rm -fr /nodetmp \
  && sudo chown runner:docker -R /runnertmp/externals/node20/lib/node_modules/npm/node_modules/

# Install AWS CLI
RUN curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip" \
  && unzip awscliv2.zip \
  && rm awscliv2.zip \
  && sudo ./aws/install \
  && rm -rf ./aws

# Install Postgresql
RUN --mount=type=cache,target=/var/cache/apt \
  sudo wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add - \
  && echo "deb http://apt.postgresql.org/pub/repos/apt/ $(lsb_release -cs)-pgdg main" | sudo tee /etc/apt/sources.list.d/postgresql-pgdg.list > /dev/null \
  && sudo apt-get -y update \
  && sudo apt-get -y install postgresql-14

# Install .NET
RUN --mount=type=cache,target=/var/cache/apt \
  sudo add-apt-repository ppa:dotnet/backports \
  && sudo apt update \
  && sudo apt-get install -y \
  dotnet-sdk-6.0 \
  dotnet-sdk-7.0 \
  dotnet-sdk-8.0 \
  dotnet-sdk-9.0

# Apply PatriotSoftware NuGet token
# TODO: Can we pass this as a runtime env var? https://learn.microsoft.com/en-us/nuget/consume-packages/consuming-packages-authenticated-feeds#credentials-in-environment-variables
RUN dotnet nuget add source -n psi -u "aws-gittoken" -p "$NUGET_TOKEN" https://nuget.pkg.github.com/SynergyDataSystems/index.json --store-password-in-clear-text

# Install JetBrains CLI tool
RUN dotnet tool install -g JetBrains.ReSharper.GlobalTools \
  && echo 'export PATH="$PATH:/home/runner/.dotnet/tools"' >> ~/.bashrc

# Add Powershell
RUN --mount=type=cache,target=/var/cache/apt \
  . /etc/os-release \
  && sudo wget -q https://packages.microsoft.com/config/ubuntu/${VERSION_ID}/packages-microsoft-prod.deb \
  && sudo dpkg -i packages-microsoft-prod.deb \
  && sudo rm packages-microsoft-prod.deb \
  && sudo apt-get update \
  && sudo apt-get install -y powershell \
  && echo '$env:PATH += ":/home/runner/.dotnet/tools"' | sudo tee -a /opt/microsoft/powershell/7/profile.ps1

# Install Edge -- Added to the top because MS is sometimes flakey, so we fail fast
RUN curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
RUN --mount=type=cache,target=/var/cache/apt \
  sudo install -o root -g root -m 644 microsoft.gpg /etc/apt/trusted.gpg.d/ \
  && sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/edge stable main" > /etc/apt/sources.list.d/microsoft-edge-beta.list' \
  && sudo rm microsoft.gpg \
  && sudo apt-get update \
  && sudo apt-get install -y microsoft-edge-beta

# Install Chrome
RUN wget https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb \
  && sudo dpkg -i google-chrome-stable_current_amd64.deb \
  && rm ./google-chrome-stable_current_amd64.deb

# Install Firefox
RUN --mount=type=cache,target=/var/cache/apt \
  sudo install -d -m 0755 /etc/apt/keyrings \
  && wget -q https://packages.mozilla.org/apt/repo-signing-key.gpg -O- | sudo tee /etc/apt/keyrings/packages.mozilla.org.asc > /dev/null \
  && echo 'deb [signed-by=/etc/apt/keyrings/packages.mozilla.org.asc] https://packages.mozilla.org/apt mozilla main' | sudo tee -a /etc/apt/sources.list.d/mozilla.list > /dev/null \
  && echo 'Package: *\nPin: origin packages.mozilla.org \nPin-Priority: 1000 \n' | sudo tee /etc/apt/preferences.d/mozilla \
  && cat /etc/apt/preferences.d/mozilla \
  && sudo apt-get update  \
  && sudo apt-get install firefox -y

# Install Kubectl
RUN curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl" \
  && sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl \
  && rm kubectl

# Install Python and Poetry
RUN sudo apt-get install -y python3 python3-poetry \
  && sudo ln -s /usr/bin/python3 /usr/bin/python \
  && curl https://pyenv.run | bash \
  && export PATH="$HOME/.pyenv/bin:$PATH" \
  && eval "$(pyenv init --path)" \
  && eval "$(pyenv virtualenv-init -)" \
  && exec $SHELL

# Configure Pyenv
RUN echo 'export PATH="$HOME/.pyenv/bin:$PATH"' >> ~/.bashrc \
  && echo 'eval "$(pyenv init --path)"' >> ~/.bashrc \
  && echo 'eval "$(pyenv virtualenv-init -)"' >> ~/.bashrc

# Install Packer
RUN --mount=type=cache,target=/var/cache/apt \
  curl -fsSL https://apt.releases.hashicorp.com/gpg | sudo apt-key add - \
  && sudo apt-add-repository "deb [arch=amd64] https://apt.releases.hashicorp.com $(lsb_release -cs) main" \
  && sudo apt-get update  \
  && sudo apt-get install -y packer \
  && packer plugins install github.com/hashicorp/amazon

COPY --chmod=0755 LinuxRunners/dnd-true.sh /etc/arc/hooks/job-started.d/
COPY --chmod=0755 LinuxRunners/dnd-false.sh /etc/arc/hooks/job-completed.d/

### END CUSTOM SETUP ###

RUN apt update -y && apt install curl unzip -y

WORKDIR /actions-runner
RUN export RUNNER_ARCH=${TARGETARCH} \
    && if [ "$RUNNER_ARCH" = "amd64" ]; then export RUNNER_ARCH=x64 ; fi \
    && curl -f -L -o runner.tar.gz https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-${TARGETOS}-${RUNNER_ARCH}-${RUNNER_VERSION}.tar.gz \
    && tar xzf ./runner.tar.gz \
    && rm runner.tar.gz

RUN curl -f -L -o runner-container-hooks.zip https://github.com/actions/runner-container-hooks/releases/download/v${RUNNER_CONTAINER_HOOKS_VERSION}/actions-runner-hooks-k8s-${RUNNER_CONTAINER_HOOKS_VERSION}.zip \
    && unzip ./runner-container-hooks.zip -d ./k8s \
    && rm runner-container-hooks.zip

RUN export RUNNER_ARCH=${TARGETARCH} \
    && if [ "$RUNNER_ARCH" = "amd64" ]; then export DOCKER_ARCH=x86_64 ; fi \
    && if [ "$RUNNER_ARCH" = "arm64" ]; then export DOCKER_ARCH=aarch64 ; fi \
    && curl -fLo docker.tgz https://download.docker.com/${TARGETOS}/static/stable/${DOCKER_ARCH}/docker-${DOCKER_VERSION}.tgz \
    && tar zxvf docker.tgz \
    && rm -rf docker.tgz \
    && mkdir -p /usr/local/lib/docker/cli-plugins \
    && curl -fLo /usr/local/lib/docker/cli-plugins/docker-buildx \
        "https://github.com/docker/buildx/releases/download/v${BUILDX_VERSION}/buildx-v${BUILDX_VERSION}.linux-${TARGETARCH}" \
    && chmod +x /usr/local/lib/docker/cli-plugins/docker-buildx

FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-noble

ENV DEBIAN_FRONTEND=noninteractive
ENV RUNNER_MANUALLY_TRAP_SIG=1
ENV ACTIONS_RUNNER_PRINT_LOG_TO_STDOUT=1
ENV ImageOS=ubuntu24

# 'gpg-agent' and 'software-properties-common' are needed for the 'add-apt-repository' command that follows
RUN apt update -y \
    && apt install -y --no-install-recommends sudo lsb-release gpg-agent software-properties-common curl jq unzip \
    && rm -rf /var/lib/apt/lists/*

# Configure git-core/ppa based on guidance here:  https://git-scm.com/download/linux
RUN add-apt-repository ppa:git-core/ppa \
    && apt update -y \
    && apt install -y git \
    && rm -rf /var/lib/apt/lists/*

RUN adduser --disabled-password --gecos "" --uid 1001 runner \
    && groupadd docker --gid 123 \
    && usermod -aG sudo runner \
    && usermod -aG docker runner \
    && echo "%sudo   ALL=(ALL:ALL) NOPASSWD:ALL" > /etc/sudoers \
    && echo "Defaults env_keep += \"DEBIAN_FRONTEND\"" >> /etc/sudoers

WORKDIR /home/runner

COPY --chown=runner:docker --from=build /actions-runner .
COPY --from=build /usr/local/lib/docker/cli-plugins/docker-buildx /usr/local/lib/docker/cli-plugins/docker-buildx

RUN install -o root -g root -m 755 docker/* /usr/bin/ && rm -rf docker

USER runner
