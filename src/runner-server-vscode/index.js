const vscode = require('vscode');
const { LanguageClient, TransportKind } = require('vscode-languageclient/node');
const path = require('path');
const cp = require('child_process');

/**
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {

	(async() => {
		console.log("Aquire Dotnet!")

		// App requires .NET 8.0
		const commandRes = await vscode.commands.executeCommand('dotnet.acquire', { version: '8.0', requestingExtensionId: `${context.extension.packageJSON.publisher}.${context.extension.packageJSON.name}`, mode: "aspnetcore" });
		const dotnetPath = commandRes.dotnetPath;
		console.log("Dotnet " + dotnetPath)

		if (!dotnetPath) {
			throw new Error('Could not resolve the dotnet path!');
		}

		console.log("Starting Language Server")
		var client = new LanguageClient(
			'Runner.Server',
			'Runner.Server Language Server',
			{
				run: {
					transport: TransportKind.stdio,
					command: dotnetPath,
					args: [ path.join(context.extensionPath, 'native', 'Runner.Language.Server.dll') ]
				},
				debug: {
					transport: TransportKind.stdio,
					command: dotnetPath,
					args: [ path.join(context.extensionPath, 'native', 'Runner.Language.Server.dll') ]
				}
			},
			{
				documentSelector: [ { language: "yaml" }, { language: "azure-pipelines" } ],
			}
		);

		var stopServer = new AbortController();

		var serverproc = cp.spawn(dotnetPath, [ path.join(context.extensionPath, 'native', 'Runner.Server.dll'), '--urls', 'http://*:0' ], { encoding: 'utf-8', killSignal: 'SIGINT', signal: stopServer.signal, windowsHide: true, stdio: 'pipe', shell: false });
		context.subscriptions.push({ dispose: () => stopServer.abort() })
		serverproc.addListener('exit', code => {
			console.log(code);
		});
		var address = null;
		serverproc.stdout.on('data', async (data) => {
			var sdata = data.asciiSlice();
			var i = sdata.indexOf("http://");
			if(i !== -1) {
				var end = sdata.indexOf('\n', i + 1);
				address = sdata.substring(i, end).replace("[::]", "localhost").replace("0.0.0.0", "localhost");

				var panel = vscode.window.createWebviewPanel(
					"runner.server",
					"Runner Server",
					vscode.ViewColumn.Two,
					{
						enableScripts: true,
						// Without this we loose your webview position when the webview is in background
						retainContextWhenHidden: true
					}
				);

				var args = [ path.join(context.extensionPath, 'native', 'Runner.Client.dll'), 'startrunner', '--parallel', '4' ];
				if(address) {
					args.push('--server', address)
				}
				vscode.window.createTerminal("runner.client", dotnetPath, args)
				panel.webview.html = `<html><body style="height: 100%;width: 100%;border: 0;padding: 0; margin: 0;overflow: hidden;" ><iframe style="height: 100vh;width: 100%;border: 0;padding: 0; margin: 0;overflow: hidden;" src="${address}"></iframe></body></html>`;
				context.subscriptions.push(panel);
			}
		});
		
		vscode.commands.registerCommand("runner.server.start-client", () => {
			var args = [ path.join(context.extensionPath, 'native', 'Runner.Client.dll'), '--interactive' ];
			if(address) {
				args.push('--server', address)
			}
			vscode.window.createTerminal("runner.client", dotnetPath, args)
		});

		vscode.commands.registerCommand("runner.server.runworkflow", (workflow) => {
			console.log(`runner.server.runjob {workflow}`)
			var args = [ path.join(context.extensionPath, 'native', 'Runner.Client.dll'), '-W', vscode.Uri.parse(workflow).fsPath ];
			if(address) {
				args.push('--server', address)
			}
			vscode.window.createTerminal("runner.client", dotnetPath, args)
		});
		vscode.commands.registerCommand("runner.server.runjob", (workflow, job) => {
			console.log(`runner.server.runjob {workflow}.{job}`)
			var args = [ path.join(context.extensionPath, 'native', 'Runner.Client.dll'), '-W', vscode.Uri.parse(workflow).fsPath, '-j', job ];
			if(address) {
				args.push('--server', address)
			}
			vscode.window.createTerminal("runner.client", dotnetPath, args)
		});

		context.subscriptions.push(client);
		client.start();
	})();
}

// this method is called when your extension is deactivated
function deactivate() {}

module.exports = {
	activate,
	deactivate
}