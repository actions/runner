#!/usr/bin/env node

var shell = require('shelljs');
var path = require('path');
var fs = require('fs');

var fail = function(lines) {
    console.error('FAIL:');
    lines.forEach(function(line) {
       console.error('\t' + line); 
    });
    process.exit(1);
}

var dotnet = function(action, args, options, bestEffort) {
    var cmd = 'dotnet ' + action;
    if (options) {
        cmd += ' ' + options.join(' ');
    }
    
    if (args) {
        cmd += ' ' + args.join(' ');
    }
    
    console.log();
    console.log('Running: ' + cmd);
    console.log();
    var rc = shell.exec(cmd).code;
    if (rc != 0 && !bestEffort) {
        fail('dotnet failed. rc=' + rc);
    }
}

var projs = require('./global.json').projects;

//-----------------------------------------------------------
//  Actions (Driven off of global.json)
//-----------------------------------------------------------
var actions = {}
actions["restore"] = function() {
    projs.forEach(function(proj) {
        dotnet('restore', [proj], null, true);
    })
    console.log('done.');  
}

actions["build"] = function() {
    projs.forEach(function(proj) {
        dotnet('build', [proj]);
    })
    console.log('done.');  
}

actions["clean"] = function() {
    projs.forEach(function(proj) {
        'Cleaning ' + proj; 
        shell.pushd(proj);
        shell.rm('-rf', 'bin');
        shell.rm('-rf', 'obj');
        shell.popd();
    })    
    console.log('done.');
}

actions["test"] = function() {
    dotnet('publish', ['Test']);
    var pubRoot = path.join(__dirname, 'Test/bin/Debug/dnxcore50');
    var plats = shell.ls(pubRoot);
    var plat;
    if (plats && plats.length > 0) {
        plats.forEach(function(item) {
            var itemPath = path.join(pubRoot, item);
            if (fs.lstatSync(itemPath).isDirectory()) {
                plat = itemPath;
            }
        })
        
        shell.pushd(plat);
        var rc = shell.exec('corerun xunit.console.netcore.exe Test.dll').code;
        console.log();
        console.log('Done tests.  rc=' + rc);
        console.log();
        shell.popd();        
    }
    else {
        fail(['Did not find a published build under ' + pubRoot]);
    }  
}


var cmd;
var args = process.argv.slice(2);
if (args) {
    cmd = args[0];
}

if (!cmd || !actions[cmd]) {
    fail(['Invalid command.', 'Use: build, clean, test, publish or restore']);    
}

console.log('Action: ' + cmd);
if (actions[cmd]) {
    actions[cmd]();
}

