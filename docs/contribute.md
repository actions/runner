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

## Development Life Cycle

### Required Dev Dependencies

![Win](res/win_sm.png) ![*nix](res/linux_sm.png)  Git for Windows and Linux [Install Here](https://git-scm.com/downloads) (needed for dev sh script)

### To Build, Test, Layout 

Navigate to the `src` directory and run the following command:

![Win](res/win_sm.png) `dev {command}`  

![*nix](res/linux_sm.png) `./dev.sh {command}`
  
**Commands:**  

* `layout` (`l`):  Run first time to create a full runner layout in `{root}/_layout`
* `build` (`b`):   Build everything and update runner layout folder
* `test` (`t`):    Build runner binaries and run unit tests

Sample developer flow:

```bash
git clone https://github.com/actions/runner
cd runner
cd ./src
./dev.(sh/cmd) layout # the runner that built from source is in {root}/_layout
<make code changes>
./dev.(sh/cmd) build # {root}/_layout will get updated
./dev.(sh/cmd) test # run all unit tests before git commit/push
```

View logs:
```bash
cd runner/_layout/_diag
ls
cat (Runner/Worker)_TIMESTAMP.log # view your log file
```

Run Runner:
```bash
cd runner/_layout
./run.sh # run your custom runner
```

### Editors

[Using Visual Studio Code](https://code.visualstudio.com/)
[Using Visual Studio](https://code.visualstudio.com/docs)  

### Styling

We use the .NET Foundation and CoreCLR style guidelines [located here](
https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md)
