/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import { commands, EventEmitter, ExtensionContext, Pseudoterminal, QuickPickItem, Uri, window, workspace } from 'vscode';
import { BaseLanguageClient, LanguageClientOptions } from 'vscode-languageclient';

import { LanguageClient } from 'vscode-languageclient/browser';
import { LanguageClient as LanguageClientNode } from 'vscode-languageclient/node';
import { spawn } from 'child_process';

let client: BaseLanguageClient | undefined;
// this method is called when vs code is activated
export async function activate(context: ExtensionContext) {
	commands.registerCommand("runner.server.runworkflow", async (workflow, events) => {
		console.log(`runner.server.runworkflow ${workflow}`)

		const writeEmitter = new EventEmitter<string>();
		const closeEmitter = new EventEmitter<void>();

		const pty: Pseudoterminal = {
			onDidWrite: writeEmitter.event,
			onDidClose: closeEmitter.event,
			open: () => {},
			close: () => {}
		};
		window.createTerminal({
			isTransient: false,
			pty: pty,
			name: "Runner.Client"
		})

		var sel : string = events.length === 1 ? events : await window.showQuickPick(events, { canPickMany: false })
		var args = ['--event', sel || 'push', '-W', Uri.parse(workflow).fsPath ];

		let startproc = spawn('Runner.Client', args, { windowsHide: true, stdio: 'pipe', shell: false, env: { ...process.env, FORCE_COLOR: "1" } });
		startproc.stdout.on('data', async (data) => {
			var sdata = data.asciiSlice();
			writeEmitter.fire(sdata.replace(/\r?\n/g, "\r\n"));
		});
		startproc.stderr.on('data', async (data) => {
			var sdata = data.asciiSlice();
			writeEmitter.fire(sdata.replace(/\r?\n/g, "\r\n"));
		});
		startproc.addListener('exit', code => {
			writeEmitter.fire(`\r\nProcess Exited with code ${code}.\r\n`);
			pty.handleInput = msg => closeEmitter.fire();
		});
	});
	commands.registerCommand("runner.server.runjob", async (workflow, job, events) => {
		if(typeof job === 'object') {
			var jobs : QuickPickItem[] = [];
			for(var j of job) {
				jobs.push({
					label: j.name,
					detail: j.jobIdLong
				});
			}
			job = (await window.showQuickPick(jobs, { canPickMany: false, title: "Select matrix job entry" }))?.detail;
			if(!job) {
				throw new Error("No job selected");
			}
		}
		console.log(`runner.server.runjob ${workflow}.${job}`)
		const writeEmitter = new EventEmitter<string>();
		const closeEmitter = new EventEmitter<void>();
		const pty: Pseudoterminal = {
			onDidWrite: writeEmitter.event,
			onDidClose: closeEmitter.event,
			open: () => {},
			close: () => {}
		};
		window.createTerminal({
			isTransient: false,
			pty: pty,
			name: "Runner.Client"
		})

		var sel : string = events.length === 1 ? events : await window.showQuickPick(events, { canPickMany: false })
		var args = [ '--event', sel || 'push', '-W', Uri.parse(workflow).fsPath, '-j', job ];
		
		let startproc = spawn('Runner.Client', args, { windowsHide: true, stdio: 'pipe', shell: false, env: { ...process.env, FORCE_COLOR: "1" } });
		startproc.stdout.on('data', async (data) => {
			var sdata = data.asciiSlice();
			writeEmitter.fire(sdata.replace(/\r?\n/g, "\r\n"));
		});
		startproc.stderr.on('data', async (data) => {
			var sdata = data.asciiSlice();
			writeEmitter.fire(sdata.replace(/\r?\n/g, "\r\n"));
		});
		startproc.addListener('exit', code => {
			writeEmitter.fire(`\r\nProcess Exited with code ${code}.\r\n`);
			pty.handleInput = msg => closeEmitter.fire();
		});
	});

	/*
	 * all except the code to create the language client in not browser specific
	 * and could be shared with a regular (Node) extension
	 */
	const documentSelector = [ { language: "yaml" }, { language: "azure-pipelines" } ];

	// Options to control the language client
	const clientOptions: LanguageClientOptions = {
		documentSelector,
		synchronize: {},
		initializationOptions: {}
	};

	if(globalThis.process) {
		console.log("Starting Language Server")
		client = new LanguageClientNode(
			'Runner.Server',
			'Runner.Server Language Server',
			{
				run: {
					transport: 0,
					module: context.asAbsolutePath("server/dist/nodeServerMain.js"),
					
				},
				debug: {
					transport: 0,
					module: context.asAbsolutePath("server/dist/nodeServerMain.js")
				}
			},
			{
				documentSelector: [ { language: "yaml" }, { language: "azure-pipelines" } ],
			}
		);
	} else {
		client = await createWorkerLanguageClient(context, clientOptions);
	}
	await client.start();
	console.log('lsp-web-extension-sample server is ready');
}

export async function deactivate(): Promise<void> {
	if (client !== undefined) {
		await client.stop();
	}
}

function createWorkerLanguageClient(context: ExtensionContext, clientOptions: LanguageClientOptions) : Promise<LanguageClient> {
	return new Promise<LanguageClient>((resolve, reject) => {
		// Create a worker. The worker main file implements the language server.
		const serverMain = Uri.joinPath(context.extensionUri, 'server/dist/browserServerMain.js');
		const worker = new Worker(serverMain.toString(true));

		worker.onmessage = async msg => {
			if(msg.data.getpath) {
				worker.postMessage({ status: 200, data: context.extensionUri.with({ path: context.extensionUri.path + "/server/src/build/AppBundle/_framework/blazor.boot.json" }).toString() });
			} else if(msg.data.loaded) {
				resolve(new LanguageClient('lsp-web-extension-sample', 'LSP Web Extension Sample', clientOptions, worker));
			} else if(msg.data.path) {
				try {
					var data = await workspace.fs.readFile(context.extensionUri.with({ path: context.extensionUri.path + "/server/src/" + msg.data.path }));
					worker.postMessage({ status: 200, data: data });
				} catch(err) {
					worker.postMessage({ status: 404, error: err });
				}
			}
		}

		// create the language server client to communicate with the server running in the worker
	});
}
