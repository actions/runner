# GitHub Actions Runner Documentation

Welcome to the GitHub Actions Runner documentation. This guide will help you get started with self-hosted runners, contribute to the project, and troubleshoot common issues.

## 🚀 Getting Started

### Installation and Setup
- **[Linux Prerequisites](start/envlinux.md)** - Complete setup guide for Linux systems
- **[Windows Prerequisites](start/envwin.md)** - Complete setup guide for Windows systems  
- **[macOS Prerequisites](start/envosx.md)** - Complete setup guide for macOS systems

### Quick Start
1. Download the [latest runner release](https://github.com/actions/runner/releases)
2. Follow the platform-specific prerequisites guide above
3. Configure your runner with `./config.sh` (Linux/macOS) or `.\config.cmd` (Windows)
4. Start the runner with `./run.sh` (Linux/macOS) or `.\run.cmd` (Windows)

## 🔧 Administration and Automation

- **[Automation Scripts](automate.md)** - Automate runner deployment and management
- **[Troubleshooting Guides](checks/)** - Common issues and solutions

## 🏗️ Development and Contributing

- **[Contributing Guide](contribute.md)** - Development setup, building, and testing
- **[Architecture Decision Records](adrs/README.md)** - Important architectural decisions and design patterns

## 📋 Reference Materials

### System Checks and Troubleshooting
- **[Network Connectivity](checks/network.md)** - Troubleshoot network issues
- **[Git Configuration](checks/git.md)** - Git-related problems
- **[Actions Troubleshooting](checks/actions.md)** - Action-specific issues
- **[SSL Certificate Issues](checks/sslcert.md)** - Certificate and TLS problems
- **[Node.js Issues](checks/nodejs.md)** - Node.js runtime problems
- **[Internet Connectivity](checks/internet.md)** - General connectivity issues

### Development Resources
- **[Visual Studio Code Setup](contribute/vscode.md)** - IDE configuration for development
- **[Design Documentation](design/)** - Technical design documents

## 🆘 Getting Help

### Community Support
- **[GitHub Community Discussions](https://github.com/orgs/community/discussions/categories/actions)** - Ask questions and get help from the community
- **[GitHub Support](https://support.github.com/contact/bug-report)** - Report critical bugs or get professional support

### Reporting Issues
- **Bug Reports**: Open an issue in this repository
- **Feature Requests**: Use [GitHub Community Discussions](https://github.com/orgs/community/discussions/categories/actions-and-packages)
- **Security Issues**: Follow our [security policy](../security.md)

## 📖 Additional Resources

- **[GitHub Actions Documentation](https://docs.github.com/en/actions)** - Official GitHub Actions documentation
- **[Self-hosted Runners Guide](https://docs.github.com/en/actions/hosting-your-own-runners)** - GitHub's official self-hosted runner documentation
- **[Runner Releases](https://github.com/actions/runner/releases)** - Download the latest runner versions

---

> **Note**: This documentation is maintained by the GitHub Actions team and the community. If you find any issues or have suggestions for improvement, please open an issue or contribute a pull request.