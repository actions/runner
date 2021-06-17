const core = require("@actions/core");
var formidable = require("formidable");
var http = require("http");
var https = require("https");
var path = require("path");
const fs = require("fs");
const { env } = require("process");
const del = require("del");

try {
    const _checkoutref = core.getInput("checkoutref");
    const _path = core.getInput("path");
    const repository = core.getInput("repository");
    const ref = core.getInput("ref");
    if (repository !== env["GITHUB_REPOSITORY"] || ref !== undefined && ref !== "" && ref !== env["GITHUB_REF"] && ref !== env["GITHUB_SHA"]) {
        core.setOutput("skip", false);
    } else {
        core.setOutput("skip", true);

        var submodules = false
        var nestedSubmodules = false
        const submodulesString = (core.getInput('submodules') || '').toUpperCase()
        if (submodulesString == 'RECURSIVE') {
            submodules = true
            nestedSubmodules = true
        } else if (submodulesString == 'TRUE') {
            submodules = true
        }
        const url = env["ACTIONS_RUNTIME_URL"] + "_apis/v1/Message/multipart/" + env["GITHUB_RUN_ID"] + "?submodules=" + (submodules ? "true" : "false") + "&nestedSubmodules=" + (nestedSubmodules ? "true" : "false");
        var githubWorkspacePath = env["GITHUB_WORKSPACE"]
        if(_checkoutref.toLowerCase().startsWith("v1")) {
            githubWorkspacePath = path.join(githubWorkspacePath,  "..")
        }
        const dest = _path !== "" && _path !== undefined ? path.join(githubWorkspacePath, _path) : githubWorkspacePath;
        if(!(dest + path.sep).startsWith(githubWorkspacePath + path.sep)) {
            throw new Error(`Repository path '${dest}' is not under '${githubWorkspacePath}'`)
        }
        var clean = core.getInput("clean");
        if(clean === undefined || clean === "" || clean === "true") {
            var posixdest = dest.replace("\\", "/");
            core.info("Clean folder: " + dest);
            del.sync([ posixdest + "/**", "!" + posixdest ]);
        }
        core.info("Copying Repository to " + dest);

        var form = formidable({
            uploadDir: dest,
            maxFileSize: 1024 * 1024 * 1024 * 1024
        });
        (url.startsWith("https://") ? https.get : http.get)(url, res => {
            var _first = true;
            form.parse(res).on("fileBegin", (formname, file) => {
                if(formname == null && _first) {
                    core.warning("No files found to copy to " + dest);
                    process.exit();
                }
                _first = false;
                if(formname.startsWith("=?utf-8?B?")) {
                    formname = Buffer.from(formname.substring("=?utf-8?B?".length), "base64").toString("utf-8");
                }
                var modeend = formname.indexOf(":");
                if(modeend !== -1) {
                    formname = formname.substr(modeend + 1);
                }
                file.filename = path.join(dest, formname);
                file.path = path.join(dest, formname);
                fs.mkdirSync(path.dirname(file.path), { recursive: true });
            }).on("file", (formname, file) => {
                if(formname.startsWith("=?utf-8?B?")) {
                    formname = Buffer.from(formname.substring("=?utf-8?B?".length), "base64").toString("utf-8");
                }
                var modeend = formname.indexOf(":");
                var mode = "644";
                if(modeend != -1) {
                    mode = formname.substr(0, modeend);
                    formname = formname.substr(modeend + 1);
                }
                try {
                    fs.chmodSync(file.path, mode);
                } catch {
                    core.warning("Failed to set mode of `" + file.path + "` to " + mode);
                }
                core.debug(formname + ", mode=" + mode + " => " + file.path);
            }).on("field", (formname, value) => {
                if(formname.startsWith("=?utf-8?B?")) {
                    formname = Buffer.from(formname.substring("=?utf-8?B?".length), "base64").toString("utf-8");
                }
                var modeend = formname.indexOf(":");
                var mode = "644";
                if(modeend != -1) {
                    mode = formname.substr(0, modeend);
                    formname = formname.substr(modeend + 1);
                }
                if(mode === "lnk") {
                    try {
                        fs.symlinkSync(path.join(dest, value), path.join(dest, formname));
                    } catch {
                        core.warning("Failed to create symlink `" + path.join(dest, formname) + "` to `" + path.join(dest, value) + "`");
                    }
                } else {
                    core.warning("Expected mode lnk, ignore entry `" + formname + "`");
                }
                core.debug(formname + ", mode=" + mode + " => " + value);
            });
        });
    }
} catch (error) {
    core.setFailed(error.message);
}