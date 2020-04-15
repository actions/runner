# Automate Configuring Self-Hosted Runner Pools

## Latest Release as Service on Nix VMs

[Run or copy this script for your use](../scripts/create-latest-svc.sh) to automate configuring a runner as a service on Linux or Mac

### Export PAT

Create a GitHub PAT and export it before running the script

```bash
export RUNNER_CFG_PAT=yourPAT
```

### Ceate service one liner

Repo level one liner; replace with yourorg/yourrepo (repo level) or just yourorg (org level) 
```bash
curl https://raw.githubusercontent.com/actions/runner/automate/scripts/create-latest-svc.sh | bash -s yourorg/yourrepo
```

### Uninstall service one liner

Repo level one liner; replace with yourorg/yourrepo (repo level) or just yourorg (org level) 
```bash
curl https://raw.githubusercontent.com/actions/runner/automate/scripts/remove-svc.sh | bash -s yourorg/yourrepo
```
