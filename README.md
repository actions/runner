# VSTS Cross Platform Agent (CoreCLR)

## Dependencies

None.  The agent will package all dependencies.

## Install

Not available yet.  Need to build from source.  See Contribute.

## Contribute (Dev)

### Dev Dependencies

.NET Core [Install Here](https://dotnet.github.io/getting-started/)  

### Build, Test, Clean, Restore 

From src:

`./dev.sh {command}` _(Win use Git bash prompt to run sh)_
  
** Commands: **

`restore` (`r`): Run first time and any time you change a project.json  

`build` (`b`):   build everything  

`test` (`t`):    run unit tests
        
  results in: 
  Test/bin/Debug/dnxcore50/{platform}/testResults.xml

`buildtest` (`bt`): build and test

`clean` (`c`):   deletes build output for each projects
 
`layout` (`l`): Creates a full layout in {root}/_layout  
   Does a clean, restore, build, publish and copy
  

