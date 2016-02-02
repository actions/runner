# VSTS Cross Platform Agent (CoreCLR)

## Dependencies

.NET core [Install Here](https://dotnet.github.io/getting-started/)
Node (build) [Install Here](http://node.js.org)

## Contribute

### Prepare for building.  

Once from root of repo:
```
$ npm install
```

### Build, Test, Clean, Restore 

From /src dir:

*nix  
`./dev.js {command}`
  
Win32   
`dev.cmd {command}`
  
** Commands: **

`restore`: Run first time and any time you change a project.json  

`build`:   build everything  

`test`:    run unit tests  
           results in src\tests\bin\Debug\dnxcore50\{platform}\testResults.xml  

`clean`:   deletes build output for each projects  

