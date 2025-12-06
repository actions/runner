#!/bin/bash

set -e

# Script to generate docker-compose configuration for self-hosted runners
# Supports N workers for each repo with a default of 3

# Default values
WORKERS_PER_REPO=3
COMPOSE_FILE="docker-compose.yml"

# Help message
show_help() {
    cat << EOF
Usage: ./gen-compose.sh [OPTIONS]

Generate a docker-compose configuration for GitHub Actions self-hosted runners.

Options:
    -w, --workers NUM       Number of workers per repo (default: 3)
    -o, --output FILE       Output file name (default: docker-compose.yml)
    -h, --help              Display this help message

Examples:
    ./gen-compose.sh                    # Generate with default 3 workers
    ./gen-compose.sh -w 5               # Generate with 5 workers per repo
    ./gen-compose.sh -w 10 -o runners.yml  # Generate with 10 workers, output to runners.yml

EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -w|--workers)
            WORKERS_PER_REPO="$2"
            shift 2
            ;;
        -o|--output)
            COMPOSE_FILE="$2"
            shift 2
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Validate workers parameter
if ! [[ "$WORKERS_PER_REPO" =~ ^[0-9]+$ ]] || [ "$WORKERS_PER_REPO" -lt 1 ]; then
    echo "Error: Workers per repo must be a positive integer"
    exit 1
fi

echo "Generating docker-compose configuration..."
echo "Workers per repo: $WORKERS_PER_REPO"
echo "Output file: $COMPOSE_FILE"

# Generate docker-compose.yml
cat > "$COMPOSE_FILE" << EOF
version: '3.8'

services:
EOF

# Generate service definitions for each worker
for i in $(seq 1 $WORKERS_PER_REPO); do
    cat >> "$COMPOSE_FILE" << EOF
  runner-worker-${i}:
    image: ghcr.io/actions/actions-runner:latest
    container_name: runner-worker-${i}
    environment:
      - RUNNER_NAME=runner-worker-${i}
      - RUNNER_WORKDIR=/home/runner/work
      - RUNNER_ALLOW_RUNASROOT=1
    env_file:
      - .env
    volumes:
      - runner-work-${i}:/home/runner/work
      - /var/run/docker.sock:/var/run/docker.sock
    restart: unless-stopped
    networks:
      - runner-network

EOF
done

# Add volumes section
cat >> "$COMPOSE_FILE" << EOF
volumes:
EOF

for i in $(seq 1 $WORKERS_PER_REPO); do
    cat >> "$COMPOSE_FILE" << EOF
  runner-work-${i}:
EOF
done

# Add networks section
cat >> "$COMPOSE_FILE" << EOF

networks:
  runner-network:
    driver: bridge
EOF

echo "Successfully generated $COMPOSE_FILE with $WORKERS_PER_REPO worker(s)"
echo ""
echo "Next steps:"
echo "1. Create a .env file with required environment variables:"
echo "   - REPO_URL (e.g., https://github.com/owner/repo)"
echo "   - RUNNER_TOKEN (GitHub runner registration token)"
echo "2. Run: docker-compose up -d"
echo ""
