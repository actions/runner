const core = require('@actions/core');
var formidable = require('formidable');
var http = require('http');
var path = require('path');
const fs = require('fs');
const { env } = require('process');

try {
    const _path = core.getInput('path');
    const repository = core.getInput('repository');
    const ref = core.getInput('ref');
    if (repository != env["GITHUB_REPOSITORY"] || ref != undefined && ref != '' && ref != env["GITHUB_REF"]) {
        core.setOutput("skip", false);
    } else {
        core.setOutput("skip", true);
        const url = env['ACTIONS_RUNTIME_URL'] + "_apis/v1/Message/multipart/" + env["GITHUB_RUN_ID"];
        const dest = _path != '' && _path != undefined ? path.join(env["GITHUB_WORKSPACE"], _path) : env["GITHUB_WORKSPACE"];

        var form = formidable({
            uploadDir: dest,
            maxFileSize: 1024 * 1024 * 1024 * 1024
        });
        http.get(url, res => {
            var _first = true;
            form.parse(res).on('fileBegin', (formname, file) => {
                if(formname == null && _first) {
                    console.log("No files found to copy to " + dest);
                    process.exit();
                }
                _first = false;
                file.filename = path.join(dest, formname);
                file.path = path.join(dest, formname);
                fs.mkdirSync(path.dirname(file.path), { recursive: true });
            }).on('file', (name) => {
                console.log(name);
            });
        });
    }
} catch (error) {
    core.setFailed(error.message);
}