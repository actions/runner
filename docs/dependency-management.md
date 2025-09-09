# Runner Dependency Management Process

## Overview

This document outlines the automated dependency management process for the GitHub Actions Runner, designed to ensure we maintain up-to-date and secure dependencies while providing predictable release cycles.

## Release Schedule

- **Monthly Runner Releases**: New runner versions are released monthly
- **Weekly Dependency Checks**: Automated workflows check for dependency updates every Monday
- **Security Patches**: Critical security vulnerabilities are addressed immediately outside the regular schedule

## Automated Workflows

**Note**: These workflows are implemented across separate PRs for easier review and independent deployment. Each workflow includes comprehensive error handling and security-focused vulnerability detection.

### 1. Foundation Labels
- **Workflow**: `.github/workflows/setup-labels.yml` (PR #4024)
- **Purpose**: Creates consistent dependency labels for all automation workflows
- **Labels**: `dependency`, `security`, `typescript`, `needs-manual-review`
- **Prerequisite**: Must be merged before other workflows for proper labeling

### 2. Node.js Version Updates
- **Workflow**: `.github/workflows/node-upgrade.yml`
- **Schedule**: Mondays at 6:00 AM UTC
- **Purpose**: Updates Node.js 20 and 24 versions in `src/Misc/externals.sh`
- **Source**: [actions/node-versions](https://github.com/actions/node-versions)
- **Priority**: First (NPM depends on current Node.js versions)

### 3. NPM Security Audit
- **Workflow**: `.github/workflows/npm-audit-ts-fix.yml`
- **Schedule**: Mondays at 7:00 AM UTC
- **Purpose**: Security vulnerability detection and TypeScript auto-repair
- **Location**: `src/Misc/expressionFunc/hashFiles/`
- **Features**: Graduated security response, auto-fix TypeScript compatibility
- **Dependency**: Runs after Node.js updates for optimal compatibility

### 4. .NET SDK Updates
- **Workflow**: `.github/workflows/dotnet-upgrade.yml`
- **Schedule**: Mondays at 8:00 AM UTC
- **Purpose**: Updates .NET SDK and package versions with build validation
- **Features**: Global.json updates, NuGet package management, compatibility checking
- **Independence**: Runs independently of Node.js/NPM updates

### 5. Docker/Buildx Updates
- **Workflow**: `.github/workflows/docker-buildx-upgrade.yml`
- **Schedule**: Mondays at 9:00 AM UTC  
- **Purpose**: Updates Docker and Docker Buildx versions with multi-platform validation
- **Features**: Container security scanning, multi-architecture build testing
- **Independence**: Runs independently of other dependency updates

### 6. Dependency Monitoring
- **Workflow**: `.github/workflows/dependency-check.yml`
- **Schedule**: Mondays at 10:00 AM UTC
- **Purpose**: Comprehensive status report of all dependencies with security audit
- **Features**: Multi-dependency checking, npm audit status, build validation
- **Summary**: Runs last to capture results from all morning dependency updates

## Release Process Integration

### Pre-Release Checklist

Before each monthly runner release:

1. **Check Dependency PRs**:
   ```bash
   # List open dependency PRs
   gh pr list --label "dependency" --state open
   ```

2. **Run Manual Dependency Check**:
   - Go to Actions tab → "Dependency Status Check" → "Run workflow"
   - Review the summary for any outdated dependencies

3. **Review and Merge Updates**:
   - Prioritize security-related updates
   - Test dependency updates in development environment
   - Merge approved dependency PRs

### Vulnerability Response

#### Critical Security Vulnerabilities
- **Response Time**: Within 24 hours
- **Process**:
  1. Assess impact on runner security
  2. Create hotfix branch if runner data security is affected
  3. Expedite patch release if necessary
  4. Document in security advisory if applicable

#### Non-Critical Vulnerabilities
- **Response Time**: Next monthly release
- **Process**:
  1. Evaluate if vulnerability affects runner functionality
  2. Include fix in regular dependency update cycle
  3. Document in release notes

## Monitoring and Alerts

### GitHub Actions Workflow Status
- All dependency workflows create PRs with the `dependency` label
- Failed workflows should be investigated immediately
- Weekly dependency status reports are generated automatically

### Manual Checks
You can manually trigger dependency checks:
- **Full Status**: Run "Dependency Status Check" workflow
- **Specific Component**: Use the dropdown to check individual dependencies

## Dependency Labels

All automated dependency PRs are tagged with labels for easy filtering and management:

### Primary Labels (Created by PR #4024)
- **`dependency`**: All automated dependency-related PRs
- **`security`**: Security vulnerability fixes and patches  
- **`typescript`**: TypeScript compatibility and type definition updates
- **`needs-manual-review`**: Complex updates requiring human verification

### Workflow-Specific Branches
- **Node.js updates**: `feature/node-upgrade-*` branches (PR #4026)
- **NPM security fixes**: `feature/npm-security-*` branches (PR #4027)
- **NuGet/.NET updates**: `feature/dotnetsdk-upgrade-*` branches (PR #4028)
- **Docker updates**: `feature/docker-upgrade-*` branches (PR #4029)

## Special Considerations

### Node.js Updates
When updating Node.js versions, remember to:
1. Create a corresponding release in [actions/alpine_nodejs](https://github.com/actions/alpine_nodejs)
2. Follow the alpine_nodejs getting started guide
3. Test container builds with new Node versions

### .NET SDK Updates
- Only patch versions are auto-updated within the same major.minor version
- Major/minor version updates require manual review and testing

### Docker Updates
- Updates include both Docker Engine and Docker Buildx
- Verify compatibility with runner container workflows

## Troubleshooting

### Common Issues

1. **NPM Audit Workflow Fails**:
   - Check if `package.json` exists in `src/Misc/expressionFunc/hashFiles/`
   - Verify Node.js setup step succeeded

2. **Version Detection Fails**:
   - Check if upstream APIs are available
   - Verify parsing logic for version extraction

3. **PR Creation Fails**:
   - Ensure `GITHUB_TOKEN` has sufficient permissions
   - Check if branch already exists

### Contact

For questions about the dependency management process:
- Create an issue with the `dependencies` label
- Review existing dependency management workflows
- Consult the runner team for security-related concerns

## Metrics and KPIs

Track these metrics to measure dependency management effectiveness:
- Number of open dependency PRs at release time
- Time to merge dependency updates
- Number of security vulnerabilities by severity
- Release cycle adherence (monthly target)
