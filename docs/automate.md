# Automate Configuring Self-Hosted Runner Pools

## Latest Release as Service on Nix VMs

[Run or copy this script for your use](../scripts/latest-svc.sh) to automate configuring a runner as a service on Linux or Mac

### Step 1: export PAT

Create a GitHub PAT and export it before running the script

```bash
export RUNNER_CFG_PAT=yourPAT
```

### Step 2: config one liner

Repo level one liner; replace yourorg/yourrepo
```bash
curl https://raw.githubusercontent.com/actions/runner/automate/scripts/latest-svc.sh | bash -s yourorg/yourrepo
```

Org level one liner; replace yourorg

```bash
curl https://raw.githubusercontent.com/actions/runner/automate/scripts/latest-svc.sh | bash -s yourorg
```
