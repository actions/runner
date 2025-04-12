const vscode = require('vscode');
import { basePaths, customImports } from "./config.js"
import { AzurePipelinesDebugSession } from "./azure-pipelines-debug";
import { integer, Position } from "vscode-languageclient";
import jsYaml from "js-yaml";
import { applyEdits, modify } from "jsonc-parser";
import vsVars from "vscode-variables";

function joinPath(l, r) {
	return l ? l + "/" + r : r;
}

function cliQuote(str) {
	if(str.includes(" ") || str.includes("$") || str.includes("\t") || str.includes("\n") || str.includes("\r") || str.includes('"')) {
		return `'${str.replace(/'/g, "'\\''")}'`;
	}
	return str;
}

async function locateExternalRepoUrl(rawbase, filename) {
	try {
		var base = vscode.Uri.parse(rawbase, true);
		var stat = await vscode.workspace.fs.stat(base);
		if(stat.type === vscode.FileType.Directory) {
			return base.with({ path: joinPath(base.path, filename) });
		}
	} catch {

	}
	try {
		var base = vscode.Uri.file(rawbase);
		var stat = await vscode.workspace.fs.stat(base);
		if(stat.type === vscode.FileType.Directory) {
			return base.with({ path: joinPath(base.path, filename) });
		}
	} catch {

	}
	throw Error(`Cannot locate: ${rawbase}`);
}

