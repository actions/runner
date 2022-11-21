FROM mcr.microsoft.com/dotnet/runtime-deps:6.0 as build

ARG RUNNER_VERSION
ARG RUNNER_ARCH="x64"
ARG RUNNER_CONTAINER_HOOKS_VERSION=0.1.3

RUN apt update -y && apt install curl unzip -y

WORKDIR /actions-runner
RUN curl -f -L -o runner.tar.gz https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-${RUNNER_ARCH}-${RUNNER_VERSION}.tar.gz \
    && tar xzf ./runner.tar.gz \
    && rm runner.tar.gz

RUN curl -f -L -o runner-container-hooks.zip https://github.com/actions/runner-container-hooks/releases/download/v${RUNNER_CONTAINER_HOOKS_VERSION}/actions-runner-hooks-k8s-${RUNNER_CONTAINER_HOOKS_VERSION}.zip \
    && unzip ./runner-container-hooks.zip -d ./k8s \
    && rm runner-container-hooks.zip

FROM mcr.microsoft.com/dotnet/runtime-deps:6.0

ENV RUNNER_ALLOW_RUNASROOT=1

WORKDIR /actions-runner
COPY --from=build /actions-runner .
