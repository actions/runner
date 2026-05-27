# SakThai Skills

This directory contains [`sakthai-skills`](https://github.com/beernanthasit-hub/SakThai-skills) as a git submodule — a curated set of .NET coding-agent skill plugins that complement the GitHub Actions Runner.

## What's included

The `sakthai-skills` submodule mirrors the structure of the [dotnet/skills](https://github.com/dotnet/skills) repository and ships the following plugins:

| Plugin | Description | Use with Runner |
|--------|-------------|----------------|
| `dotnet` | Core .NET coding skills | Build/test task authoring |
| `dotnet-ai` | AI and ML skills for .NET | Copilot workflow steps |
| `dotnet-aspnet` | ASP.NET Core web development skills | Web service action authoring |
| `dotnet-blazor` | Blazor development skills | Frontend action authoring |
| `dotnet-data` | .NET data access and Entity Framework skills | DB migration actions |
| `dotnet-diag` | .NET performance and diagnostics skills | Profiling/tracing in CI |
| `dotnet-msbuild` | MSBuild and build system skills | Custom build targets |
| `dotnet-nuget` | NuGet and package management skills | Package publish pipelines |
| `dotnet-test` | .NET test execution and migration skills | Test filtering, retries |
| `dotnet-upgrade` | .NET project upgrade and migration skills | Automated upgrade PRs |
| `dotnet-maui` | .NET MAUI development skills | Mobile/desktop CI jobs |
| `dotnet-template-engine` | .NET Template Engine skills | Project scaffolding |
| `dotnet11` | .NET 11 API and language feature skills | Cutting-edge SDK support |

## Getting started

Initialize the submodule after cloning SakThai:

```bash
git clone --recurse-submodules https://github.com/beernanthasit-hub/SakThai.git
```

Or if you already have a clone:

```bash
git submodule update --init --recursive
```

After initialization, the plugins are available at `skills/sakthai-skills/plugins/`.

## CI/CD Integration

The two repos are wired together via a bidirectional automation loop:

```
sakthai-skills: merge to plugins/ on main
  └─> .github/workflows/notify-runner-update.yml fires
        └─> repository_dispatch (skills-updated) → SakThai
              └─> .github/workflows/update-skills-submodule.yml fires
                    └─> git submodule update --remote
                          └─> peter-evans/create-pull-request opens:
                                auto/bump-skills-submodule PR in SakThai
```

The bump PR records the triggering commit SHA, actor, and ref so updates are fully traceable.

### Required secret

For the cross-repo dispatch to work, the `SakThai-skills` repo must have a `RUNNER_REPO_PAT` repository secret set to a Personal Access Token (PAT) with `repo` scope on `beernanthasit-hub/SakThai`.

Setup:
1. Create a PAT at **GitHub → Settings → Developer settings → Personal access tokens → Fine-grained** (or classic with `repo` scope)
2. Add it to `SakThai-skills`: **Settings → Secrets and variables → Actions → New repository secret → `RUNNER_REPO_PAT`**

## Development workflow

### Making a change to skills

1. Fork or branch `sakthai-skills`
2. Edit files under `plugins/<plugin-name>/skills/`
3. Open a PR against `sakthai-skills` main
4. Once merged, the `notify-runner-update.yml` workflow fires automatically
5. A bump PR (`auto/bump-skills-submodule`) opens in SakThai within minutes
6. Review and merge the bump PR to update SakThai's pinned skills version

### Updating the submodule manually

```bash
git submodule update --remote skills/sakthai-skills
git add skills/sakthai-skills
git commit -m "chore: bump skills/sakthai-skills submodule to latest"
git push
```

### Pinning to a specific skills version

```bash
cd skills/sakthai-skills
git checkout <desired-sha>
cd ../..
git add skills/sakthai-skills
git commit -m "chore: pin skills/sakthai-skills to <desired-sha>"
```

## Troubleshooting

### `git submodule update` fails with "no url found"

The git tree entry (mode 160000) may not be registered. Run:

```bash
git submodule add https://github.com/beernanthasit-hub/SakThai-skills.git skills/sakthai-skills
git submodule update --remote skills/sakthai-skills
```

### `update-skills-submodule` workflow creates an empty PR

This happens if the submodule pointer in `main` already matches the latest `sakthai-skills` commit. No action needed — the workflow correctly skips creating a PR when there are no changes.

### `notify-runner-update` workflow fails with "Bad credentials"

The `RUNNER_REPO_PAT` secret is missing or expired. See [Required secret](#required-secret) above.

### Submodule shows `(null)` or dirty state after checkout

Run `git submodule sync && git submodule update --init --recursive` to re-sync the URL from `.gitmodules` and re-initialize.
