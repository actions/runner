/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import { ExtensionContext, Uri, workspace } from 'vscode';
import { LanguageClientOptions } from 'vscode-languageclient';

import { LanguageClient } from 'vscode-languageclient/browser';

let client: LanguageClient | undefined;
// this method is called when vs code is activated
export async function activate(context: ExtensionContext) {

	console.log('lsp-web-extension-sample activated!');

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

	client = await createWorkerLanguageClient(context, clientOptions);

	await  client.start();
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
