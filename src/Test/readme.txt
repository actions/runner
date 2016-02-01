run tests with: 
corerun "xunit.console.netcore.exe" "tests.dll" -xml "testResults.xml" -notrait category=failing

{
    "version": "1.0.0-*",

    "compilationOptions": {
        "emitEntryPoint": true
    },

    "commands": {
        "test": "xunit.runner.dnx"
    },

     "testRunner": "xunit",
   
     "dependencies": {
        "NETStandard.Library": "1.0.0-rc3-23727",        

        "xunit.runner.visualstudio": "2.1.0",
        "xunit.runner.dnx": "2.1.0-rc1-build204",
        "xunit.runners": "2.0.0",

        "xunit": "2.1.0",        
        "xunit.console.netcore": "1.0.2-prerelease-00101",
        "xunit.netcore.extensions": "1.0.0-prerelease-*",
        "xunit.runner.utility": "2.1.0"        
    },
    "frameworks": {
        "dnxcore50": {
            "imports": "portable-net451+win8"
        }
    }    
}


        "xunit": "2.1.0",
        "xunit.console.netcore": "1.0.2-prerelease-00101",
        "xunit.netcore.extensions": "1.0.0-prerelease-00153",
        "xunit.runner.utility": "2.1.0"    
        