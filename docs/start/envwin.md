# ![win](../res/win_med.png) Windows System Prerequisites

## Supported Versions

Please see "[Supported architectures and operating systems for self-hosted runners](https://docs.github.com/en/actions/reference/runners/self-hosted-runners#windows)."

## Quick Setup

1. **Download** the latest runner from [releases](https://github.com/actions/runner/releases)
2. **Extract** the downloaded archive to your desired directory
3. **Run** `config.cmd` as Administrator to configure the runner
4. **Install** as a service (optional): `svc.sh install` and `svc.sh start`

## System Requirements

### .NET Runtime
- .NET 6.0 runtime (automatically installed with the runner)
- Windows PowerShell 5.1 or PowerShell Core 6.0+

### Windows Features
Windows runners require the following components:

```powershell
# Enable required Windows features (run as Administrator)
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux
```

### Visual Studio Build Tools (For builds requiring compilation)
For repositories that need to compile code, install:

- **Visual Studio 2017 or newer** [Install here](https://visualstudio.microsoft.com)
- **Visual Studio 2022 17.3 Preview or later** (for ARM64) [Install here](https://docs.microsoft.com/en-us/visualstudio/releases/2022/release-notes-preview)

### Git for Windows
- **Git for Windows** [Install here](https://git-scm.com/downloads) (required for repository operations)

## Common Setup Steps

### 1. Create Runner Directory
```cmd
mkdir C:\actions-runner
cd C:\actions-runner
```

### 2. Download and Extract
```powershell
# Download latest release
Invoke-WebRequest -Uri "https://github.com/actions/runner/releases/download/v2.xyz.z/actions-runner-win-x64-2.xyz.z.zip" -OutFile "actions-runner.zip"
# Extract
Expand-Archive -Path "actions-runner.zip" -DestinationPath "."
```

### 3. Configure
```cmd
.\config.cmd --url https://github.com/YOUR_ORG/YOUR_REPO --token YOUR_TOKEN
```

### 4. Run as Service
```cmd
# Install service
.\svc.sh install
# Start service  
.\svc.sh start
```

## Troubleshooting

### Common Issues

**PowerShell Execution Policy:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Windows Defender/Antivirus:**
- Add runner directory to antivirus exclusions
- Exclude `Runner.Listener.exe` and `Runner.Worker.exe`

**Firewall Issues:**
```powershell
# Allow runner through Windows Firewall
New-NetFirewallRule -DisplayName "GitHub Actions Runner" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

**Permission Issues:**
- Run `config.cmd` as Administrator
- Ensure the runner user has "Log on as a service" rights

### Getting Help

- Check our [troubleshooting guide](../checks/README.md)
- Review [common issues](../checks/actions.md)
- Search [GitHub Community Discussions](https://github.com/orgs/community/discussions/categories/actions)

## [More .NET Prerequisites Information](https://docs.microsoft.com/en-us/dotnet/core/windows-prerequisites?tabs=netcore30)
