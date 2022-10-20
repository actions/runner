# Contributions

We welcome contributions in the form of issues and pull requests.  We view the contributions and the process as the same for github and external contributors.

> IMPORTANT: Building your own runner is critical for the dev inner loop process when contributing changes.  However, only runners built and distributed by GitHub (releases) are supported in production.  Be aware that workflows and orchestrations run service side with the runner being a remote process to run steps.  For that reason, the service can pull the runner forward so customizations can be lost.

## Issues

Log issues for both bugs and enhancement requests.  Logging issues are important for the open community.

Issues in this repository should be for the runner application.  Note that the VM and virtual machine images (including the developer toolsets) installed on the actions hosted machine pools are located [in this repository](https://github.com/actions/virtual-environments)

## Enhancements and Feature Requests

We ask that before significant effort is put into code changes, that we have agreement on taking the change before time is invested in code changes.

1. Create a feature request.  Once agreed we will take the enhancement
2. Create an ADR to agree on the details of the change.

An ADR is an Architectural Decision Record.  This allows consensus on the direction forward and also serves as a record of the change and motivation.  [Read more here](adrs/README.md)

## Required Dev Dependencies

![Win](res/win_sm.png) ![*nix](res/linux_sm.png)  Git for Windows and Linux [Install Here](https://git-scm.com/downloads) (needed for dev sh script)

![*nix](res/linux_sm.png) cURL [Install here](https://curl.se/download.html) (needed for external sh script)

![Win](res/win_sm.png) Visual Studio 2017 or newer [Install here](https://visualstudio.microsoft.com) (needed for dev sh script)

![Win-arm](res/win_sm.png) Visual Studio 2022 17.3 Preview or later. [Install here](https://docs.microsoft.com/en-us/visualstudio/releases/2022/release-notes-preview)

## Quickstart: Run a job from a real repository

If you just want to get from building the sourcecode to using it to execute an action, you will need:

- The url of your repository
- A runner registration token. You can find it at `https://github.com/{your-repo}/settings/actions/runners/new`


```bash
git clone https://github.com/actions/runner
cd runner/src
./dev.(sh/cmd) layout # the runner that built from source is in {root}/_layout
cd ../_layout
./config.(sh/cmd) --url https://github.com/{your-repo} --token ABCABCABCABCABCABCABCABCABCAB # accept default name, labels and work folder
./run.(sh/cmd)
```

If you trigger a job now, you can see the runner execute it.

Tip: Make sure your job can run on this runner. The easiest way is to set `runs-on: self-hosted` in the workflow file.


## Development Life Cycle
If you're using VS Code, you can follow [these](contribute/vscode.md) steps instead.

### To Build, Test, Layout

Navigate to the `src` directory and run the following command:

![Win](res/win_sm.png) `dev {command}`  

![*nix](res/linux_sm.png) `./dev.sh {command}`
  
**Commands:**  

* `layout` (`l`):  Run first time to create a full runner layout in `{root}/_layout`
* `build` (`b`):   Build everything and update runner layout folder
* `test` (`t`):    Build runner binaries and run unit tests

### Sample developer flow:

```bash
git clone https://github.com/actions/runner
cd runner
cd ./src
./dev.(sh/cmd) layout # the runner that built from source is in {root}/_layout
<make code changes>
./dev.(sh/cmd) build # {root}/_layout will get updated
./dev.(sh/cmd) test # run all unit tests before git commit/push
```

Let's break that down.

### Clone repository:

```bash
git clone https://github.com/actions/runner
cd runner
```
If you want to push your changes to a remote, it is recommended you fork the repository and use that fork as your origin instead of `https://github.com/actions/runner`.


### Build Layout:

This command will build all projects, then copies them and other dependencies into a folder called `_layout`. The binaries in this folder are then used for running, debugging the runner.

```bash
cd ./src # execute the script from this folder
./dev.(sh/cmd) layout # the runner that built from source is in {root}/_layout
```

If you make code changes after this point, use the argument `build` to build your code in the `src` folder to keep your `_layout` folder up to date.

```bash
cd ./src
./dev.(sh/cmd) build # {root}/_layout will get updated
```
### Test Layout:

This command runs the suite of unit tests in the project

```bash
cd ./src
./dev.(sh/cmd) test # run all unit tests before git commit/push
```

### Configure Runner:

If you want to manually test your runner and run actions from a real repository, you'll have to configure it before running it.

```bash
cd runner/_layout
./config.(sh/cmd) # configure your custom runner
```

You will need your the name of your repository and a runner registration token.
Check [Quickstart](##Quickstart:-Run-a-job-from-a-real-repository) if you don't know how to get this token.

These can also be passed down as arguments to `config.(sh/cmd)`:
```bash
cd runner/_layout
./config.(sh/cmd) --url https://github.com/{your-repo} --token ABCABCABCABCABCABCABCABCABCAB
```

### Run Runner

All that's left to do is to start the runner:
```bash
cd runner/_layout
./run.(sh/cmd) # run your custom runner
```

### View logs:

```bash
cd runner/_layout/_diag
ls
cat (Runner/Worker)_TIMESTAMP.log # view your log file
```

## Editors

[Using Visual Studio Code](https://code.visualstudio.com/)
[Using Visual Studio](https://code.visualstudio.com/docs)  

## Styling

We use the .NET Foundation and CoreCLR style guidelines [located here](
https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md)

### Format C# Code

```
cd ./src
./dev.(cmd|sh) format
```

### Linting Locally

In our CI we use [github/super-linter](https://github.com/github/super-linter) to format code using `dotnet-format`.
You can run the CI version of the linter locally using the Docker image provided by the super-linter team.

```
./script/lint.sh
```
