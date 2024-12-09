/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
import { BrowserMessageReader, BrowserMessageWriter } from 'vscode-languageserver/browser';
import { Semaphore } from 'vscode-jsonrpc/lib/common/semaphore'

import { RAL } from 'vscode-languageserver';

import { nodeRequire } from './config'

(async () => {
	var freadSemaphore = new Semaphore<Response>(1);
	var writeMsg = new Semaphore<void>(1);
	try {
		var isNode = typeof globalThis.postMessage !== 'function';
		if(isNode) {
			process.stdout.setEncoding('utf8');
			process.stdin.setEncoding('utf8');
		}
		var getBoot = new Promise<string>((resolve, reject) => {
			if(isNode) {
				resolve("file://" + __dirname + "/../src/build/AppBundle/_framework/blazor.boot.json")
			} else {
				onmessage = (msg) => {
					if(msg.data.error) {
						reject(msg.data.error);
					} else {
						resolve(msg.data.data);
					}
				}
			}
		});
		if(!isNode) {
			postMessage({ getpath: true });
		}

		var { basePaths, customImports } = require("./config.js")
		basePaths.basedir = await getBoot;
		var { dotnet } = require("./build/AppBundle/_framework/dotnet.js");
		customImports["dotnet.runtime.js"] = require("./build/AppBundle/_framework/dotnet.runtime.js");
		customImports["dotnet.native.js"] = require("./build/AppBundle/_framework/dotnet.native.js");

		var runtime = await dotnet.withOnConfigLoaded(async (config : any) => {
			config.resources.wasmSymbols = {}
			// 2024-10-28 Unknown breaking change in .net8.0 deployed pdb's that refuse to be served by marketplace cdn
			config.debugLevel = 0;
			config.resources.pdb = {};
		}).withConfigSrc(basePaths.basedir).withResourceLoader((type : string, name : string, defaultUri : string, integrity : string, behavior : string) => {
			if(type === "dotnetjs") {
				// Allow both nodejs and browser to use the same code
				customImports[defaultUri] = customImports[name];
				return defaultUri;
			}
			return freadSemaphore.lock(async () => {
				if(name.endsWith(".dat")) {
					name = name.substring(0, name.length - 3) + "icu";
				}
				
				if(isNode) {
					var nodeContent = nodeRequire("fs").readFileSync(__dirname + "/../src/build/AppBundle/_framework/" + name);
					return new Response(nodeContent, { status: 200 });
				}
				var getContent = new Promise<Uint8Array>((resolve, reject) => {
					onmessage = (msg) => {
						if(msg.data.error) {
							reject(msg.data.error);
						} else {
							resolve(msg.data.data);
						}
					}
				});
				postMessage({ path: "build/AppBundle/_framework/" + name  });
				var content = await getContent;
				return new Response(content, { status: 200 });

			});
		}).create();
		const messageWriter = isNode ? null : new BrowserMessageWriter(self);
		runtime.setModuleImports("extension.js", {
			sendOutputMessageAsync: async (data : string) => {
				if(isNode) {
					await writeMsg.lock(async () => {						await new Promise((accept) => {
							process.stdout.write(data, accept);
						})
					});
				} else {
					await writeMsg.lock(() => {
						console.log(data);
						messageWriter?.write(JSON.parse(data));
					});
				}
			}
		});

		runtime.runMainAndExit("Runner.Language.Server", isNode ? [ "--stdio" ] : []);
		var SendInputMessageAsync = runtime.BINDING.bind_static_method("[Runner.Language.Server] Interop:SendInputMessageAsync")

		var readSemaphore = new Semaphore(1);
		if(isNode) {
			process.stdin.on('readable', async () => {
				readSemaphore.lock(async () => {
					let chunk;
					while ((chunk = process.stdin.read()) !== null) {
						await SendInputMessageAsync(chunk);
					}
				});
			});
		} else {
			postMessage({ loaded: true })
			/* browser specific setup code */ 

			const messageReader = new BrowserMessageReader(self);

			messageReader.listen(async cbc => {
				readSemaphore.lock(async () => {
					await SendInputMessageAsync(JSON.stringify(cbc)); 
				})
			})
		}

	} catch(err)
 {
	console.log(err)
 }

})()

