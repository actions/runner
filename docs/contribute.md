# Contribute (Dev)

## Dev Dependencies

![Win](res/win_sm.png) Git for Windows [Install Here](https://git-scm.com/downloads) (needed for dev sh script)

## Build, Test, Layout 

From src:

![Win](res/win_sm.png) `dev {command}`  

![*nix](res/linux_sm.png) `./dev.sh {command}`
  
**Commands:**  

`layout` (`l`):  Run first time to create a full agent layout in {root}/_layout  

`build` (`b`):   build everything and update agent layout folder  

`test` (`t`):    build agent binaries and run unit tests  

Normal dev flow:
```bash
git clone https://github.com/microsoft/azure-pipelines-agent
cd ./src
./dev.(sh/cmd) layout # the agent that build from source is in {root}/_layout
<make code changes>
./dev.(sh/cmd) build # {root}/_layout will get updated
./dev.(sh/cmd) test # run all unit tests before git commit/push
```

## Editors

[Using Visual Studio 2017](https://www.visualstudio.com/vs/)  
[Using Visual Studio Code](https://code.visualstudio.com/)

## Styling

We use the dotnet foundation and CoreCLR style guidelines [located here](
https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md)
