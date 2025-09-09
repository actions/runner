# Runner Dependency Management Process

## Overview

This document outlines the automated dependency management process for the GitHub Actions Runner, designed to ensure we maintain up-to-date and secure dependencies while providing predictable release cycles.

## Release Schedule

- **Monthly Runner Releases**: New runner versions are released monthly
- **Weekly Dependency Checks**: Automated workflows check for dependency updates every Monday
- **Security Patches**: Critical security vulnerabilities are addressed immediately outside the regular schedule

## Automated Workflows

### 1. Node.js Version Updates
- **Workflow**: `.github/workflows/node-upgrade.yml`
- **Schedule**: Mondays at 6:00 AM UTC
- **Purpose**: Updates Node.js 20 and 24 versions in `src/Misc/externals.sh`
- **Source**: [actions/node-versions](https://github.com/actions/node-versions)

### 2. NPM Security Audit
- **Workflow**: `.github/workflows/npm-upgrade.yml`
- **Schedule**: Mondays at 7:00 AM UTC
- **Purpose**: Runs `npm audit fix` on hashFiles dependencies
- **Location**: `src/Misc/expressionFunc/hashFiles/`

### 3. .NET SDK Updates
- **Workflow**: `.github/workflows/dotnet-upgrade.yml`
- **Schedule**: Mondays at 12:00 AM UTC
- **Purpose**: Updates .NET SDK patch versions in `src/global.json`

### 4. Docker/Buildx Updates
- **Workflow**: `.github/workflows/docker-buildx-upgrade.yml`
- **Schedule**: Mondays at 12:00 AM UTC
- **Purpose**: Updates Docker and Docker Buildx versions in `images/Dockerfile`

### 5. Dependency Status Check
- **Workflow**: `.github/workflows/dependency-check.yml`
- **Schedule**: Mondays at 8:00 AM UTC
- **Purpose**: Provides comprehensive status report of all dependencies

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

All automated dependency PRs are tagged with the `dependency` label for easy filtering:
- Node.js updates: `chore/update-node` branch
- NPM security fixes: `chore/npm-audit-fix` branch  
- .NET updates: `feature/dotnetsdk-upgrade/*` branch
- Docker updates: Branch named with versions

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
