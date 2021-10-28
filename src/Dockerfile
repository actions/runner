FROM mcr.microsoft.com/dotnet/sdk:3.1 AS Build

# ENV RUNNER_CONFIG_URL=""
# ENV GITHUB_PAT=""
# ENV RUNNER_NAME=""
# ENV RUNNER_GROUP=""
# ENV RUNNER_LABELS=""
# ENV GITHUB_RUNNER_SCOPE=""
# ENV GITHUB_SERVER_URL=""
# ENV GITHUB_API_URL=""
# ENV K8S_HOST_IP=""

RUN apt-get update --fix-missing \
    && apt-get install -y --no-install-recommends \
    curl \
    # jq \
    # git \
    apt-utils \
    apt-transport-https \
    unzip \
    net-tools\
    gnupg2\
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install kubectl
# RUN curl -s https://packages.cloud.google.com/apt/doc/apt-key.gpg | apt-key add - && \
#     echo "deb https://apt.kubernetes.io/ kubernetes-xenial main" | tee -a /etc/apt/sources.list.d/kubernetes.list && \
#     apt-get update && apt-get -y install --no-install-recommends kubectl

# Install docker
# RUN curl -fsSL https://get.docker.com -o get-docker.sh
# RUN sh get-docker.sh

# Allow runner to run as root
# ENV RUNNER_ALLOW_RUNASROOT=1

# Directory for runner to operate in
RUN mkdir /actions-runner
RUN mkdir /actions-runner/src
WORKDIR /actions-runner/src

COPY ./ /actions-runner/src

RUN /actions-runner/src/dev.sh l

FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1

ENV RUNNER_CONFIG_URL=""
ENV GITHUB_PAT=""

RUN apt-get update --fix-missing \
    && apt-get install -y --no-install-recommends \
    curl \
    # jq \
    # git \
    # apt-utils \
    # apt-transport-https \
    # unzip \
    # net-tools\
    gnupg2\
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install kubectl
RUN curl -s https://packages.cloud.google.com/apt/doc/apt-key.gpg | apt-key add - && \
    echo "deb https://apt.kubernetes.io/ kubernetes-xenial main" | tee -a /etc/apt/sources.list.d/kubernetes.list && \
    apt-get update && apt-get -y install --no-install-recommends kubectl


# Allow runner to run as root
ENV RUNNER_ALLOW_RUNASROOT=1

# Directory for runner to operate in
RUN mkdir /actions-runner
WORKDIR /actions-runner
COPY --from=Build /actions-runner/_layout /actions-runner
ENTRYPOINT ["./entrypoint.sh"] 
