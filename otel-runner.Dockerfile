# Build an ARC-compatible runner image carrying the native-OTel runner
# (actions/runner#4366). We build the linux layout from source, then overlay it
# onto the official actions-runner image (keeping its runner user, k8s hooks,
# and entrypoint).
#
#   docker build -f otel-runner.Dockerfile -t otel-runner:dev .
#   kind load docker-image otel-runner:dev --name gha-runner

# ---- builder: compile the runner layout from source (linux/arm64 native) ----
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS builder
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl git unzip ca-certificates \
 && rm -rf /var/lib/apt/lists/*
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
WORKDIR /runner
COPY . .
WORKDIR /runner/src
# dev.sh bootstraps its own pinned dotnet SDK and downloads externals (node).
RUN ./dev.sh layout Release

# ---- runtime: official ARC runner image + our instrumented binaries ----
FROM ghcr.io/actions/actions-runner:latest
# Merge the source-built layout over the official runner home. This replaces
# bin/externals/run.sh/config.sh/env.sh with our OTel build while keeping the
# official image's k8s hooks, run-helper.sh, safe_sleep.sh, and entrypoint.
COPY --from=builder --chown=runner:runner /runner/_layout/. /home/runner/

# Bake the runner launch command into the image. ARC normally sets
# `command: [/home/runner/run.sh]` on the pod, but doing so in chart values
# triggers an "Outdated" hash-reconcile loop in gha-runner-scale-set 0.14.1.
# Baking it here lets the values stay command-free (loop-safe).
WORKDIR /home/runner
CMD ["/home/runner/run.sh"]
