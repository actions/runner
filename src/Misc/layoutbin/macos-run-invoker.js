const { spawn } = require('child_process');
// argv[0] = node
// argv[1] = macos-run-invoker.js
var shell = process.argv[2];
var args = process.argv.slice(3);
console.log(`::debug::macos-run-invoker: ${shell}`);
console.log(`::debug::macos-run-invoker: ${JSON.stringify(args)}`);
var launch = spawn(shell, args, { stdio: 'inherit' });
launch.on('exit', function (code) {
    if (code !== 0) {
        process.exit(code);
    }
});