/**
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {
	basePaths.basedir = context.extensionUri.with({ path: context.extensionUri.path + "/build/AppBundle/_framework/blazor.boot.json" }).toString();
	var { dotnet } = require("./build/AppBundle/_framework/dotnet.js");
	customImports["dotnet.runtime.js"] = require("./build/AppBundle/_framework/dotnet.runtime.js");
	customImports["dotnet.native.js"] = require("./build/AppBundle/_framework/dotnet.native.js");

	var logchannel = vscode.window.createOutputChannel("Azure Pipeline Evaluation Log", { log: true });

	var virtualFiles = {};
	var myScheme = "azure-pipelines-vscode-ext";
	var changeDoc = new vscode.EventEmitter();
	vscode.workspace.registerTextDocumentContentProvider(myScheme, {
		onDidChange: changeDoc.event,
		provideTextDocumentContent(uri) {
			return virtualFiles[uri.path];
		}
	});
	var loadingPromise = null;
	var runtimePromise = () => loadingPromise ??= vscode.window.withProgress({
		location: vscode.ProgressLocation.Notification,
		title: "Updating Runtime",
		cancellable: true
	}, async (progress, token) => {
		logchannel.appendLine("Updating Runtime");
		var items = 1;
		var citem = 0;
		var runtime = await dotnet.withOnConfigLoaded(async config => {
			items = Object.keys(config.resources.assembly).length;
			// 2024-10-28 Unknown breaking change in .net8.0 deployed pdb's that refuse to be served by marketplace cdn
			config.debugLevel = 0;
			config.resources.pdb = {};
		}).withConfigSrc(context.extensionUri.with({ path: context.extensionUri.path + "/build/AppBundle/_framework/blazor.boot.json" }).toString()).withResourceLoader((type, name, defaultUri, integrity, behavior) => {
			if(type === "dotnetjs") {
				// Allow both nodejs and browser to use the same code
				customImports[defaultUri] = customImports[name];
				return defaultUri;
			}
			return (async () => {
				if(token.isCancellationRequested) {
					throw new Error("loading Runtime aborted, reload the window to use this extension");
				}
				if(type === "assembly") {
					await progress.report({ message: name, increment: citem++ / items });
				}
				if(name.endsWith(".dat")) {
					name = name.substring(0, name.length - 3) + "icu";
				}
				var content = await vscode.workspace.fs.readFile(context.extensionUri.with({ path: context.extensionUri.path + "/build/AppBundle/_framework/" + name }));
				return new Response(content, { status: 200 });
			})();
		}).create();
		runtime.setModuleImports("extension.js", {
			readFile: async (handle, repositoryAndRef, filename) => {
				try {
					var uri = null;
					if(repositoryAndRef) {
						if(handle.repositories && repositoryAndRef in handle.repositories) {
							var rawbase = handle.repositories[repositoryAndRef];
							uri = await locateExternalRepoUrl(rawbase, filename);
						} else {
							var result = null;
							while(true) {
								try {
									result = handle.askForInput ? await (() => {
										let inputbox = vscode.window.createInputBox();
										var ret = new Promise((resolve, reject) => {
											inputbox.ignoreFocusOut = true;
											inputbox.placeholder = "local folder path or vscode uri";
											inputbox.buttons = [{
												iconPath: new vscode.ThemeIcon("folder"),
												tooltip: "Show folder picker"
											}];
											inputbox.prompt = `${repositoryAndRef} (${filename})`;
											inputbox.value = result;
											inputbox.title = "Provide the vscode uri or filepath to the folder of your required Repository";
											inputbox.onDidAccept(() => resolve(inputbox.value));
											let unregister = inputbox.onDidHide(() => resolve(null));
											inputbox.onDidTriggerButton(async (button) => {
												unregister.dispose();
												var uris = await vscode.window.showOpenDialog({ canSelectFolders: true, openLabel: "Select Repository"});
												resolve(uris && uris[0] ? uris[0].toString() : null);
											});
											inputbox.show();
										});
										ret.finally(() => inputbox.dispose());
										return ret;
									})() : null;
									if(result) {
										handle.repositories ??= {};
										handle.repositories[repositoryAndRef] = result;
										uri = await locateExternalRepoUrl(result, filename);
										handle.customConfig ??= {};
										handle.customConfig.repositories ??= {};
										handle.customConfig.repositories[repositoryAndRef] = result;
										break;
									} else {
										throw new Error(`Cannot access remote repository ${repositoryAndRef} (${filename})`);
									}
								} catch(ex) {
									if(handle.askForInput) {
										var retry = await vscode.window.showQuickPick(["Retry", "Cancel"], { placeHolder: ex.toString(), ignoreFocusOut: true });
										if(retry === "Retry") {
											continue;
										}
									}
									logchannel.error(ex.toString());
									return null;
								}
							}
						}
					} else {
						uri = handle.base.with({ path: joinPath(handle.base.path, filename) });
					}
					if(uri === null) {
						logchannel.error(`Failed to access ${filename} (${repositoryAndRef ?? "self"}), error: uri is null`);
						return null;
					}
					handle.referencedFiles.push(uri);
					handle.refToUri[`(${repositoryAndRef ?? "self"})/${filename}`] = uri;
					var scontent = null;
					var textDocument = vscode.workspace.textDocuments.find(t => t.uri.toString() === uri.toString());
					if(textDocument) {
						scontent = textDocument.getText();
					} else {
						// Read template references via filesystem api
						var content = await vscode.workspace.fs.readFile(uri);
						scontent = new TextDecoder().decode(content);
					}
					return scontent;
				} catch(ex) {
					logchannel.error(`Failed to access ${filename} (${repositoryAndRef ?? "self"}), error: ${ex.toString()}`);
					return null;
				}
			},
			message: (handle, type, content) => {
				switch(type) {
					case 0:
						((handle.task && handle.task.info) ?? logchannel.info)(content);
						vscode.window.showInformationMessage(content);
						break;
					case 1:
						((handle.task && handle.task.warn) ?? logchannel.warn)(content);
						vscode.window.showWarningMessage(content);
						break;
					case 2:
						((handle.task && handle.task.error) ?? logchannel.error)(content);
						vscode.window.showErrorMessage(content);
						break;
				}
				return Promise.resolve();
			},
			sleep: time => new Promise((resolve, reject) => setTimeout(resolve), time),
			log: (handle, type, message) => {
				switch(type) {
					case 1:
						((handle.task && handle.task.trace) ?? logchannel.trace)(message);
						break;
					case 2:
						((handle.task && handle.task.debug) ?? logchannel.debug)(message);
						break;
					case 3:
						((handle.task && handle.task.info) ?? logchannel.info)(message);
						break;
					case 4:
						((handle.task && handle.task.warn) ?? logchannel.warn)(message);
						break;
					case 5:
						((handle.task && handle.task.error) ?? logchannel.error)(message);
						break;
				}
			},
			requestRequiredParameter: async (handle, name, type, values) => {
				if(!handle.askForInput) {
					return;
				}
				if(type === "boolean") {
					values ??= ["true", "false"];
				}
				var value = undefined;
				var acceptYAML = false;
				if(values && values.length) {
					value = await vscode.window.showQuickPick(values, {
						canPickMany: type.endsWith("List"),
						placeHolder: "value",
						prompt: name,
						ignoreFocusOut: true,
						title: "Select the required Parameter " + name + " of Type " + type
					});
				} else {
					switch(type) {
						case "stringList":
						case "legacyObject":
						case "object":
						case "step":
						case "stepList":
						case "job":
						case "jobList":
						case "deployment":
						case "deploymentList":
						case "stage":
						case "stageList":
						case "container":
						case "containerList":
							acceptYAML = true;
							break;
					}
					value = await vscode.window.showInputBox({
						ignoreFocusOut: true,
						placeHolder: "value",
						prompt: name,
						title: "Provide required Parameter " + name + " of Type " + type + (acceptYAML ?  " in YAML notation" : "as free text"),
					});
				}
				if(value === undefined) {
					value = null;
					handle.stopTask = true;
					return null;
				}
				if(!acceptYAML) {
					value = JSON.stringify(value)
				}
				handle.customConfig ??= {};
				handle.customConfig.parameters ??= {};
				return handle.customConfig.parameters[name] = handle.parameters[name] = value;
			},
			error: async (handle, message) => {
				await handle.error(message);
			},
			autocompletelist: async (handle, completions) => {
				handle.autocompletelist = JSON.parse(completions);
			},
			semTokens: async (handle, completions) => {
				if(handle.enableSemTokens){
					handle.semTokens = completions;
				}
			},
			hoverResult: async (handle, range, content) => {
				handle.hover = { range: JSON.parse(range), content };
			}
		});
		logchannel.appendLine("Starting extension main to keep dotnet alive");
		runtime.runMainAndExit("ext-core", []);
		logchannel.appendLine("Runtime is now ready");
		return runtime;
	}).catch(async ex => {
		// Failed to load, allow retry
		loadingPromise = null;
		await vscode.window.showErrorMessage("Failed to load .net: " + ex.toString());
	});

	var defcollection = vscode.languages.createDiagnosticCollection();
	var expandAzurePipeline = async (validate, repos, vars, params, callback, fspathname, error, task, collection, state, skipAskForInput, syntaxOnly, schema, pos, autocompletelist) => {
		collection ??= defcollection;
		var textEditor = vscode.window.activeTextEditor;
		if(!textEditor && !fspathname) {
			await vscode.window.showErrorMessage("No active TextEditor");
			return;
		}
		var oldConf = vscode.workspace.getConfiguration("azure-pipelines");
		var conf = vscode.workspace.getConfiguration("azure-pipelines-vscode-ext");
		var repositories = {};
		for(var repo of [...(oldConf.repositories ?? []), ...(conf.repositories ?? [])]) {
			var line = repo.split("=");
			var name = line.shift();
			repositories[name] = line.join("=");
		}
		if(repos) {
			for(var name in repos) {
				repositories[name] = repos[name];
			}
		}
		var variables = {};
		for(var repo of [...(oldConf.variables ?? []), ...(conf.variables ?? [])]) {
			var line = repo.split("=");
			var name = line.shift();
			variables[name] = line.join("=");
		}
		if(vars) {
			for(var name in vars) {
				variables[name] = vars[name];
			}
		}
		var parameters = {};
		if(params) {
			for(var name in params) {
				parameters[name] = JSON.stringify(params[name]);
			}
		} else {
			for(var repo of [...(oldConf.parameters ?? []), ...(conf.parameters ?? [])]) {
				var line = repo.split("=");
				var name = line.shift();
				parameters[name] = line.join("=");
			}
		}

		var runtime = await runtimePromise();
		var { name, base, skipCurrentEditor, filename } = await locatePipeline(fspathname, name, textEditor);
		var handle = { hasErrors: false, base: base, skipCurrentEditor: skipCurrentEditor, textEditor: textEditor, filename: filename, repositories: repositories, parameters: parameters, error: (async jsonex => {
			if(autocompletelist?.disableErrors) {
				return;
			}
			var items = [];
			var pex = JSON.parse(jsonex);
			for(var ex of pex.Errors) {
				var matched = false;
				var err = null;
				let i = ex.indexOf(" (Line: ");
				if(i !== -1 && !matched) {
					let m = ex.substring(i).match(/^ \(Line: (\d+), Col: (\d+)\): (.*)$/);
					if(m) {
						err = [m.shift(), ex.substring(0, i), ...m];
					}
				}
				if(err) {
					var ref = err[1];
					var row = parseInt(err[2]) - 1;
					var column = parseInt(err[3]) - 1;
					var msg = err[4];
					var range = new vscode.Range(new vscode.Position(row, column), new vscode.Position(row, integer.MAX_VALUE));
					var diag = new vscode.Diagnostic(range, msg, vscode.DiagnosticSeverity.Error);
					var uri = handle.refToUri[ref];
					if(uri) {
						matched = true;
						items.push([uri, [diag]]);
					}
				}
				err = null;
				if(i !== -1 && !matched) {
					let m = ex.substring(i - 1).match(/^: \(Line: (\d+), Col: (\d+), Idx: \d+\) - \(Line: (\d+), Col: (\d+), Idx: \d+\): (.*)$/);
					if(m) {
						err = [m.shift(), ex.substring(0, i - 1), ...m];
					}
				}
				if(err) {
					var ref = err[1];
					var row = parseInt(err[2]) - 1;
					var column = parseInt(err[3]) - 1;
					var rowEnd = parseInt(err[4]) - 1;
					var columnEnd = parseInt(err[5]) - 1;
					var msg = err[6];
					var range = new vscode.Range(new vscode.Position(row, column), new vscode.Position(rowEnd, columnEnd));
					var diag = new vscode.Diagnostic(range, msg, vscode.DiagnosticSeverity.Error);
					var uri = handle.refToUri[ref];
					if(uri) {
						matched = true;
						items.push([uri, [diag]]);
					}
				}
				err = !matched ? ex.match(/^([^:]+): (.*)$/) : null;
				if(err) {
					var ref = err[1];
					var msg = err[2];
					var range = new vscode.Range(new vscode.Position(0, 0), new vscode.Position(0, 0));
					var diag = new vscode.Diagnostic(range, msg, vscode.DiagnosticSeverity.Error);
					var uri = handle.refToUri[ref];
					if(uri) {
						matched = true;
						items.push([uri, [diag]]);
					}
				}
				if(!matched) {
					var uri = handle.refToUri[`(self)/${handle.filename}`];
					var range = new vscode.Range(new vscode.Position(0, 0), new vscode.Position(0, 0));
					var diag = new vscode.Diagnostic(range, ex, vscode.DiagnosticSeverity.Error);
					if(uri) {
						items.push([uri, [diag]]);
					}
				}
			}
			for(var uri of handle.referencedFiles) {
				if(uri) {
					items.push([uri, []]);
				}
			}
			handle.hasErrors = true;
			collection.set(items);
			if(!error) {
				await vscode.window.showErrorMessage(pex.Message);
				return;
			}
			return await error(pex.Message);
		}), referencedFiles: [], refToUri: {}, task: task, askForInput: !skipAskForInput };
		if(!syntaxOnly && !schema) {
			var uri = handle.base.with({ path: joinPath(handle.base.path, filename) });
			var scontent = null;
			var textDocument = vscode.workspace.textDocuments.find(t => t.uri.toString() === uri.toString());
			if(textDocument) {
				scontent = textDocument.getText();
			} else {
				// Read template references via filesystem api
				var content = await vscode.workspace.fs.readFile(uri);
				scontent = new TextDecoder().decode(content);
			}
			var obj = null;
			try {
				obj ??= jsYaml.load(scontent);
			} catch {
				obj = {};
			}
			schema = extractSchema(obj)
			if(schema != null) {
				vscode.window.showWarningMessage("Please run this command on your root pipeline and not on a nested template detected as: " + schema);
			}
		}
		handle.enableSemTokens = autocompletelist && ('enableSemTokens' in autocompletelist);

		var result = syntaxOnly
		                ? await runtime.BINDING.bind_static_method("[ext-core] MyClass:ParseCurrentPipeline")(handle, filename, schema ?? null, pos ? pos.character + 1 : 0, pos ? pos.line + 1 : 0, (handle.enableSemTokens || pos) ? true : false)
						: await runtime.BINDING.bind_static_method("[ext-core] MyClass:ExpandCurrentPipeline")(handle, filename, JSON.stringify(variables), JSON.stringify(parameters), (error && true) == true, schema)

        if(pos) {
			autocompletelist.autocompletelist = handle.autocompletelist
			autocompletelist.hover = handle.hover
		}
		if(handle.enableSemTokens) {
			autocompletelist.semTokens = handle.semTokens
		}
		// Ask for saving the changed repository setting
		if(handle.customConfig?.repositories) {
			await (async () => {
				var result = await vscode.window.showQuickPick(["No", /* "Save (Workspace Folder)", */ "Save (Workspace)", "Save (Global)"], { placeHolder: "Remember chosen external repositories in extension settings?", ignoreFocusOut: true });
				var target = null;
				switch(result) {
					// Broken?, can we check if it is working
					case "Save (Workspace Folder)":
						target = vscode.ConfigurationTarget.WorkspaceFolder;
						break;
					case "Save (Workspace)":
						target = vscode.ConfigurationTarget.Workspace;
						break;
					case "Save (Global)":
						target = vscode.ConfigurationTarget.Global;
						break;
					default:
						break;
				}
				if(target !== null) {
					var config = vscode.workspace.getConfiguration("azure-pipelines-vscode-ext");
					for(var key in handle.customConfig.repositories) {
						config.repositories ??= [];
						config.repositories.push(`${key}=${handle.customConfig.repositories[key]}`);
					}
					await config.update("repositories", config.repositories, target);
				}
			})().catch(() => {});
		}
		// Ask for saving as new custom Task of this extension
		if(handle.customConfig?.parameters && handle.base) {
			var workspaceFolder = vscode.workspace.getWorkspaceFolder(handle.base);
			await (async () => {
				if(workspaceFolder) {
					var tasks = await vscode.workspace.fs.readFile(workspaceFolder.uri.with({ path: joinPath(workspaceFolder.uri.path, ".vscode/tasks.json") })).catch(() => null);
					if(tasks) {
						tasks = new TextDecoder().decode(tasks);
					} else {
						tasks = JSON.stringify({ version: "2.0.0", tasks: [] }, null, 4);
					}
				}
				var result = await vscode.window.showQuickPick(["No",  "Add to .vscode/tasks.json"], { placeHolder: "Remember chosen required Parameters in a new VSCode Task?", ignoreFocusOut: true });
				if(result === "Add to .vscode/tasks.json") {
					try {
						var convertedParam = {};
						for(var name in handle.customConfig.parameters) {
							try {
								var yml = handle.customConfig.parameters[name];
								var js = runtime.BINDING.bind_static_method("[ext-core] MyClass:YAMLToJson")(yml);
								convertedParam[name] = JSON.parse(js);
							} catch(ex) {
								console.log(ex);
							}
						}
						var existingTasks = await vscode.tasks.fetchTasks();
						// Generate a unique label
						var label = `${validate ? "Validate" : "Expand"} Pipeline ${handle.filename}`;
						var i = 0;
						while(existingTasks.find(t => t.name === label)) {
							label = `${handle.filename} (${++i})`;
						}

						var nTasks = applyEdits(tasks, modify(tasks, ["tasks", -1], {
							"type": "azure-pipelines-vscode-ext",
							"label": label,
							"program": "${workspaceFolder}/" + handle.filename,
							"repositories": {},
							"parameters": convertedParam,
							"variables": {},
							"preview": !validate,
							"watch": true
						}, { formattingOptions: { insertSpaces: true, tabSize: 4 }, isArrayInsertion: true }));
						await vscode.workspace.fs.createDirectory(workspaceFolder.uri.with({ path: joinPath(workspaceFolder.uri.path, ".vscode") }));
						await vscode.workspace.fs.writeFile(workspaceFolder.uri.with({ path: joinPath(workspaceFolder.uri.path, ".vscode/tasks.json") }), new TextEncoder().encode(nTasks));
					} catch {
						
					}
				}
			})().catch(() => {});
		}
		if(state) {
            state.referencedFiles = handle.referencedFiles;
			if(!skipAskForInput) {
				state.repositories = handle.repositories;
				var rawparams = {};
				var binding = runtime.BINDING.bind_static_method("[ext-core] MyClass:YAMLToJson");
				for(var name in handle.parameters) {
					try {
						var yml = handle.parameters[name];
						var js = binding(yml);
						rawparams[name] = JSON.parse(js);
					} catch(ex) {
						console.log(ex);
					}
				}
				state.parameters = rawparams;
			}
        }

		if(result || syntaxOnly) {
			logchannel.debug(result);
			if(!handle.hasErrors && !autocompletelist?.disableErrors) {
				var items = [];
				for(var uri of handle.referencedFiles) {
					items.push([uri, []]);
				}
				collection.set(items);
			}
			if(validate) {
				if(!handle.hasErrors) {
					await vscode.window.showInformationMessage("No issues found");
				}
			} else if(callback) {
				callback(result);
			} else {
				await vscode.workspace.openTextDocument({ language: "yaml", content: result });
			}
		}
	};

	var expandAzurePipelineCommand = () => {
		return vscode.tasks.executeTask(
			new vscode.Task({
					type: "azure-pipelines-vscode-ext",
					program: vscode.window.activeTextEditor?.document?.uri?.toString(),
					watch: true,
					preview: true,
					autoClosePreview: true
				},
				vscode.TaskScope.Workspace,
				"Azure Pipeline Preview (watch)",
				"azure-pipelines",
				executor,
				null
			)
		);
	}

	var validateAzurePipelineCommand = () => {
		return vscode.tasks.executeTask(
			new vscode.Task({
					type: "azure-pipelines-vscode-ext",
					program: vscode.window.activeTextEditor?.document?.uri?.toString(),
					watch: true,
				},
				vscode.TaskScope.Workspace,
				"Azure Pipeline Validate (watch)",
				"azure-pipelines",
				executor,
				null
			)
		);
	}

	var checkSyntaxAzurePipelineCommand = schema => {
		return vscode.tasks.executeTask(
			new vscode.Task({
					type: "azure-pipelines-vscode-ext",
					program: vscode.window.activeTextEditor?.document?.uri?.toString(),
					syntaxOnly: true,
					watch: true,
					schema: schema,
				},
				vscode.TaskScope.Workspace,
				"Azure Pipeline Syntax Check (watch)",
				"azure-pipelines",
				executor,
				null
			)
		);
	}

	context.subscriptions.push(vscode.commands.registerCommand('azure-pipelines-vscode-ext.expandAzurePipeline', () => expandAzurePipelineCommand()));

	context.subscriptions.push(vscode.commands.registerCommand('azure-pipelines-vscode-ext.validateAzurePipeline', () => validateAzurePipelineCommand()));
	
	context.subscriptions.push(vscode.commands.registerCommand('extension.expandAzurePipeline', () => expandAzurePipelineCommand()));

	context.subscriptions.push(vscode.commands.registerCommand('extension.validateAzurePipeline', () => validateAzurePipelineCommand()));

	context.subscriptions.push(vscode.commands.registerCommand('azure-pipelines-vscode-ext.copyTaskAsCommand', () => {
		vscode.tasks.fetchTasks({ type: "azure-pipelines-vscode-ext"  }).then(tasks =>
			vscode.window.showQuickPick(tasks.filter(t => t.definition.program).map(t => t.name), { placeHolder: "Select Task to copy as Runner.Client Command" }).then(async name => {
				if(name) {
					var task = tasks.find(t => t.name === name);
					if(task) {
						var { filename } = await locatePipeline(vsVars(task.definition.program), null, null);
						let quiet = task.definition.preview ? " -q" : "";
						await vscode.env.clipboard.writeText(`Runner.Client azexpand${quiet} -W ${cliQuote(filename)} ${task.definition.parameters ? Object.entries(task.definition.parameters).reduce((p, c) => `${p} --input ${cliQuote(`${c[0]}=${typeof c[1] === 'object' ? JSON.stringify(c[1]) : c[1]}`)}`, "") : ""}${task.definition.variables ? Object.entries(task.definition.variables).reduce((p, c) => `${p} --var ${cliQuote(`${c[0]}=${c[1]}'`)}`, "") : ""}${task.definition.repositories ? Object.entries(task.definition.repositories).reduce((p, c) => `${p} --local-repository ${cliQuote(`${c[0]}=${c[1]}`)}`, "") : ""}`);
						await vscode.window.showInformationMessage("Copied to clipboard");
						return;
					}
				}
				await vscode.window.showErrorMessage("No Task selected");
			}).catch(err => vscode.window.showErrorMessage(err.toString())));
	}));

	var statusbar = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right);
	context.subscriptions.push(statusbar);
	statusbar.tooltip = "Configure this Icon in Settings";

	var update = function(status) {
		statusbar.text = `$(${status}) Azure Pipelines Tools`;
	}
	update("pass");
	var syntaxChecks = vscode.languages.createDiagnosticCollection("Syntax Checks");
	statusbar.command = {
		command: 'azure-pipelines-vscode-ext.checkSyntaxAzurePipeline',
		arguments: [null, syntaxChecks, null]
	};

	context.subscriptions.push(syntaxChecks);

	var extractSchema = obj => {
		var varTempl = obj.variables;
		var stageTempl = obj.stages instanceof Array;
		var jobsTempl = obj.jobs instanceof Array;
		var stepsTempl = obj.steps instanceof Array;
		var isTypedTemplate = false;
		var mustBeTempl = obj.parameters && (!(isTypedTemplate = obj.parameters instanceof Array) || obj.parameters.find(x => x && x.type === "legacyObject"))
		var schema = null;
		if(mustBeTempl && isTypedTemplate && obj.extends) {
			schema = "extend-template-root"
		} else if(mustBeTempl && (!varTempl && stageTempl && !jobsTempl && !stepsTempl)) {
			schema = "stage-template-root"
		} else if(mustBeTempl && (!varTempl && !stageTempl && jobsTempl && !stepsTempl)) {
			schema = "job-template-root"
		} else if(mustBeTempl && (!varTempl && !stageTempl && !jobsTempl && stepsTempl)) {
			schema = "step-template-root"
		} else if((mustBeTempl || !obj.extends) && (varTempl && !stageTempl && !jobsTempl && !stepsTempl)) {
			schema = "variable-template-root"
		}
		return schema;
	}
	
	var semHighlight = null;
	var autoComplete = null;
	var hover = null;
	var semHighlightSettingChanged = () => {
		if(vscode.workspace.getConfiguration("azure-pipelines-vscode-ext").get("enable-semantic-highlighting")) {
			semHighlight = vscode.languages.registerDocumentSemanticTokensProvider([
				{
					language: "yaml"
				},
				{
					language: "azure-pipelines"
				}
			], {
				provideDocumentSemanticTokens: async (doc, token) => {
					var obj;
					var getSchema = () => {
						try {
							obj ??= jsYaml.load(doc.getText());
						} catch {
							obj = {};
						}
						return extractSchema(obj);
					}
					if(doc.languageId === "azure-pipelines" || doc.languageId === "yaml" && (obj = checkIsPipelineByContent(doc.getText()))) {
						var data = {enableSemTokens: true, disableErrors: true};
						await expandAzurePipeline(false, null, null, null, () => {
						}, doc.uri.toString(), () => {
						}, null, null, null, true, true, getSchema(), null, data);
						var semTokens = data.semTokens || new Uint32Array();
						return new vscode.SemanticTokens(semTokens);
					}
					return null;
				}
			}, new vscode.SemanticTokensLegend(["variable","parameter","function","property","constant","punctuation","string"], ["readonly","defaultLibrary","numeric"]));
			context.subscriptions.push(semHighlight);
		} else {
			semHighlight?.dispose();
		}
	};
	var autoCompleteSettingChanged = () => {
		if(vscode.workspace.getConfiguration("azure-pipelines-vscode-ext").get("enable-auto-complete")) {
			autoComplete = vscode.languages.registerCompletionItemProvider([
				{
					language: "yaml"
				},
				{
					language: "azure-pipelines"
				}
			], {
				provideCompletionItems: async (doc, pos, token, context) => {
					var obj;
					var getSchema = () => {
						try {
							obj ??= jsYaml.load(doc.getText());
						} catch {
							obj = {};
						}
						return extractSchema(obj);
					}
					if(doc.languageId === "azure-pipelines" || doc.languageId === "yaml" && (obj = checkIsPipelineByContent(doc.getText()))) {
						var data = {autocompletelist: [], disableErrors: true};
						await expandAzurePipeline(false, null, null, null, () => {
						}, doc.uri.toString(), () => {
						}, null, null, null, true, true, getSchema(), pos, data);
						for(var item of data.autocompletelist) {
							if(item.insertText && item.insertText.value) {
								item.insertText = new vscode.SnippetString(item.insertText.value)
							}
							if(item.range) {
								if(item.range.inserting) {
									item.range.inserting = new vscode.Range(new vscode.Position(item.range.inserting.start.line, item.range.inserting.start.character), new vscode.Position(item.range.inserting.end.line, item.range.inserting.end.character))
								}
								if(item.range.replacing) {
									item.range.replacing = new vscode.Range(new vscode.Position(item.range.replacing.start.line, item.range.replacing.start.character), new vscode.Position(item.range.replacing.end.line, item.range.replacing.end.character))
								}
							}
							if(item.documentation) {
								item.documentation = new vscode.MarkdownString(item.documentation.value, item.supportThemeIcons)
							}
						}
						return data.autocompletelist
					}
					return null;
				}
			});
			context.subscriptions.push(autoComplete);
		} else {
			autoComplete?.dispose();
		}
	};
	var hoverSettingChanged = () => {
		if(vscode.workspace.getConfiguration("azure-pipelines-vscode-ext").get("enable-hover")) {
			hover = vscode.languages.registerHoverProvider([
				{
					language: "yaml"
				},
				{
					language: "azure-pipelines"
				}
			], {
				provideHover: async (doc, pos, token) => {
					var obj;
					var getSchema = () => {
						try {
							obj ??= jsYaml.load(doc.getText());
						} catch {
							obj = {};
						}
						return extractSchema(obj);
					}
					if(doc.languageId === "azure-pipelines" || doc.languageId === "yaml" && (obj = checkIsPipelineByContent(doc.getText()))) {
						var data = {autocompletelist: [], disableErrors: true};
						await expandAzurePipeline(false, null, null, null, () => {
						}, doc.uri.toString(), () => {
						}, null, null, null, true, true, getSchema(), pos, data);
						if(data.hover && data.hover.range && data.hover.content) {
							return new vscode.Hover(new vscode.MarkdownString(data.hover.content, true), data.hover.range)
						}
					}
					return null;
				}
			});
			context.subscriptions.push(hover);
		} else {
			hover?.dispose();
		}
	};
	vscode.workspace.onDidChangeConfiguration(conf => {
		if(conf.affectsConfiguration("azure-pipelines-vscode-ext.enable-semantic-highlighting")) {
			semHighlightSettingChanged();
		}
		if(conf.affectsConfiguration("azure-pipelines-vscode-ext.enable-auto-complete")) {
			autoCompleteSettingChanged();
		}
		if(conf.affectsConfiguration("azure-pipelines-vscode-ext.enable-hover")) {
			hoverSettingChanged();
		}
	})
	semHighlightSettingChanged();
	autoCompleteSettingChanged();
	hoverSettingChanged();

	context.subscriptions.push(vscode.commands.registerCommand(statusbar.command.command, async (file, collection, obj) => {
		var getSchema = () => {
            try {
				obj ??= jsYaml.load(vscode.window.activeTextEditor.document.getText());
            } catch {
                obj = {};
            }
            return extractSchema(obj);
        }
		if(collection) {
			var hasError = false;
			update("sync~spin");
			await new Promise((resolve) => {
                setTimeout(resolve, 1);
            });
			await expandAzurePipeline(false, null, null, null, () => {
				if(!hasError) {
					update("pass");
				}
			}, null, () => {
				hasError = true;
				update("error");
			}, null, collection, null, true, true, getSchema());
			await new Promise((resolve) => {
                setTimeout(resolve, 1);
            });
		} else {
			await checkSyntaxAzurePipelineCommand(getSchema());
		}
 	}));

	var checkAllIsIn = (obj, allowed) => {
		for(var k in obj) {
			if(allowed.indexOf(k) === -1) {
				return false;
			}
		}
		return true;
	}

	var checkIsPipelineByContent = (content) => {
		try {
			var obj = jsYaml.load(content);
			return ((obj.trigger || obj.pr || obj.resources && (obj.resources.builds instanceof Array || obj.resources.containers instanceof Array || obj.resources.pipelines instanceof Array || obj.resources.repositories instanceof Array || obj.resources.webhooks instanceof Array || obj.resources.packages instanceof Array) || obj.schedules instanceof Array || obj.lockBehavior instanceof String || obj.variables instanceof Object || obj.variables instanceof Array || obj.parameters instanceof Object || obj.parameters instanceof Array) && (obj.stages instanceof Array || obj.jobs instanceof Array || obj.steps instanceof Array)
				|| obj.extends && obj.extends.template	
				|| obj.steps instanceof Array && obj.steps.find(x => x && (x.task || x.script !== undefined || x.bash !== undefined || x.pwsh !== undefined || x.powershell !== undefined || x.template !== undefined))
				|| obj.jobs instanceof Array && obj.jobs.find(x => x && (x.job !== undefined || x.deployment !== undefined || x.template !== undefined))
				|| obj.stages instanceof Array && obj.stages.find(x => x && (x.stage !== undefined || x.template !== undefined))
				|| obj.variables instanceof Array && obj.variables.find(x => x && (x.name !== undefined && x.value !== undefined || x.group !== undefined || x.template !== undefined))
				|| obj.variables && checkAllIsIn(obj, [ "parameters", "variables" ])
			) ? obj : null;
		} catch {

		}
		return null;
	}

	var checkIsPipeline = () => {
		try {
			return checkIsPipelineByContent(vscode.window.activeTextEditor.document.getText());
		} catch {

		}
		return null;
	}

	var inProgress = false;
	var lastLanguageChange = undefined;

	var onLanguageChanged = (languageId, reset) => {
		// Reduce CPU load
		setTimeout(() => {
			var obj = null;
			if(!inProgress || reset) {
				lastLanguageChange = undefined;
				inProgress = true;
				var conf = vscode.workspace.getConfiguration("azure-pipelines-vscode-ext");
				if(languageId === "azure-pipelines" || languageId === "yaml" && (obj = checkIsPipeline())) {
					if(conf.get("disable-status-bar")) {
						statusbar.hide();
					} else {
						statusbar.show();
					}
					if(!conf.get("disable-auto-syntax-check")) {
						var _finally = () => {
                            setTimeout(() => {
								var queue = lastLanguageChange;
								if(queue !== undefined) {
									onLanguageChanged(queue, true);
								} else {
									inProgress = false;
								}
                            }, 1);
						};
						vscode.commands.executeCommand(statusbar.command.command, null, syntaxChecks, obj).then(_finally, (err) => {
							_finally();
						});
					} else {
						defcollection.clear();
					}
				} else {
					if(vscode.window.activeTextEditor && vscode.window.activeTextEditor.document) {
						syntaxChecks.set(vscode.window.activeTextEditor.document.uri, []);
					}
					statusbar.hide();
					inProgress = false;
				}
			} else {
				lastLanguageChange = languageId;
			}
		}, 1);
	};
	var z = 0;
	vscode.debug.registerDebugAdapterDescriptorFactory("azure-pipelines-vscode-ext", {
		createDebugAdapterDescriptor: (session, executable) => {
			return new vscode.DebugAdapterInlineImplementation(new AzurePipelinesDebugSession(virtualFiles, `azure-pipelines-preview-${z++}.yml`, expandAzurePipeline, arg => changeDoc.fire(arg)));
		}
	});

	var onTextEditChanged = texteditor => onLanguageChanged(texteditor && texteditor.document && texteditor.document.languageId ? texteditor.document.languageId : null);
	context.subscriptions.push(vscode.window.onDidChangeActiveTextEditor(onTextEditChanged));
	context.subscriptions.push(vscode.workspace.onDidChangeTextDocument(ev => {
		if(vscode.window.activeTextEditor.document === ev.document) {
			onLanguageChanged(ev.document && ev.document.languageId ? ev.document.languageId : null);
		}
	}));
	context.subscriptions.push(vscode.workspace.onDidCloseTextDocument(document => {
		if(vscode.window.activeTextEditor.document === document) {
			onLanguageChanged(null);
		}
	}));
	context.subscriptions.push(vscode.workspace.onDidOpenTextDocument(document => {
		if(vscode.window.activeTextEditor.document === document) {
			onLanguageChanged(document && document.languageId ? document.languageId : null);
		}
	}));
	onTextEditChanged(vscode.window.activeTextEditor);
	var executor = new vscode.CustomExecution(async def => {
		const writeEmitter = new vscode.EventEmitter();
		const closeEmitter = new vscode.EventEmitter();
		var self = {
			virtualFiles: virtualFiles,
			name: `azure-pipelines-preview-${z++}.yml`,
			changed: arg => changeDoc.fire(arg),
			disposables: [],
			parameters: null,
			repositories: null
		};
		self.collection = vscode.languages.createDiagnosticCollection(self.name);
		self.disposables.push(self.collection);
		var task = {
			trace: message => writeEmitter.fire("\x1b[34m[trace]" + message.replace(/\r?\n/g, "\r\n") + "\x1b[0m\r\n"),
			debug: message => writeEmitter.fire("\x1b[35m[debug]" + message.replace(/\r?\n/g, "\r\n") + "\x1b[0m\r\n"),
			info: message => writeEmitter.fire("\x1b[32m[info]" + message.replace(/\r?\n/g, "\r\n") + "\x1b[0m\r\n"),
			warn: message => writeEmitter.fire("\x1b[33m[warn]" + message.replace(/\r?\n/g, "\r\n") + "\x1b[0m\r\n"),
			error: message => writeEmitter.fire("\x1b[31m[error]" + message.replace(/\r?\n/g, "\r\n") + "\x1b[0m\r\n"),
		};
		let config = vscode.workspace.getConfiguration("azure-pipelines-vscode-ext");
		if(config.get("create-task-log-files")) {
			let taskChannel = vscode.window.createOutputChannel(self.name, { log: true });
			self.disposables.push(taskChannel);
			for(var t in task) {
				let type = t;
				let org = task[type];
				task[type] = message => {
					taskChannel[type](message);
					org(message);
				}
			}
		}
		var requestReOpen = true;
		var documentClosed = true;
		var doc = null;
		var uri = vscode.Uri.from({
			scheme: "azure-pipelines-vscode-ext",
			path: self.name
		});
		var reopenPreviewIfNeeded = async () => {
			if(requestReOpen) {
				if(documentClosed || !doc || doc.isClosed) {
					self.virtualFiles[self.name] = "";
					doc = await vscode.workspace.openTextDocument(uri);
				}
				await vscode.window.showTextDocument(doc, { preview: true, viewColumn: vscode.ViewColumn.Two, preserveFocus: true });
				requestReOpen = false;
				documentClosed = false;
			}
		};
		var close = () => {
			console.log("closed");
			writeEmitter.dispose();
			closeEmitter.fire(0);
			closeEmitter.dispose();
			if(self.watcher) {
				self.watcher.dispose();
			}
			if(self.disposables) {
				for(var disp of self.disposables) {
					disp.dispose();
				}
			}
		}
		return {
			close: close,
			onDidWrite: writeEmitter.event,
			onDidClose: closeEmitter.event,
			open: () => {
				var args = def;
				if(args.preview) {
					var previewIsOpen = () => vscode.window.tabGroups && vscode.window.tabGroups.all && vscode.window.tabGroups.all.some(g => g && g.tabs && g.tabs.some(t => t && t.input && t.input["uri"] && t.input["uri"].toString() === uri.toString()));
					self.disposables.push(vscode.window.tabGroups.onDidChangeTabs(e => {
						if(!previewIsOpen()) {
							if(!requestReOpen) {
								console.log(`file closed ${self.name}`);
							}
							requestReOpen = true;
						} else {
							if(requestReOpen) {
								console.log(`file opened ${self.name}`);
							}
							requestReOpen = false;
						}
					}));
					self.disposables.push(vscode.workspace.onDidCloseTextDocument(adoc => {
						if(doc === adoc) {
							if(args.autoClosePreview) {
								close();
								return;
							}
							delete self.virtualFiles[self.name];
							console.log(`document closed ${self.name}`);
							documentClosed = true;
						}
					}));
				}

				var inProgress = false;
				var waiting = false;
				var run = async (askForInput) => {
					if(inProgress) {
						waiting = true;
						return;
					}
					waiting = false;
					inProgress = true;
					try {
						var hasErrors = false;
						await new Promise((resolve) => {
                            setTimeout(resolve, 1);
                        });
						await expandAzurePipeline(false, self.repositories ?? args.repositories, args.variables, self.parameters ?? args.parameters, async result => {
							if(!args.syntaxOnly) {
								task.info(result);
								if(args.preview) {
									await reopenPreviewIfNeeded();
									self.virtualFiles[self.name] = result;
									self.changed(uri);
								} else if(!hasErrors) {
									vscode.window.showInformationMessage("No Issues found");
								}
							}
						}, args.program, async errmsg => {
							hasErrors = true;
							task.error(errmsg);
							if(!args.syntaxOnly) {
								if(args.preview) {
									await reopenPreviewIfNeeded();
									self.virtualFiles[self.name] = errmsg;
									self.changed(uri);
								} else {
									vscode.window.showErrorMessage(errmsg);
								}
							}
						}, task, self.collection, self, !askForInput, args.syntaxOnly, args.schema);
					} catch(err) {
                        task.error(err?.toString() ?? "Unknown Error");
					}
					await new Promise((resolve) => {
                        setTimeout(resolve, 1);
                    });
					inProgress = false;
					if(!args.watch) {
						close();
					}
					if(waiting) {
						run();
					}
				};
				run(true);
				if(def.watch) {
					var isReferenced = uri => self.referencedFiles.find((u) => u.toString() === uri.toString());
					// Reload yaml on file and textdocument changes
					self.disposables.push(vscode.workspace.onDidChangeTextDocument(ch => {
						var doc = ch.document;
						if(isReferenced(doc.uri)) {
							console.log(`changed(doc): ${doc.uri.toString()}`);
							run();
						}
					}));
					
					self.watcher = vscode.workspace.createFileSystemWatcher("**/*.{yml,yaml}");
					self.watcher.onDidCreate(e => {
						if(isReferenced(e)) {
							console.log(`created: ${e.toString()}`);
							run();
						}
					});
					self.watcher.onDidChange(e => {
						if(isReferenced(e) && !vscode.workspace.textDocuments.find(t => t.uri.toString() === e.toString())) {
							console.log(`changed: ${e.toString()}`);
							run();
						}
					});
					self.watcher.onDidDelete(e => {
						if(isReferenced(e)) {
							console.log(`deleted: ${e.toString()}`);
							run();
						}
					});
				}
			}
		}
	});
	context.subscriptions.push(vscode.tasks.registerTaskProvider("azure-pipelines-vscode-ext", {
		provideTasks: async token => ([
			new vscode.Task({
					type: "azure-pipelines-vscode-ext",
					variables: {},
					watch: true,
					preview: true
				},
				vscode.TaskScope.Workspace,
				"Azure Pipeline Preview (watch)",
				"azure-pipelines",
				executor,
				null
			)
		]),
		resolveTask: _task => {
			  // resolveTask requires that the same definition object be used.
			  	return new vscode.Task(_task.definition,
					vscode.TaskScope.Workspace,
					"Azure Pipeline Preview (watch)",
					"azure-pipelines",
					executor,
					null
				);
		}
	}));
}

