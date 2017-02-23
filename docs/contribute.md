# Contribute (Dev)

## Dev Dependencies

![Win](res/win_sm.png) Git for Windows [Install Here](https://git-scm.com/downloads) _(needed for dev sh script)

## Build, Test, Clean, Restore 

From src:

![Win](res/win_sm.png) `dev {command}`  

![*nix](res/linux_sm.png) `./dev.sh {command}`
  
**Commands:**  

`restore` (`r`): Run first time and any time you change a project.json  

`build` (`b`):   build everything  

`test` (`t`):    run unit tests
        
  results in: 
  Test/bin/Debug/dnxcore50/{platform}/testResults.xml

`buildtest` (`bt`): build and test

`clean` (`c`):   deletes build output for each projects
 
`layout` (`l`): Creates a full layout in {root}/_layout  
   Does a clean, restore, build, publish and copy
   Default is Debug.  Passing Release as argument is supported (dev l Release)

`update` (`u`) {dirname}: Builds and publishes just one dir.  Patches the layout
   update {dirname}
   Use if you change code in an assembly and don't want to wait for the full layout.

`validate` (`v`): Precheckin validation.  Runs git clean, layout and test.

## Editors

[Using Visual Studio 2017](https://www.visualstudio.com/vs/)  
[Using Visual Studio Code](https://code.visualstudio.com/)

## Styling

We use the dotnet foundation and CoreCLR style guidelines [located here](
https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md)
