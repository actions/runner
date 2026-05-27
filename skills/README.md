# SakThai Skills

This directory contains [`sakthai-skills`](https://github.com/beernanthasit-hub/SakThai-skills) as a git submodule — a curated set of .NET coding-agent skill plugins that complement the GitHub Actions Runner.

## What's included

The `sakthai-skills` submodule mirrors the structure of the [dotnet/skills](https://github.com/dotnet/skills) repository and ships the following plugins:

| Plugin | Description |
|--------|-------------|
| `dotnet` | Core .NET coding skills |
| `dotnet-ai` | AI and ML skills for .NET |
| `dotnet-aspnet` | ASP.NET Core web development skills |
| `dotnet-blazor` | Blazor development skills |
| `dotnet-data` | .NET data access and Entity Framework skills |
| `dotnet-diag` | .NET performance and diagnostics skills |
| `dotnet-msbuild` | MSBuild and build system skills |
| `dotnet-nuget` | NuGet and package management skills |
| `dotnet-test` | .NET test execution and migration skills |
| `dotnet-upgrade` | .NET project upgrade and migration skills |
| `dotnet-maui` | .NET MAUI development skills |
| `dotnet-template-engine` | .NET Template Engine skills |
| `dotnet11` | .NET 11 API and language feature skills |

## Getting started

Initialize the submodule after cloning SakThai:

```bash
git clone --recurse-submodules https://github.com/beernanthasit-hub/SakThai.git
```

Or if you already have a clone:

```bash
git submodule update --init --recursive
```

## Keeping skills up to date

The [`update-skills-submodule`](../.github/workflows/update-skills-submodule.yml) workflow in this repo automatically bumps the submodule pointer whenever a new change is merged into `sakthai-skills`. It is triggered via a `repository_dispatch` event from the `sakthai-skills` repo.

To update manually:

```bash
git submodule update --remote skills/sakthai-skills
git add skills/sakthai-skills
git commit -m "chore: bump skills/sakthai-skills submodule to latest"
```
