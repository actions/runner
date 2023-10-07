// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
const vscode = require('vscode');
import { basePaths, customImports } from "./config.js" 

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed

/**
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {
	basePaths.basedir = context.extensionUri.with({ path: context.extensionUri.path + "/build/AppBundle/_framework/blazor.boot.json" }).toString();
	var { dotnet } = require("./build/AppBundle/_framework/dotnet.js");
	customImports["dotnet.runtime.js"] = require("./build/AppBundle/_framework/dotnet.runtime.js");
	customImports["dotnet.native.js"] = require("./build/AppBundle/_framework/dotnet.native.js");

	var runtimePromise = dotnet.withConfigSrc(context.extensionUri.with({ path: context.extensionUri.path + "/build/AppBundle/_framework/blazor.boot.json" }).toString()).withResourceLoader((type, name, defaultUri, integrity, behavior) => {
		console.log(JSON.stringify({type, name, defaultUri, integrity, behavior}));
		if(type === "dotnetjs") {
			return name;
		}
		return (async () => {
			var content = await vscode.workspace.fs.readFile(context.extensionUri.with({ path: context.extensionUri.path + "/build/AppBundle/_framework/" + name }));
			
			return new Response(content, { status: 200 });
		})();
	}).create();

	let disposable = vscode.commands.registerCommand('extension.expandAzurePipeline', async () => {
		var runtime = await runtimePromise;
		var base = null;
		var filename = null;
		var current = vscode.window.activeTextEditor.document.uri;
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
		runtime.setModuleImports("extension.js", {
			readFile: async filename => {
				try {
					var content = await vscode.workspace.fs.readFile(base.with({ path: base.path + "/" + filename }));
					var scontent = new TextDecoder().decode(content);
					return scontent;
				} catch {
					return null;
				}
			}
		})
		var result = await runtime.BINDING.bind_static_method("[ext-core] MyClass:ExpandCurrentPipeline")(filename);
		await vscode.workspace.openTextDocument({ language: "yaml", content: result });
	});

	var statusbar = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right);

	statusbar.text = "Validate";

	statusbar.command = 'extension.expandAzurePipeline';

	statusbar.show();

	context.subscriptions.push(disposable);
}

// this method is called when your extension is deactivated
function deactivate() {}

// eslint-disable-next-line no-undef
export {
	activate,
	deactivate
}
