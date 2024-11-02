import { commands, window, ExtensionContext, Uri, ViewColumn, env, workspace } from 'vscode';
import { LanguageClient, TransportKind } from 'vscode-languageclient/node';
import { join } from 'path';
import { ChildProcessWithoutNullStreams, spawn } from 'child_process';
import { RSTreeDataProvider } from './treeItemProvider';

var startRunner : ChildProcessWithoutNullStreams | null = null;
var finishPromise : Promise<void> | null = null;

function activate(context : ExtensionContext) {

	(async() => {
		console.log("Aquire Dotnet!")

		// App requires .NET 8.0
		const commandRes = await commands.executeCommand('dotnet.acquire', { version: '8.0', requestingExtensionId: `${context.extension.packageJSON.publisher}.${context.extension.packageJSON.name}`, mode: "aspnetcore" }) as any;
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
					args: [ join(context.extensionPath, 'native', 'Runner.Language.Server.dll') ]
				},
				debug: {
					transport: TransportKind.stdio,
					command: dotnetPath,
					args: [ join(context.extensionPath, 'native', 'Runner.Language.Server.dll') ]
				}
			},
			{
				documentSelector: [ { language: "yaml" }, { language: "azure-pipelines" } ],
			}
		);

		var stopServer = new AbortController();

		var serverproc = spawn(dotnetPath, [ join(context.extensionPath, 'native', 'Runner.Server.dll'), '--urls', 'http://*:0' ], { killSignal: 'SIGINT', signal: stopServer.signal, windowsHide: true, stdio: 'pipe', shell: false });
		context.subscriptions.push({ dispose: () => stopServer.abort() })
		serverproc.addListener('exit', code => {
			console.log(code);
		});
		var address : string | null = null;
		serverproc.stdout.on('data', async (data) => {
			var sdata = data.asciiSlice();
			console.log(sdata)
			var i = sdata.indexOf("http://");
			if(i !== -1) {
				var end = sdata.indexOf('\n', i + 1);
				address = sdata.substring(i, end).replace("[::]", "localhost").replace("0.0.0.0", "localhost").trim();

				var panel = window.createWebviewPanel(
					"runner.server",
					"Runner Server",
					ViewColumn.Two,
					{
						enableScripts: true,
						// Without this we loose your webview position when the webview is in background
						retainContextWhenHidden: true
					}
				);

				window.registerTreeDataProvider("workflow-view", new RSTreeDataProvider(context, address));

				const fullWebServerUri = address && await env.asExternalUri(
					Uri.parse(address)
				);

				if(address) {
					var args = [ join(context.extensionPath, 'native', 'Runner.Client.dll'), 'startrunner', '--parallel', '4' ];
					args.push('--server', address)
				}

				finishPromise = new Promise<void>(onexit => {
					startRunner = spawn(dotnetPath, args, { windowsHide: true, stdio: 'pipe', shell: false, env: { ...process.env, RUNNER_CLIENT_EXIT_AFTER_ENTER: "1" } });
					startRunner.stdout.on('data', async (data) => {
						var sdata = data.asciiSlice();
						console.log(sdata)
					});
					startRunner.addListener('exit', code => {
						console.log(code);
						onexit();
					});
				})
				const cspSource = panel.webview.cspSource;
				// Get the content Uri
				const style = panel.webview.asWebviewUri(
					Uri.joinPath(context.extensionUri, 'style.css')
				);
				panel.webview.html = `<!DOCTYPE html>
				<html>
					<head>
						<meta
							http-equiv="Content-Security-Policy"
							content="default-src 'none'; frame-src ${fullWebServerUri} ${cspSource} https:; img-src ${cspSource} https:; script-src ${cspSource}; style-src ${cspSource};"
						/>
						<meta name="viewport" content="width=device-width, initial-scale=1.0">
						<link rel="stylesheet" href="${style}">
					</head>
					<body>
						<iframe src="${fullWebServerUri}?view=allworkflows"></iframe>
					</body>
				</html>`;
				context.subscriptions.push(panel);

				commands.registerCommand("runner.server.openjob", (runId, id) => {
					panel.webview.html = `<!DOCTYPE html>
					<html>
						<head>
							<meta
								http-equiv="Content-Security-Policy"
								content="default-src 'none'; frame-src ${fullWebServerUri} ${cspSource} https:; img-src ${cspSource} https:; script-src ${cspSource}; style-src ${cspSource};"
							/>
							<meta name="viewport" content="width=device-width, initial-scale=1.0">
							<link rel="stylesheet" href="${style}">
						</head>
						<body>
							<iframe src="${fullWebServerUri}?view=allworkflows#/0/${runId}/0/${id}"></iframe>
						</body>
					</html>`;
				});

				commands.registerCommand("runner.server.openworkflowrun", runId => {
					panel.webview.html = `<!DOCTYPE html>
					<html>
						<head>
							<meta
								http-equiv="Content-Security-Policy"
								content="default-src 'none'; frame-src ${fullWebServerUri} ${cspSource} https:; img-src ${cspSource} https:; script-src ${cspSource}; style-src ${cspSource};"
							/>
							<meta name="viewport" content="width=device-width, initial-scale=1.0">
							<link rel="stylesheet" href="${style}">
						</head>
						<body>
							<iframe src="${fullWebServerUri}?view=allworkflows#/0/${runId}"></iframe>
						</body>
					</html>`;
				});
			}
		});
		serverproc.stderr.on('data', async (data) => {
			var sdata = data.asciiSlice();
			console.log(sdata)
		});
		
		commands.registerCommand("runner.server.start-client", () => {
			var args = [ join(context.extensionPath, 'native', 'Runner.Client.dll'), '--interactive' ];
			if(address) {
				args.push('--server', address)
			}
			context.subscriptions.push(window.createTerminal("runner.client", dotnetPath, args))
		});

		commands.registerCommand("runner.server.runworkflow", (workflow) => {
			console.log(`runner.server.runjob {workflow}`)
			var args = [ join(context.extensionPath, 'native', 'Runner.Client.dll'), '-W', Uri.parse(workflow).fsPath ];
			if(address) {
				args.push('--server', address)
			}
			context.subscriptions.push(window.createTerminal("runner.client", dotnetPath, args))
		});
		commands.registerCommand("runner.server.runjob", (workflow, job) => {
			console.log(`runner.server.runjob {workflow}.{job}`)
			var args = [ join(context.extensionPath, 'native', 'Runner.Client.dll'), '-W', Uri.parse(workflow).fsPath, '-j', job ];
			if(address) {
				args.push('--server', address)
			}
			context.subscriptions.push(window.createTerminal("runner.client", dotnetPath, args))
		});

		context.subscriptions.push(client);
		client.start();
	})();
}

// this method is called when your extension is deactivated
async function deactivate() {
	if(finishPromise !== null && startRunner !== null) {
		startRunner.stdin.write("\n");
		await finishPromise;
	}
}
module.exports = {
	activate,
	deactivate
}
