# VSTS Cross Platform Agent (CoreCLR)

## Dependencies

None.  The agent will package all dependencies.

## Install

Not available yet.  Need to build from source.  See Contribute.

## Contribute (Dev)

### Dev Dependencies

.NET Core [Install Here](https://dotnet.github.io/getting-started/)  
Node (only for build) [Install Here](http://node.js.org)

### Prepare for building.  

Once from root of repo:
```
$ npm install
```

### Build, Test, Clean, Restore 

From src:

`dev {command}` _(./dev on *nix)_
  
** Commands: **

`restore`: Run first time and any time you change a project.json  

`build`:   build everything  

`clean`:   deletes build output for each projects

`test`:    run unit tests

  can run by level:

  `dev test L0`  
  `dev test L1`
        
  results in: 
  Test/bin/Debug/dnxcore50/{platform}/testResults.xml  

`layout`: Creates a full layout in {root}/_layout  
   Does a clean, restore, build, publish and copy
  

