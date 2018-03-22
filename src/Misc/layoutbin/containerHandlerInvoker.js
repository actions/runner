const { spawn } = require('child_process');
var stdinString = "";
process.stdin.on('data', function (chunk) {
    stdinString += chunk;
});

process.stdin.on('end', function () {
    var stdinData = JSON.parse(stdinString);
    var handler = stdinData.handler;
    var handlerArg = stdinData.args;
    var handlerWorkDir = stdinData.workDir;

    console.log("##vso[task.debug]Handler: " + handler);
    console.log("##vso[task.debug]HandlerArg: " + handlerArg);
    console.log("##vso[task.debug]HandlerWorkDir: " + handlerWorkDir);
    Object.keys(stdinData.environment).forEach(function (key) {
        console.log("##vso[task.debug]Set env: " + key + "=" + stdinData.environment[key]);
        process.env[key] = stdinData.environment[key];
    });

    var options = {
        stdio: 'inherit',
        cwd: handlerWorkDir
    };
    if (process.platform == 'win32') {
        options.argv0 = `"${handler}"`;
        options.windowsVerbatimArguments = true;
    }

    var launch = spawn(handler, [handlerArg], options);
    launch.on('exit', function (code) {
        console.log("##vso[task.debug]Handler exit code: " + code);
        if (code != 0) {
            process.exit(code);
        }
    });
});
