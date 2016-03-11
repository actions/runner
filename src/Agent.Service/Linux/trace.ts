// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <reference path="./definitions/node.d.ts"/>

var path = require('path');
var os = require('os');
var moment = require('moment')
var shell = require('shelljs')
var fs = require('fs')

//-----------------------------------------------------------
// Interfaces
//-----------------------------------------------------------

interface ILogWriter {
    log(message: string): void;
}

class FileLogWriter implements ILogWriter {
    constructor(fullPath: string, fileName: string) {
        shell.mkdir('-p', fullPath);
        shell.chmod(775, fullPath);

        this._fd = fs.openSync(path.join(fullPath, fileName), 'a');  // append, create if not exist
    }

    private _fd: any;

    public log(message: string): void {
        fs.writeSync(this._fd, message);
    }
}

export interface ITraceWriter {
    info(message: string): void;
    error(message: string): void;
    enter(message: string): void;
}

export class TraceWriter implements ITraceWriter {
    constructor(writer: ILogWriter) {
        this.writer = writer;
    }

    private writer: ILogWriter;

    public enter(location: string) {
        this.write('Entering ' + location);
    }

    public info(message: string) {
	this.write('[Info]: ' + message);
    }

    public error(message: string) {
        this.write('[Error]: ' + message);
    }

    public write(message: string) {
        this.writer.log(message + os.EOL);
    }
}

export function getAgentServiceDiagnosticWriter(): ITraceWriter {
    var diagFolder = path.join(__dirname, '..', '_diag');
    var fileName = "AgentService_" + moment(Date.now()).format('YYYYMMDD-HHmmss') + "-utc.log"

    return new TraceWriter(new FileLogWriter(diagFolder, fileName));
}
