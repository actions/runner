#!/usr/bin/env node
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

var childProcess = require("child_process");
var path = require("path")

var supported = ['linux', 'darwin']

if (supported.indexOf(process.platform) == -1) {
    console.log('Unsupported platform: ' + process.platform);
    console.log('Supported platforms are: ' + supported.toString());
    process.exit(1);
}

var stopping = false;
var listener = null;

var runService = function() {
    var listenerExePath = path.join(__dirname, '../bin/Agent.Listener');
    console.log('Starting Agent listener');

    if(!stopping) {
        try {
            listener = childProcess.spawn(listenerExePath, ['run', '--startuptype', 'service'], { env: process.env });
            console.log('started listener process');
        
            listener.stdout.on('data', (data) => {
                console.log(data);
            });

            listener.stderr.on('data', (data) => {
                console.log(data);
            });

            listener.on('close', (code) => {
                console.log(`Agent listener exited with error code ${code}`);

                if (code === 0) {
                    console.log('Agent listener exit with 0 return code, stop the service, no retry needed.');
                    stopping = true;
                } else if (code === 1) {
                    console.log('Agent listener exit with terminated error, stop the service, no retry needed.');
                    stopping = true;
                } else if (code === 2) {
                    console.log('Agent listener exit with retryable error, re-launch agent in 5 seconds.');
                } else if (code === 3) {
                    console.log('Agent listener exit because of updating, re-launch agent in 5 seconds.');
                } else {
                    console.log('Agent listener exit with undefined return code, re-launch agent in 5 seconds.');
                }
                
                if(!stopping) {
                    setTimeout(runService, 5000);
                }
            });

        } catch(ex) {
            console.log(ex);
        }
    }
}

runService();
console.log('started running service');

var gracefulShutdown = function(code) {
    console.log('shutting down agent listener');
    stopping = true;
    if (listener) {
        console.log('Sending SIGINT to agent listener to stop');
        listener.kill('SIGINT');

        // TODO wait for 30 seconds and send a SIGKILL
    }
}

process.on('SIGINT', () => {
    gracefulShutdown(0);
});

process.on('SIGTERM', () => {
    gracefulShutdown(0);
});

