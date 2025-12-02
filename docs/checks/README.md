# Troubleshooting Guides

This directory contains troubleshooting guides for common issues you might encounter when setting up or running GitHub Actions self-hosted runners.

## Quick Reference

| Issue Type | Guide | Description |
|------------|-------|-------------|
| 🌐 **Network** | [network.md](network.md) | Connection issues, proxy, firewall problems |
| 🔒 **SSL/TLS** | [sslcert.md](sslcert.md) | Certificate and TLS handshake issues |
| 📦 **Git** | [git.md](git.md) | Git configuration and repository access |
| ⚡ **Actions** | [actions.md](actions.md) | Action-specific runtime issues |
| 🟢 **Node.js** | [nodejs.md](nodejs.md) | Node.js runtime and npm issues |
| 🌍 **Internet** | [internet.md](internet.md) | General internet connectivity |

## Common First Steps

Before diving into specific guides, try these general troubleshooting steps:

### 1. Check Basic Connectivity
```bash
# Test GitHub API access
curl -I https://api.github.com/

# For GitHub Enterprise Server
curl -I https://your-github-enterprise.com/api/v3/
```

### 2. Verify Runner Status
```bash
# Check if runner service is running
./svc.sh status

# View recent logs
tail -f _diag/Runner_*.log
```

### 3. Test Runner Configuration
```bash
# Re-run configuration
./config.sh

# Test connection without running
./run.sh --check
```

## Getting Additional Help

If these guides don't resolve your issue:

1. **Search existing issues** in the [runner repository](https://github.com/actions/runner/issues)
2. **Check GitHub Status** at [githubstatus.com](https://githubstatus.com)
3. **Ask the community** in [GitHub Community Discussions](https://github.com/orgs/community/discussions/categories/actions)
4. **Contact support** for critical issues via [GitHub Support](https://support.github.com/contact)

## Contributing

Found a solution to a common problem not covered here? Consider contributing:

1. Create a new `.md` file for the issue type
2. Follow the format of existing guides
3. Submit a pull request with your improvements

---

💡 **Tip**: Always check the `_diag/` directory for detailed log files when troubleshooting issues.