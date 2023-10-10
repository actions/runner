const vscode = require('vscode');
import { basePaths, customImports } from "./config.js" 

/**
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {
	basePaths.basedir = context.extensionUri.with({ path: context.extensionUri.path + "/build/AppBundle/_framework/blazor.boot.json" }).toString();
	var { dotnet } = require("./build/AppBundle/_framework/dotnet.js");
	customImports["dotnet.runtime.js"] = require("./build/AppBundle/_framework/dotnet.runtime.js");
	customImports["dotnet.native.js"] = require("./build/AppBundle/_framework/dotnet.native.js");

	var runtimePromise = vscode.window.withProgress({
		location: vscode.ProgressLocation.Notification,
		title: "Updating Runtime",
		cancellable: true
	}, async (progress, token) => {
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
			readFile: async (handle, filename) => {
				try {
					// Get current textEditor content for the entrypoint
					var doc = handle.textEditor.document;
					if(handle.filename === filename && doc) {
						return doc.getText();
					}
					// Read template references via filesystem api
					var content = await vscode.workspace.fs.readFile(handle.base.with({ path: handle.base.path + "/" + filename }));	
					var scontent = new TextDecoder().decode(content);
					return scontent;
				} catch {
					return null;
				}
			},
			message: async (type, content) => {
				switch(type) {
					case 0:
						await vscode.window.showInformationMessage(content);
						break;
					case 1:
						await vscode.window.showWarningMessage(content);
						break;
					case 2:
						await vscode.window.showErrorMessage(content);
						break;
				}
			},
			sleep: time => new Promise((resolve, reject) => setTimeout(resolve), time)
		});
		runtime.runMainAndExit("ext-core", []);
		return runtime;
	});

	var expandAzurePipeline = async validate => {
		var textEditor = vscode.window.activeTextEditor;
		if(!textEditor) {
			await vscode.window.showErrorMessage("No active TextEditor");
			return;
		}
		var runtime = await runtimePromise;
		var base = null;
		var filename = null;
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
		var result = await runtime.BINDING.bind_static_method("[ext-core] MyClass:ExpandCurrentPipeline")({ base: base, textEditor: textEditor, filename: filename }, filename);
		if(result) {
			if(validate) {
				await vscode.window.showInformationMessage("No issues found");
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

	var onDocumentChanged = document => {
		if(document && document.languageId && (document.languageId === "azure-pipelines" || document.languageId === "yaml")) {
			statusbar.show();
		} else {
			statusbar.hide();
		}
	};
	var onTextEditChanged = texteditor => onDocumentChanged(texteditor ? texteditor.document : null);
	context.subscriptions.push(vscode.window.onDidChangeActiveTextEditor(onTextEditChanged))
	context.subscriptions.push(vscode.workspace.onDidCloseTextDocument(document => {
		var texteditor = vscode.window.activeTextEditor;
		onDocumentChanged(texteditor && texteditor.document === document ? document : null);
	}));
	context.subscriptions.push(vscode.workspace.onDidOpenTextDocument(document => {
		var texteditor = vscode.window.activeTextEditor;
		onDocumentChanged(texteditor && texteditor.document === document ? document : null);
	}));
	onTextEditChanged(vscode.window.activeTextEditor);
}

// this method is called when your extension is deactivated
function deactivate() {}

// eslint-disable-next-line no-undef
export {
	activate,
	deactivate
}