async function locatePipeline(fspathname, name, textEditor) {
	var base = null;

	var skipCurrentEditor = false;
	var filename = null;
	if (fspathname) {
		skipCurrentEditor = true;
		var uris = [vscode.Uri.parse(fspathname), vscode.Uri.file(fspathname)];
		for (var current of uris) {
			var rbase = vscode.workspace.getWorkspaceFolder(current);
			var name = vscode.workspace.asRelativePath(current, false);
			if (rbase && name) {
				base = rbase.uri;
				filename = name;
				break;
			}
		}
		if (filename == null) {
			for (var workspace of (vscode.workspace.workspaceFolders || [])) {
				// Normalize
				var nativepathname = vscode.Uri.file(fspathname).fsPath;
				if (nativepathname.startsWith(workspace.uri.fsPath)) {
					base = workspace.uri;
					filename = vscode.workspace.asRelativePath(workspace.uri.with({ path: joinPath(workspace.uri.path, nativepathname.substring(workspace.uri.fsPath.length).replace(/[\\\/]+/g, "/")) }), false);
					break;
				}
			}
		}
		if (filename == null) {
			// untitled uris will land here
			var current = vscode.Uri.parse(fspathname);
			for (var workspace of (vscode.workspace.workspaceFolders || [])) {
				var workspacePath = workspace.uri.path.replace(/\/*$/, "/");
				if (workspace.uri.scheme === current.scheme && workspace.uri.authority === current.authority && current.path.startsWith(workspacePath)) {
					base = workspace.uri;
					filename = current.path.substring(workspacePath.length);
					break;
				}
			}
			var li = current.path.lastIndexOf("/");
			base ??= current.with({ path: current.path.substring(0, li) });
			filename ??= current.path.substring(li + 1);
			try {
				await vscode.workspace.fs.stat(current);
			} catch {
				// untitled uris cannot be read by readFile
				skipCurrentEditor = false;
			}
		}
	} else if(textEditor !== null) {
		filename = null;
		var current = textEditor.document.uri;
		for (var workspace of (vscode.workspace.workspaceFolders || [])) {
			var workspacePath = workspace.uri.path.replace(/\/*$/, "/");
			if (workspace.uri.scheme === current.scheme && workspace.uri.authority === current.authority && current.path.startsWith(workspacePath)) {
				base = workspace.uri;
				filename = current.path.substring(workspacePath.length);
				break;
			}
		}
		var li = current.path.lastIndexOf("/");
		base ??= current.with({ path: current.path.substring(0, li) });
		filename ??= current.path.substring(li + 1);
	}
	return { name, base, skipCurrentEditor, filename };
}

// this method is called when your extension is deactivated
function deactivate() {}

// eslint-disable-next-line no-undef
export {
	activate,
	deactivate
}
