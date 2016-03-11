#!/usr/bin/env node
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <reference path="./definitions/node.d.ts"/>

import childProcess = require("child_process");
import os = require("os")
import fs = require("fs")
import path = require("path")
import traceModule = require("./trace")

var supported = ['linux']

if (supported.indexOf(process.platform) == -1) {
    console.log('Unsupported platform: ' + process.platform);
    console.log('Supported platforms are: ' + supported.toString());
    process.exit(1);
}

var stopping:boolean = false;
var listener:childProcess.ChildProcess = null;
var trace:traceModule.ITraceWriter = traceModule.getAgentServiceDiagnosticWriter();

var runService = function() {
    var listenerExePath = path.join(__dirname, 'Agent.Listener');
    trace.info('Starting Agent listener');

    if(!stopping) {
	try {
	    listener = childProcess.spawn(listenerExePath, ['run'], { env: process.env });
	    trace.info('started listener process');
		
	    listener.stdout.on('data', (data) => {
		trace.info(data);
	    });

	    listener.stderr.on('data', (data) => {
		trace.error(data);
	    });

	    listener.on('close', (code, signal) => {
		trace.info(`Agent listenere exited with error code ${code}`);

		if (code == 1)
		{
		    trace.error('Can not start agent listener process, check logs for error detail');
		    stopping = true;
		}

		if (code == 2)
		{
		    trace.info('Received message from agent listener to stop the service');
		    stopping = true;
		}

		setTimeout(runService, 0);
	    });

	} catch(ex) {
	    trace.error(ex);
	}
    }
}

runService();
trace.info('started running service');

var gracefulShutdown = function(code) {
    trace.info('shutting down agent listener');
    stopping = true;
    if (listener) {
	trace.info('Sending SIGINT to agent listener to stop');
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

