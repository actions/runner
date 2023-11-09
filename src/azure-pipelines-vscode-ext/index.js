const vscode = require('vscode');
import { basePaths, customImports } from "./config.js"
import { AzurePipelinesDebugSession } from "./azure-pipelines-debug";

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

	var runtimePromise = vscode.window.withProgress({
		location: vscode.ProgressLocation.Notification,
		title: "Updating Runtime",
		cancellable: true
	}, async (progress, token) => {
		logchannel.appendLine("Updating Runtime");
		var items = 1;
		var citem = 0;
		var runtime = await dotnet.withOnConfigLoaded(async config => {
			items = Object.keys(config.resources.assembly).length;
		}).withConfigSrc(context.extensionUri.with({ path: context.extensionUri.path + "/build/AppBundle/_framework/blazor.boot.json" }).toString()).withResourceLoader((type, name, defaultUri, integrity, behavior) => {
			if(type === "dotnetjs") {
				return name;
			}
			return (async () => {
				if(type === "assembly") {
					if(token.isCancellationRequested) {
						throw new Error("loading Runtime aborted, reload the window to use this extension");
					}
					await progress.report({ message: name, increment: citem++ / items });
				}
				var content = await vscode.workspace.fs.readFile(context.extensionUri.with({ path: context.extensionUri.path + "/build/AppBundle/_framework/" + name }));
				return new Response(content, { status: 200 });
			})();
		}).create();
		runtime.setModuleImports("extension.js", {
			readFile: async (handle, repositoryAndRef, filename) => {
				try {
					var uri = "";
					if(repositoryAndRef) {
						if(handle.repositories && repositoryAndRef in handle.repositories) {
							var base = vscode.Uri.parse(handle.repositories[repositoryAndRef]);
							uri = base.with({ path: base.path + "/" + filename });
						} else {
							var result = await vscode.window.showInputBox({
								ignoreFocusOut: true,
								placeHolder: "value",
								prompt: `${repositoryAndRef} (${filename})`,
								title: "Provide the uri to the required Repository"
							})
							if(result) {
								handle.repositories ??= {};
								handle.repositories[repositoryAndRef] = result;
								var base = vscode.Uri.parse(result);
								uri = base.with({ path: base.path + "/" + filename });
							} else {
								logchannel.error(`Cannot access remote repository ${repositoryAndRef} (${filename})`);
								return null;
							}
						}
					} else {
						// Get current textEditor content for the entrypoint
						var doc = handle.textEditor ? handle.textEditor.document : null;
						if(handle.filename === filename && doc && !handle.skipCurrentEditor) {
							return doc.getText();
						}
						uri = handle.base.with({ path: handle.base.path + "/" + filename });
					}
					// Read template references via filesystem api
					var content = await vscode.workspace.fs.readFile(uri);	
					var scontent = new TextDecoder().decode(content);
					return scontent;
				} catch(ex) {
					logchannel.error(`Failed to access ${filename} (${repositoryAndRef ?? "self"}), error: ${ex.toString()}`);
					return null;
				}
			},
			message: async (type, content) => {
				switch(type) {
					case 0:
						logchannel.info(content);
						await vscode.window.showInformationMessage(content);
						break;
					case 1:
						logchannel.warn(content);
						await vscode.window.showWarningMessage(content);
						break;
					case 2:
						logchannel.error(content);
						await vscode.window.showErrorMessage(content);
						break;
				}
			},
			sleep: time => new Promise((resolve, reject) => setTimeout(resolve), time),
			log: (type, message) => {
				switch(type) {
					case 1:
						logchannel.trace(message);
						break;
					case 2:
						logchannel.debug(message);
						break;
					case 3:
						logchannel.info(message);
						break;
					case 4:
						logchannel.warn(message);
						break;
					case 5:
						logchannel.error(message);
						break;
				}
			},
			requestRequiredParameter: async (handle, name) => {
				return await vscode.window.showInputBox({
					ignoreFocusOut: true,
					placeHolder: "value",
					prompt: name,
					title: "Provide required Variables in yaml notation"
				})
			},
			error: async (handle, message) => {
				await handle.error(message);
			}
		});
		logchannel.appendLine("Starting extension main to keep dotnet alive");
		runtime.runMainAndExit("ext-core", []);
		logchannel.appendLine("Runtime is now ready");
		return runtime;
	});

	var expandAzurePipeline = async (validate, repos, vars, params, callback, fspathname, error) => {
		var textEditor = vscode.window.activeTextEditor;
		if(!textEditor && !fspathname) {
			await vscode.window.showErrorMessage("No active TextEditor");
			return;
		}
		var conf = vscode.workspace.getConfiguration("azure-pipelines");
		var repositories = {};
		for(var repo of conf.repositories ?? []) {
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
		for(var repo of conf.variables ?? []) {
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
			for(var repo of conf.parameters ?? []) {
				var line = repo.split("=");
				var name = line.shift();
				parameters[name] = line.join("=");
			}
		}

		var runtime = await runtimePromise;
		var base = null;

		var skipCurrentEditor = false;
		var filename = null
		if(fspathname) {
			skipCurrentEditor = true;
			var uris = [vscode.Uri.parse(fspathname), vscode.Uri.file(fspathname)];
			for(var current of uris) {
				var rbase = vscode.workspace.getWorkspaceFolder(current);
				var name = vscode.workspace.asRelativePath(current, false);
				if(rbase && name) {
					base = rbase.uri;
					filename = name;
					break;
				}
			}
			if(filename == null) {
				for(var workspace of vscode.workspace.workspaceFolders) {
					if(fspathname.startsWith(workspace.uri.fsPath)) {
						base = workspace.uri;
						filename = vscode.workspace.asRelativePath(workspace.uri.with({path: workspace.uri.path + "/" + fspathname.substring(workspace.uri.fsPath.length).replace(/[\\\/]+/g, "/")}), false);
						break;
					}
				}
			}
		} else {
			filename = null;
			var current = textEditor.document.uri;
			for(var workspace of vscode.workspace.workspaceFolders) {
				var workspacePath = workspace.uri.path.replace(/\/*$/, "/");
				if(workspace.uri.scheme === current.scheme && workspace.uri.authority === current.authority && current.path.startsWith(workspacePath)) {
					base = workspace.uri;
					filename = current.path.substring(workspacePath.length);
					break;
				}
			}
			var li = current.path.lastIndexOf("/");
			base ??= current.with({ path: current.path.substring(0, li)});
			filename ??= current.path.substring(li + 1);
		}	
		var result = await runtime.BINDING.bind_static_method("[ext-core] MyClass:ExpandCurrentPipeline")({ base: base, skipCurrentEditor: skipCurrentEditor, textEditor: textEditor, filename: filename, repositories: repositories, error: error }, filename, JSON.stringify(variables), JSON.stringify(parameters), (error && true) == true);

		if(result) {
			logchannel.debug(result);
			if(validate) {
				await vscode.window.showInformationMessage("No issues found");
			} else if(callback) {
				callback(result);
			} else {
				await vscode.workspace.openTextDocument({ language: "yaml", content: result });
			}
		}
	};

	context.subscriptions.push(vscode.commands.registerCommand('extension.expandAzurePipeline', () => expandAzurePipeline(false)));

	context.subscriptions.push(vscode.commands.registerCommand('extension.validateAzurePipeline', () => expandAzurePipeline(true)));

	var statusbar = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right);

	statusbar.text = "Validate Azure Pipeline";

	statusbar.command = 'extension.validateAzurePipeline';

	var onLanguageChanged = languageId => {
		if(languageId === "azure-pipelines" || languageId === "yaml") {
			statusbar.show();
		} else {
			statusbar.hide();
		}
	};
	var z = 0;
	vscode.debug.registerDebugAdapterDescriptorFactory("azure-pipelines-vscode-ext", {
		createDebugAdapterDescriptor: (session, executable) => {
			return new vscode.DebugAdapterInlineImplementation(new AzurePipelinesDebugSession(virtualFiles, `azure-pipelines-preview-${z++}.yml`, expandAzurePipeline, arg => changeDoc.fire(arg)));
		}
	});

	var onTextEditChanged = texteditor => onLanguageChanged(texteditor && texteditor.document && texteditor.document.languageId ? texteditor.document.languageId : null);
	context.subscriptions.push(vscode.window.onDidChangeActiveTextEditor(onTextEditChanged))
	context.subscriptions.push(vscode.workspace.onDidCloseTextDocument(document => onLanguageChanged(document && document.languageId ? document.languageId : null)));
	context.subscriptions.push(vscode.workspace.onDidOpenTextDocument(document => onLanguageChanged(document && document.languageId ? document.languageId : null)));
	onTextEditChanged(vscode.window.activeTextEditor);
}

// this method is called when your extension is deactivated
function deactivate() {}

// eslint-disable-next-line no-undef
export {
	activate,
	deactivate
}
