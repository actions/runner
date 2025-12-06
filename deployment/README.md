# Deployment Scripts

This directory contains scripts for deploying GitHub Actions self-hosted runners.

## gen-compose.sh

Generate docker-compose configurations for deploying multiple GitHub Actions runner workers.

### Quick Start

```bash
# Generate with default 3 workers
./gen-compose.sh

# Generate with custom number of workers
./gen-compose.sh -w 5

# Specify custom output file
./gen-compose.sh -w 10 -o my-runners.yml
```

### Usage

```
./gen-compose.sh [OPTIONS]

Options:
  -w, --workers NUM    Number of workers per repo (default: 3)
  -o, --output FILE    Output file name (default: docker-compose.yml)
  -h, --help           Display help message
```

### Examples

#### Default Configuration
```bash
./gen-compose.sh
```
Generates `docker-compose.yml` with 3 runner workers.

#### Custom Worker Count
```bash
./gen-compose.sh -w 5
```
Generates `docker-compose.yml` with 5 runner workers.

#### Custom Output File
```bash
./gen-compose.sh -w 10 -o production-runners.yml
```
Generates `production-runners.yml` with 10 runner workers.

### Setup and Deployment

After generating the docker-compose file:

1. **Create a `.env` file** in the same directory with the following variables:
   ```env
   REPO_URL=https://github.com/your-org/your-repo
   RUNNER_TOKEN=your_github_runner_registration_token
   ```

2. **Start the runners**:
   ```bash
   docker-compose up -d
   ```

3. **Check runner status**:
   ```bash
   docker-compose ps
   ```

4. **View runner logs**:
   ```bash
   docker-compose logs -f runner-worker-1
   ```

### Getting a Runner Token

To get a registration token for your runners:

1. Navigate to your repository settings
2. Go to **Actions** → **Runners**
3. Click **New self-hosted runner**
4. Copy the token from the configuration command

Or use the GitHub API:
```bash
# For a repository
curl -X POST \
  -H "Authorization: token YOUR_PAT" \
  https://api.github.com/repos/OWNER/REPO/actions/runners/registration-token

# For an organization
curl -X POST \
  -H "Authorization: token YOUR_PAT" \
  https://api.github.com/orgs/ORG/actions/runners/registration-token
```

### Configuration Details

Each runner worker includes:
- Unique container name (`runner-worker-1`, `runner-worker-2`, etc.)
- Dedicated work volume for isolation
- Docker socket access for running containerized actions
- Auto-restart policy
- Shared network for communication

### Scaling

To scale up or down:

1. Generate a new configuration with desired worker count:
   ```bash
   ./gen-compose.sh -w 10
   ```

2. Apply the changes:
   ```bash
   docker-compose up -d
   ```

Docker Compose will automatically add new workers or remove excess ones.

### Troubleshooting

**Issue**: Workers not registering with GitHub
- **Solution**: Verify `REPO_URL` and `RUNNER_TOKEN` in `.env` file

**Issue**: Permission errors
- **Solution**: Ensure script is executable: `chmod +x gen-compose.sh`

**Issue**: Docker socket permission denied
- **Solution**: Ensure Docker daemon is running and user has Docker permissions

### Requirements

- Docker Engine 20.10+
- Docker Compose 1.29+
- Valid GitHub PAT with `repo` or `admin:org` scope

### Notes

- Runner tokens expire after 1 hour
- Each worker runs independently and can execute jobs concurrently
- Workers share the same network but have isolated file systems
- Default configuration uses the latest runner image from ghcr.io

### Related Documentation

- [GitHub Actions Self-Hosted Runners](https://docs.github.com/en/actions/hosting-your-own-runners)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Actions Runner Image](https://github.com/actions/runner)
