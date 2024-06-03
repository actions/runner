<p align="center">
  <img src="docs/res/github-graph.png">
</p>

# Github Actions Runner for DMG

## Instructions

Update the `images/Dockerfile` as you please. Then run the [publish image action](https://github.com/johngeorgewright/actions-runner/actions/workflows/publish-image.yml) with **no** arguments. This will override our ARC image, as the "latest", in dockerhub.

## Troubleshooting

### GitHub is complaing that the image is out of date

Sometimes GitHub will update their systems and this image will need updating.

1. pull the changes from upstream
```
gh repo clone johngeorgewright/actions-runner
cd actions-runner
git fetch upstream
git pull upstream main
git push origin main
```
2. Run the [publish image action](https://github.com/johngeorgewright/actions-runner/actions/workflows/publish-image.yml)
3. Sit back and watch everything come back to life :coffee:

---

# GitHub Actions Runner

[![Actions Status](https://github.com/actions/runner/workflows/Runner%20CI/badge.svg)](https://github.com/actions/runner/actions)

The runner is the application that runs a job from a GitHub Actions workflow. It is used by GitHub Actions in the [hosted virtual environments](https://github.com/actions/virtual-environments), or you can [self-host the runner](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/about-self-hosted-runners) in your own environment.

## Get Started

For more information about installing and using self-hosted runners, see [Adding self-hosted runners](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/adding-self-hosted-runners) and [Using self-hosted runners in a workflow](https://help.github.com/en/actions/automating-your-workflow-with-github-actions/using-self-hosted-runners-in-a-workflow)

Runner releases:

![win](docs/res/win_sm.png) [Pre-reqs](docs/start/envwin.md) | [Download](https://github.com/actions/runner/releases)  

![macOS](docs/res/apple_sm.png)  [Pre-reqs](docs/start/envosx.md) | [Download](https://github.com/actions/runner/releases)  

![linux](docs/res/linux_sm.png)  [Pre-reqs](docs/start/envlinux.md) | [Download](https://github.com/actions/runner/releases)

## Contribute

We accept contributions in the form of issues and pull requests. The runner typically requires changes across the entire system and we aim for issues in the runner to be entirely self contained and fixable here. Therefore, we will primarily handle bug issues opened in this repo and we kindly request you to create all feature and enhancement requests on the [GitHub Feedback](https://github.com/community/community/discussions/categories/actions-and-packages) page. [Read more about our guidelines here](docs/contribute.md) before contributing.
