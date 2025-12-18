#!/usr/bin/env bash
set -euo pipefail

# 启动 docker daemon（用 sudo 提权到 root）
if ! pgrep dockerd >/dev/null 2>&1; then
  sudo dockerd --host=unix:///var/run/docker.sock --storage-driver=overlay2 &
fi

# 等待 dockerd 就绪
tries=0
until docker info >/dev/null 2>&1; do
  tries=$((tries+1))
  if [ "$tries" -gt 30 ]; then
    echo "Docker daemon failed to start" >&2
    exit 1
  fi
  sleep 1
done

echo "Docker daemon is up."

exec "$@"