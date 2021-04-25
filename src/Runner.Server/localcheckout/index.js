const core = require('@actions/core');
var formidable = require('formidable');
var http = require('http');
var path = require('path');
const fs = require('fs');
const { env } = require('process');
// const tc = require('@actions/tool-cache');

try {
    const _path = core.getInput('path');
    const repository = core.getInput('repository');
    const ref = core.getInput('ref');
    if (repository != env["GITHUB_REPOSITORY"] || ref != undefined && ref != '' && ref != env["GITHUB_REF"]) {
        // var cp = require('child_process');
        // fs.existsSync(path.join(path.dirname(path.dirname(__dirname), "actions")
        // const url = env['ACTIONS_RUNTIME_URL'] + "_apis/v1/ActionDownloadInfo/" + env["GITHUB_RUN_ID"];
        // await tc.downloadTool()
        // cp.fork(path.join(__dirname, 'index.js')).on('exit', code => {
        //     process.exit(code);
        // });
        core.setOutput("skip", false);
    } else {
        // const clean = core.getInput('clean');
        const url = env['ACTIONS_RUNTIME_URL'] + "_apis/v1/Message/multipart/" + env["GITHUB_RUN_ID"];
        const dest = _path != '' && _path != undefined ? path.join(env["GITHUB_WORKSPACE"], _path) : env["GITHUB_WORKSPACE"];//"C:\\Users\\Christopher\\Documents\\test";
        // if(clean === 'true' && fs.existsSync(dest)) {
        //     fs.rmSync(path.join(dest, "*"), { recursive: true, force: true });
        // }
        var form = formidable({
            uploadDir: dest,
            maxFileSize: 1024 * 1024 * 1024 * 1024
        });
        http.get(url, res => {

            form.parse(res).on('fileBegin', (formname, file) => {
                file.filename = path.join(dest, formname);
                file.path = path.join(dest, formname);
                fs.mkdirSync(path.dirname(file.path), { recursive: true });
            }).on('file', (name, file) => {
                console.log(name);
            });
        });
        core.setOutput("skip", true);
    }
} catch (error) {
    core.setFailed(error.message);
}