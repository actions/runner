using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Net.Mime;
using System.Linq;

namespace GitHub.Runner.Worker
{
    public class ExternalToolHelper {
        public static IHostContext HostContext;

        public static string GetHostArch() {
            switch(System.Runtime.InteropServices.RuntimeInformation.OSArchitecture) {
                case System.Runtime.InteropServices.Architecture.X86:
                    return "386";
                case System.Runtime.InteropServices.Architecture.X64:
                    return "amd64";
                case System.Runtime.InteropServices.Architecture.Arm:
                    return "arm";
                case System.Runtime.InteropServices.Architecture.Arm64:
                    return "arm64";
                default:
                    throw new InvalidOperationException();
            }
        }

        public static string GetHostOS() {
            if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)) {
                return "linux";
            } else if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                return "windows";
            } else if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) {
                return "osx";
            }
            return null;
        }

        private static string NodeOfficialUrl(string NODE_URL, string NODE12_VERSION, string os, string arch, string suffix) {
            return $"{NODE_URL}/v{NODE12_VERSION}/node-v{NODE12_VERSION}-{os}-{arch}.{suffix}";
        }

        private static async Task DownloadTool(IExecutionContext executionContext, string link, string destDirectory, string tarextraopts = "", bool unwrap = false) {
            executionContext.Write("", $"Downloading from {link} to {destDirectory}");
            string tempDirectory = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "temp" + System.Guid.NewGuid().ToString());
            var stagingDirectory = Path.Combine(tempDirectory, "_staging");
            var stagingDirectory2 = Path.Combine(tempDirectory, "_staging2");
            try {
                Directory.CreateDirectory(stagingDirectory);
                Directory.CreateDirectory(stagingDirectory2);
                Directory.CreateDirectory(destDirectory);
                string archiveName = "";
                {
                    var lastSlash = link.LastIndexOf('/');
                    if(lastSlash != -1) {
                        archiveName = link.Substring(lastSlash + 1);
                    }
                }
                string archiveFile = "";

                using (var httpClientHandler = HostContext.CreateHttpClientHandler())
                {
                    httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    using (var httpClient = new HttpClient(httpClientHandler))
                    {
                        using (var response = await httpClient.GetAsync(link, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();
                            if(response.Headers.TryGetValues("Content-Disposition", out var values)) {
                                executionContext.Debug(string.Join("; ", values));
                                var content = new ContentDisposition(string.Join("; ", values));
                                var lastSlash = content.FileName?.LastIndexOfAny(new[]{'/', '\\'});
                                if(lastSlash.HasValue) {
                                    if(lastSlash != -1) {
                                        archiveName = content.FileName.Substring(lastSlash.Value  + 1);
                                    } else {
                                        archiveName = content.FileName;
                                    }
                                }
                            }
                            archiveFile = Path.Combine(tempDirectory, archiveName);
                            var contentLength = response.Content.Headers.ContentLength;
                            using (FileStream fs = new FileStream(archiveFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                            using (var result = await response.Content.ReadAsStreamAsync())
                            {
                                var token = executionContext.CancellationToken;
                                bool finished = false;
                                var progress = (new Func<Task>(async () => {
                                    try {
                                        while(!finished) {
                                            await Task.Delay(1000, token);
                                            executionContext.Write("", contentLength == null ? $"Downloaded {fs.Position / 1024 / 1024} MB" : $"Downloaded {fs.Position / 1024 / 1024}/{contentLength / 1024 / 1024} MB");
                                        }
                                    } catch {

                                    }
                                }))();
                                await result.CopyToAsync(fs, 4096, token);
                                finished = true;
                                await fs.FlushAsync(token);
                                await progress;
                            }
                        }
                    }
                }

                if(archiveName.ToLower().EndsWith(".zip")) {
                    ZipFile.ExtractToDirectory(archiveFile, stagingDirectory);
                } else if (archiveName.ToLower().EndsWith(".tar.gz")) {
                    string tar = WhichUtil.Which("tar", require: true);

                    // tar -xzf
                    using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
                    {
                        processInvoker.OutputDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                        {
                            if (!string.IsNullOrEmpty(args.Data))
                            {
                                executionContext.Write("", args.Data);
                            }
                        });

                        processInvoker.ErrorDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                        {
                            if (!string.IsNullOrEmpty(args.Data))
                            {
                                executionContext.Write("", args.Data);
                            }
                        });

                        int exitCode = await processInvoker.ExecuteAsync(stagingDirectory, tar, $"-xzf \"{archiveFile}\"{tarextraopts}", null, CancellationToken.None);
                        if (exitCode != 0)
                        {
                            throw new NotSupportedException($"Can't use 'tar -xzf' extract archive file: {archiveFile}. return code: {exitCode}.");
                        }
                    }
                } else {
                    File.Move(archiveFile, Path.Combine(destDirectory, archiveName));
                    return;
                }

                if(unwrap) {
                    var subDirectories = new DirectoryInfo(stagingDirectory).GetDirectories();
                    if (subDirectories.Length != 1)
                    {
                        throw new InvalidOperationException($"'{archiveFile}' contains '{subDirectories.Length}' directories");
                    }
                    else
                    {
                        executionContext.Debug($"Unwrap '{subDirectories[0].Name}' to '{destDirectory}'");
                        IOUtil.MoveDirectory(subDirectories[0].FullName, destDirectory, stagingDirectory2, executionContext.CancellationToken);
                    }
                } else {
                    IOUtil.MoveDirectory(stagingDirectory, destDirectory, stagingDirectory2, executionContext.CancellationToken); 
                }
            } finally {
                IOUtil.DeleteDirectory(tempDirectory, CancellationToken.None);
            }
        }

        public static Task<string> GetHostNodeTool(IExecutionContext executionContext, string name) {
            return GetNodeTool(executionContext, System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier.Contains("musl") || System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier.Contains("alpine") ? name + "_alpine" : name, GetHostOS(), GetHostArch());
        }
        public static async Task<string> GetNodeTool(IExecutionContext executionContext, string name, string os, string arch) {
            if(HostContext == null) {
                throw new Exception("Cannot download any tool without an valid HostContext");
            }
            string platform = os + "/" + arch;
            var externalsPath = HostContext.GetDirectory(WellKnownDirectory.Externals);
#if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
            externalsPath = Path.Combine(externalsPath, os, arch);
#else
            if(GetHostOS() != os || GetHostArch() != arch) {
                externalsPath = Path.Combine(externalsPath, os, arch);
            }
#endif
            var exeExtension = os == "windows" ? ".exe" : "";
            string file = Path.Combine(externalsPath, name, "bin", $"node{exeExtension}");
            if(!File.Exists(file)) {
                executionContext.Write("", $"{file} executable not found locally");
                Dictionary<string, Func<string, Task>> _tools = null;
                if(name == "node12") {
                    string nodeUrl = "https://nodejs.org/dist";
                    string nodeUnofficialUrl = "https://unofficial-builds.nodejs.org/download/release";
                    string nodeVersion = "12.13.1";
                    string tarextraopts = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) ? " --exclude \"*/lib/*\" \"*/bin/node*\" \"*/LICENSE\"" : "";
                    _tools = new Dictionary<string, Func<string, Task>> {
                        { "windows/386", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "win", "x86", "zip"), Path.Combine(dest, "bin"), unwrap: true)},
                        { "windows/amd64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "win", "x64", "zip"), Path.Combine(dest, "bin"), unwrap: true)},
                        { "windows/arm64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUnofficialUrl, nodeVersion, "win", "arm64", "zip"), Path.Combine(dest, "bin"), unwrap: true)},
                        { "linux/386", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUnofficialUrl, nodeVersion, "linux", "x86", "tar.gz"), dest, tarextraopts, true)},
                        { "linux/amd64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "linux", "x64", "tar.gz"), dest, tarextraopts, true)},
                        { "linux/arm", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "linux", "armv7l", "tar.gz"), dest, tarextraopts, true)},
                        { "linux/arm64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "linux", "arm64", "tar.gz"), dest, tarextraopts, true)},
                        { "osx/amd64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "darwin", "x64", "tar.gz"), dest, tarextraopts, true)},
                    };

                } else if(name == "node16") {
                    string nodeUrl = "https://nodejs.org/dist";
                    string nodeUnofficialUrl = "https://unofficial-builds.nodejs.org/download/release";
                    string nodeVersion = "16.20.1";
                    string tarextraopts = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) ? " --exclude \"*/lib/*\" \"*/bin/node*\" \"*/LICENSE\"" : "";
                    _tools = new Dictionary<string, Func<string, Task>> {
                        { "windows/386", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "win", "x86", "zip"), Path.Combine(dest, "bin"), unwrap: true)},
                        { "windows/amd64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "win", "x64", "zip"), Path.Combine(dest, "bin"), unwrap: true)},
                        { "windows/arm64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUnofficialUrl, "16.20.0", "win", "arm64", "zip"), Path.Combine(dest, "bin"), unwrap: true)},
                        { "linux/386", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUnofficialUrl, nodeVersion, "linux", "x86", "tar.gz"), dest, tarextraopts, true)},
                        { "linux/amd64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "linux", "x64", "tar.gz"), dest, tarextraopts, true)},
                        { "linux/arm", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "linux", "armv7l", "tar.gz"), dest, tarextraopts, true)},
                        { "linux/arm64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "linux", "arm64", "tar.gz"), dest, tarextraopts, true)},
                        { "osx/amd64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "darwin", "x64", "tar.gz"), dest, tarextraopts, true)},
                        { "osx/arm64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "darwin", "arm64", "tar.gz"), dest, tarextraopts, true)},
                    };

                } else if(name == "node20") {
                    string nodeUrl = "https://nodejs.org/dist";
                    string nodeVersion = "20.5.0";
                    string tarextraopts = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) ? " --exclude \"*/lib/*\" \"*/bin/node*\" \"*/LICENSE\"" : "";
                    _tools = new Dictionary<string, Func<string, Task>> {
                        { "windows/386", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "win", "x86", "zip"), Path.Combine(dest, "bin"), unwrap: true)},
                        { "windows/amd64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "win", "x64", "zip"), Path.Combine(dest, "bin"), unwrap: true)},
                        { "windows/arm64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, "16.6.2", "win", "arm64", "zip"), Path.Combine(dest, "bin"), unwrap: true)},
                        { "linux/amd64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "linux", "x64", "tar.gz"), dest, tarextraopts, true)},
                        { "linux/arm", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "linux", "armv7l", "tar.gz"), dest, tarextraopts, true)},
                        { "linux/arm64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "linux", "arm64", "tar.gz"), dest, tarextraopts, true)},
                        { "osx/amd64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "darwin", "x64", "tar.gz"), dest, tarextraopts, true)},
                        { "osx/arm64", dest => DownloadTool(executionContext, NodeOfficialUrl(nodeUrl, nodeVersion, "darwin", "arm64", "tar.gz"), dest, tarextraopts, true)},
                    };
                } else if(name == "node12_alpine") {
                    string nodeVersion = "12.13.1";
                    _tools = new Dictionary<string, Func<string, Task>> {
                        { "linux/amd64", dest => DownloadTool(executionContext, $"https://vstsagenttools.blob.core.windows.net/tools/nodejs/{nodeVersion}/alpine/x64/node-{nodeVersion}-alpine-x64.tar.gz", dest)},
                        { "linux/arm", dest => DownloadTool(executionContext, "https://github.com/ChristopherHX/node_alpine_arm/releases/download/v1.1918136754.1/node-12-alpine3.10-arm.tar.gz", dest)},
                        { "linux/arm64", dest => DownloadTool(executionContext, "https://github.com/ChristopherHX/node_alpine_arm/releases/download/v1.1918136754.1/node-12-alpine3.10-arm64.tar.gz", dest)},
                    };
                } else if(name == "node16_alpine") {
                    string nodeVersion = "16.20.1";
                    _tools = new Dictionary<string, Func<string, Task>> {
                        { "linux/amd64", dest => DownloadTool(executionContext, $"https://vstsagenttools.blob.core.windows.net/tools/nodejs/{nodeVersion}/alpine/x64/node-v{nodeVersion}-alpine-x64.tar.gz", dest)},
                        { "linux/arm", dest => DownloadTool(executionContext, "https://github.com/ChristopherHX/node_alpine_arm/releases/download/v1.1918136754.1/node-16-alpine3.11-arm.tar.gz", dest)},
                        { "linux/arm64", dest => DownloadTool(executionContext, "https://github.com/ChristopherHX/node_alpine_arm/releases/download/v1.1918136754.1/node-16-alpine3.11-arm64.tar.gz", dest)},
                    };
                } else if(name == "node20_alpine") {
                    string nodeVersion = "20.5.0";
                    _tools = new Dictionary<string, Func<string, Task>> {
                        { "linux/amd64", dest => DownloadTool(executionContext, $"https://vstsagenttools.blob.core.windows.net/tools/nodejs/{nodeVersion}/alpine/x64/node-v{nodeVersion}-alpine-x64.tar.gz", dest)},
                        { "linux/arm", dest => DownloadTool(executionContext, "https://github.com/ChristopherHX/node_alpine_arm/releases/download/v1.5822236100.1/node-20-alpine3.16-arm.tar.gz", dest)},
                        { "linux/arm64", dest => DownloadTool(executionContext, "https://github.com/ChristopherHX/node_alpine_arm/releases/download/v1.5822236100.1/node-20-alpine3.16-arm64.tar.gz", dest)},
                    };
                }
                if(_tools.TryGetValue(platform, out Func<string, Task> download)) {
                    await download(Path.Combine(externalsPath, name));
                    if(!File.Exists(file)) {
                        throw new Exception("node executable, not found after download");
                    }
                    executionContext.Write("", "node executable downloaded, continue workflow");
                } else {
                    throw new Exception("Failed to get node executable");
                }
            }
            return file;
        }
    }
}