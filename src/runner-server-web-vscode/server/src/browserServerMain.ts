/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
import { BrowserMessageReader, BrowserMessageWriter } from 'vscode-languageserver/browser';
import { Semaphore } from 'vscode-jsonrpc/lib/common/semaphore'

import { RAL } from 'vscode-languageserver';

console.log('running server lsp-web-extension-sample');

(async () => {
	var freadSemaphore = new Semaphore<Response>(1);
	var writeMsg = new Semaphore<void>(1);
	try {
		var getBoot = new Promise<string>((resolve, reject) => {
			onmessage = (msg) => {
				if(msg.data.error) {
					reject(msg.data.error);
				} else {
					resolve(msg.data.data);
				}
			}
		});
		postMessage({ getpath: true });
		

		var { basePaths, customImports } = require("./config.js")
		basePaths.basedir = await getBoot;
		var { dotnet } = require("./build/AppBundle/_framework/dotnet.js");
		customImports["dotnet.runtime.js"] = require("./build/AppBundle/_framework/dotnet.runtime.js");
		customImports["dotnet.native.js"] = require("./build/AppBundle/_framework/dotnet.native.js");

		var items = 0;
		var citem = 0;
		var runtime = await dotnet.withOnConfigLoaded(async (config : any) => {
			items = Object.keys(config.resources.assembly).length;
		}).withConfigSrc(basePaths.basedir).withResourceLoader((type : string, name : string, defaultUri : string, integrity : string, behavior : string) => {
			if(type === "dotnetjs") {
				// Allow both nodejs and browser to use the same code
				customImports[defaultUri] = customImports[name];
				return defaultUri;
			}
			return freadSemaphore.lock(async () => {
				if(type === "assembly") {
					console.log({ message: name, increment: citem++ / items });
				}
				if(name.endsWith(".dat")) {
					name = name.substring(0, name.length - 3) + "icu";
				}
				
				// return await fetch(self.location.toString() + "/../build/AppBundle/_framework/" + name)
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
		const messageWriter = new BrowserMessageWriter(self);
		runtime.setModuleImports("extension.js", {
			sendOutputMessageAsync: async (data : string) => {
				await writeMsg.lock(() => {
					console.log(data);
					messageWriter.write(JSON.parse(data));
				});
			}
		});
		runtime.runMainAndExit("Runner.Language.Server", []);
		var SendInputMessageAsync = runtime.BINDING.bind_static_method("[Runner.Language.Server] Interop:SendInputMessageAsync")

		postMessage({ loaded: true })
		/* browser specific setup code */ 

		const messageReader = new BrowserMessageReader(self);

		var lastContentLength = -1;
		var readSemaphore = new Semaphore(1);
		var buffer = RAL().messageBuffer.create('utf-8');

		messageReader.listen(async cbc => {
			readSemaphore.lock(async () => {
				// var data = await RAL().applicationJson.encoder.encode(cbc, { charset: "utf-8" })
				// console.log(data);

				// var enc = new TextEncoder();

				// var d = enc.encode("Content-Length: " + data.length + "\r\n\r\n");
				
				
				
				// var orglen = d.byteLength;
				// var nlen = (len: number) => Math.ceil(len / 4) * 4;
				// await SendInputMessageAsync(new Int32Array(d.buffer), orglen)
				// orglen = 
				// await SendInputMessageAsync(new Int32Array(data), data.byteLength)
				await SendInputMessageAsync(JSON.stringify(cbc)); 
				// buffer.append(data);
				// if(lastContentLength === -1) {
				// 	var headers = buffer.tryReadHeaders(true);
				// 	const contentLength = headers?.get('content-length');
				// 	const length = parseInt(contentLength!);
				// 	lastContentLength = length;
				// }
				// var result = buffer.tryReadBody(lastContentLength);
				// if(result) {
				// 	lastContentLength = -1;
				// 	messageWriter.write(await RAL().applicationJson.decoder.decode(result, { charset: "utf-8" }))
				// }
			})
		})

	} catch(err)
 {
	console.log(err)
 }

})()

