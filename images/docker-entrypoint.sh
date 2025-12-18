#!/usr/bin/env bash
set -euo pipefail

# 可通过环境变量覆盖存储驱动，默认用 vfs，更兼容 DinD 环境
STORAGE_DRIVER="${DOCKER_STORAGE_DRIVER:-vfs}"

# 启动 docker daemon（用 sudo 提权到 root）
if ! pgrep dockerd >/dev/null 2>&1; then
  sudo dockerd --host=unix:///var/run/docker.sock --storage-driver="${STORAGE_DRIVER}" &
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