# VSTS Cross Platform Agent (CoreCLR)

## Dependencies

None.  The agent will package all dependencies.

## Install

Not available yet.  Need to build from source.  See Contribute.

## Contribute (Dev)

### Dev Dependencies

.NET core [Install Here](https://dotnet.github.io/getting-started/)
Node (only for build) [Install Here](http://node.js.org)

### Prepare for building.  

Once from root of repo:
```
$ npm install
```

### Build, Test, Clean, Restore 

From /src dir:

Win32   
`dev {command}`

*nix  
`./dev.js {command}`
  
** Commands: **

`restore`: Run first time and any time you change a project.json  

`build`:   build everything  

`test`:    run unit tests  
           results in src\tests\bin\Debug\dnxcore50\{platform}\testResults.xml  

`clean`:   deletes build output for each projects  

