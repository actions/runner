FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-buster-slim

ENV GITHUB_PAT=""
ENV GITHUB_RUNNER_SCOPE=""
ENV GITHUB_SERVER_URL=""
ENV GITHUB_API_URL=""
ENV K8S_HOST_IP=""

RUN apt-get update --fix-missing \
    && apt-get install -y --no-install-recommends \
    curl \
    jq \
    apt-utils \
    apt-transport-https \
    unzip \
    net-tools\
    gnupg2\
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install kubectl
RUN curl -s https://packages.cloud.google.com/apt/doc/apt-key.gpg | apt-key add - && \
    echo "deb https://apt.kubernetes.io/ kubernetes-xenial main" | tee -a /etc/apt/sources.list.d/kubernetes.list && \
    apt-get update && apt-get -y install --no-install-recommends kubectl

# Install docker
RUN curl -fsSL https://get.docker.com -o get-docker.sh
RUN sh get-docker.sh

# Allow runner to run as root
ENV RUNNER_ALLOW_RUNASROOT=1
# Directory for runner to operate in
RUN mkdir /actions-runner
WORKDIR /actions-runner
COPY ./src/Misc/download-runner.sh /actions-runner/download-runner.sh
COPY ./src/Misc/entrypoint.sh /actions-runner/entrypoint.sh
COPY ./src/Misc/jobstart.sh /actions-runner/jobstart.sh
COPY ./src/Misc/jobrunning.sh /actions-runner/jobrunning.sh
COPY ./src/Misc/jobcomplete.sh /actions-runner/jobcomplete.sh

RUN /actions-runner/download-runner.sh
RUN rm -f /actions-runner/download-runner.sh

ENV _INTERNAL_JOBSTART_NOTIFICATION=/actions-runner/jobstart.sh
ENV _INTERNAL_JOBRUNNING_NOTIFICATION=/actions-runner/jobrunning.sh
ENV _INTERNAL_JOBCOMPLETE_NOTIFICATION=/actions-runner/jobcomplete.sh

ENTRYPOINT ["./entrypoint.sh"] 