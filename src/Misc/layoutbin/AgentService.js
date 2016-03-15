#!/usr/bin/env node
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

var childProcess = require("child_process");
var path = require("path")

var supported = ['linux']

if (supported.indexOf(process.platform) == -1) {
    console.log('Unsupported platform: ' + process.platform);
    console.log('Supported platforms are: ' + supported.toString());
    process.exit(1);
}

var stopping = false;
var listener = null;

var runService = function() {
    var listenerExePath = path.join(__dirname, 'Agent.Listener');
    console.log('Starting Agent listener');

    if(!stopping) {
        try {
            listener = childProcess.spawn(listenerExePath, ['run'], { env: process.env });
            console.log('started listener process');
        
            listener.stdout.on('data', (data) => {
                console.log(data);
            });

            listener.stderr.on('data', (data) => {
                console.log(data);
            });

            listener.on('close', (code) => {
                console.log(`Agent listenere exited with error code ${code}`);

                if (code === 1)
                {
                    console.log('Can not start agent listener process, check logs for error detail');
                    stopping = true;
                }

                if (code === 2)
                {
                    console.log('Received message from agent listener to stop the service');
                    stopping = true;
                }

                setTimeout(runService, 0);
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

    process.exit(code);
}

process.on('SIGINT', () => {
    gracefulShutdown(0);
});

process.on('SIGTERM', () => {
    gracefulShutdown(0);
});

