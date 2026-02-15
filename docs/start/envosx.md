

# ![osx](../res/apple_med.png) macOS System Prerequisites

## Supported Versions

Please see "[Supported architectures and operating systems for self-hosted runners](https://docs.github.com/en/actions/reference/runners/self-hosted-runners#macos)."

## Quick Setup

1. **Download** the latest runner from [releases](https://github.com/actions/runner/releases)
2. **Extract** the downloaded archive: `tar xzf actions-runner-osx-x64-*.tar.gz`
3. **Run** `./config.sh` to configure the runner
4. **Install** as a service: `sudo ./svc.sh install` and `sudo ./svc.sh start`

## System Requirements

### macOS Version
- macOS 10.15 (Catalina) or later
- Both Intel (x64) and Apple Silicon (ARM64) are supported

### Required Tools

#### Homebrew (Recommended)
Install Homebrew for easy package management:
```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

#### Development Tools
```bash
# Install Xcode Command Line Tools
xcode-select --install

# Install essential development tools via Homebrew
brew install git curl wget
```

### .NET Runtime
- .NET 6.0 runtime (automatically included with the runner)

## Setup Steps

### 1. Download and Extract
```bash
# Create runner directory
mkdir ~/actions-runner && cd ~/actions-runner

# Download latest release (replace with actual version)
curl -O -L https://github.com/actions/runner/releases/download/v2.xyz.z/actions-runner-osx-x64-2.xyz.z.tar.gz

# Extract
tar xzf ./actions-runner-osx-x64-2.xyz.z.tar.gz
```

### 2. Configure
```bash
./config.sh --url https://github.com/YOUR_ORG/YOUR_REPO --token YOUR_TOKEN
```

### 3. Run as Service (macOS)
```bash
# Install as launchd service
sudo ./svc.sh install

# Start the service
sudo ./svc.sh start

# Check status
sudo ./svc.sh status
```

### 4. Run Interactively (Alternative)
```bash
./run.sh
```

## macOS-Specific Considerations

### Security & Privacy
- Allow the runner executable through macOS Gatekeeper
- Grant necessary permissions in System Preferences > Security & Privacy

### Code Signing
For repositories that build macOS applications:
```bash
# Install certificates for code signing
security import developer_certificates.p12 -k ~/Library/Keychains/login.keychain
```

### Xcode Integration
If building iOS/macOS apps:
```bash
# Install Xcode from App Store or developer portal
# Set Xcode path
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
```

## Troubleshooting

### Common Issues

**Permission Denied:**
```bash
chmod +x ./config.sh ./run.sh ./svc.sh
```

**Gatekeeper Issues:**
```bash
# Allow unsigned binary to run
sudo spctl --master-disable
# Or specifically allow the runner
sudo spctl --add ./bin/Runner.Listener
```

**Service Not Starting:**
```bash
# Check system logs
sudo ./svc.sh status
tail -f ~/Library/Logs/Runner_*.log
```

**Path Issues:**
```bash
# Ensure proper PATH in your shell profile
echo 'export PATH="/usr/local/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc
```

### Getting Help

- Check our [troubleshooting guide](../checks/README.md)
- Review [common network issues](../checks/network.md)
- Search [GitHub Community Discussions](https://github.com/orgs/community/discussions/categories/actions)

## [More .NET Prerequisites Information](https://docs.microsoft.com/en-us/dotnet/core/macos-prerequisites?tabs=netcore30)
