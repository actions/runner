# Contribution guide for developers

## Required Dev Dependencies

![Win](res/win_sm.png) Git for Windows [Install Here](https://git-scm.com/downloads) (needed for dev sh script)

## To Build, Test, Layout 

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
cd ./src
./dev.(sh/cmd) layout # the runner that build from source is in {root}/_layout
<make code changes>
./dev.(sh/cmd) build # {root}/_layout will get updated
./dev.(sh/cmd) test # run all unit tests before git commit/push
```

## Editors

[Using Visual Studio 2019](https://www.visualstudio.com/vs/)  
[Using Visual Studio Code](https://code.visualstudio.com/)

## Styling

We use the .NET Foundation and CoreCLR style guidelines [located here](
https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md)
