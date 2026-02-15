# Automate Configuring Self-Hosted Runners


## Export PAT

Before running any of these sample scripts, create a GitHub PAT and export it before running the script

```bash
export RUNNER_CFG_PAT=yourPAT
```

## Create running as a service

**Scenario**: Run on a machine or VM ([not container](#why-cant-i-use-a-container)) which automates:

 - Resolving latest released runner
 - Download and extract latest
 - Acquire a registration token
 - Configure the runner
 - Run as a systemd (linux) or Launchd (osx) service

:point_right: [Sample script here](../scripts/create-latest-svc.sh) :point_left:

Run as a one-liner. NOTE: replace with yourorg/yourrepo (repo level) or just yourorg (org level)
```bash
curl -s https://raw.githubusercontent.com/actions/runner/main/scripts/create-latest-svc.sh | bash -s yourorg/yourrepo
```

You can call the script with additional arguments:
```bash
#   Usage:
#       export RUNNER_CFG_PAT=<yourPAT>
#       ./create-latest-svc -s scope -g [ghe_domain] -n [name] -u [user] -l [labels]
#       -s          required  scope: repo (:owner/:repo) or org (:organization)
#       -g          optional  ghe_hostname: the fully qualified domain name of your GitHub Enterprise Server deployment
#       -n          optional  name of the runner, defaults to hostname
#       -u          optional  user svc will run as, defaults to current
#       -l          optional  list of labels (split by comma) applied on the runner"
```

Use `--` to pass any number of optional named parameters:

```
curl -s https://raw.githubusercontent.com/actions/runner/main/scripts/create-latest-svc.sh | bash -s -- -s myorg/myrepo -n myname -l label1,label2
```
### Why can't I use a container?

The runner is installed as a service using `systemd` and `systemctl`. Docker does not support `systemd` for service configuration on a container.

## Uninstall running as service

**Scenario**: Run on a machine or VM ([not container](#why-cant-i-use-a-container)) which automates:

 - Stops and uninstalls the systemd (linux) or Launchd (osx) service
 - Acquires a removal token
 - Removes the runner

:point_right: [Sample script here](../scripts/remove-svc.sh) :point_left:

Repo level one liner.  NOTE: replace with yourorg/yourrepo (repo level) or just yourorg (org level)
```bash
curl -s https://raw.githubusercontent.com/actions/runner/main/scripts/remove-svc.sh | bash -s yourorg/yourrepo
```

### Delete an offline runner

**Scenario**: Deletes a registered runner that is offline:

 - Ensures the runner is offline
 - Resolves id from name
 - Deletes the runner

:point_right: [Sample script here](../scripts/delete.sh) :point_left:

Repo level one-liner.  NOTE: replace with yourorg/yourrepo (repo level) or just yourorg (org level) and replace runnername
```bash
curl -s https://raw.githubusercontent.com/actions/runner/main/scripts/delete.sh | bash -s yourorg/yourrepo runnername
```

## Troubleshooting

### Common Issues

#### Permission Denied
```bash
# Ensure scripts have execute permissions
chmod +x ./config.sh ./run.sh
```

#### PAT Token Issues
```bash
# Verify your PAT has the correct scopes:
# - repo (for repository-level runners)
# - admin:org (for organization-level runners)
export RUNNER_CFG_PAT=your_token_here
echo $RUNNER_CFG_PAT  # Verify it's set
```

#### Network Connectivity
```bash
# Test GitHub connectivity
curl -H "Authorization: token $RUNNER_CFG_PAT" https://api.github.com/user

# For GitHub Enterprise Server
curl -H "Authorization: token $RUNNER_CFG_PAT" https://your-github-enterprise/api/v3/user
```

#### Service Installation Fails
```bash
# Check if running as appropriate user
whoami

# For Linux - ensure systemd is available
systemctl --version

# For macOS - ensure launchd is available
launchctl version
```

#### Runner Registration Fails
```bash
# Check if runner already exists
curl -H "Authorization: token $RUNNER_CFG_PAT" \
  "https://api.github.com/repos/OWNER/REPO/actions/runners"

# Remove existing runner if needed
./config.sh remove --token $RUNNER_CFG_PAT
```

### Getting Help

- **Configuration Issues**: Check the [Prerequisites](start/envlinux.md) for your platform
- **Network Problems**: Review [network troubleshooting guide](checks/network.md)
- **General Support**: Visit [GitHub Community Discussions](https://github.com/orgs/community/discussions/categories/actions)

### Advanced Examples

#### Organization-level Runner with Custom Labels
```bash
export RUNNER_CFG_PAT=your_org_pat
curl -s https://raw.githubusercontent.com/actions/runner/main/scripts/create-latest-svc.sh | \
  bash -s -- -s myorg -n prod-runner-1 -l production,linux,docker
```

#### Repository-level Runner for GitHub Enterprise
```bash
export RUNNER_CFG_PAT=your_ghe_pat
curl -s https://raw.githubusercontent.com/actions/runner/main/scripts/create-latest-svc.sh | \
  bash -s -- -s myorg/myrepo -g github.company.com -n build-server -u builder
```
```
