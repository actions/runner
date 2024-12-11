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
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.Runner.Worker;

namespace Runner.Client
{
    public class ExternalToolHelper {
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

        private static string runnerOfficialUrl(string runner_URL, string runner12_VERSION, string os, string arch, string suffix) {
            return $"{runner_URL}/v{runner12_VERSION}/runner-v{runner12_VERSION}-{os}-{arch}.{suffix}";
        }

        private static async Task DownloadTool(Program.Parameters parameters, string link, string destDirectory, CancellationToken token, string tarextraopts = "", bool unwrap = false) {
            Console.WriteLine($"Downloading from {link} to {destDirectory}");
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

                using (var httpClientHandler = new HttpClientHandler())
                {
                    httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    httpClientHandler.AllowAutoRedirect = true;
                    httpClientHandler.MaxAutomaticRedirections = 10;
                    using (var httpClient = new HttpClient(httpClientHandler))
                    {
                        using (var response = await httpClient.GetAsync(link, HttpCompletionOption.ResponseHeadersRead, token))
                        {
                            response.EnsureSuccessStatusCode();
                            if(response.Headers.TryGetValues("Content-Disposition", out var values)) {
                                Console.WriteLine(string.Join("; ", values));
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
                                bool finished = false;
                                var progress = (new Func<Task>(async () => {
                                    try {
                                        while(!finished) {
                                            await Task.Delay(1000, token);
                                            Console.WriteLine(contentLength == null ? $"Downloaded {fs.Position / 1024 / 1024} MB" : $"Downloaded {fs.Position / 1024 / 1024}/{contentLength / 1024 / 1024} MB");
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
                    using (var processInvoker = new ProcessInvoker(new Program.TraceWriter(parameters)))
                    {
                        processInvoker.OutputDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                        {
                            if (!string.IsNullOrEmpty(args.Data))
                            {
                                Console.WriteLine(args.Data);
                            }
                        });

                        processInvoker.ErrorDataReceived += new EventHandler<ProcessDataReceivedEventArgs>((sender, args) =>
                        {
                            if (!string.IsNullOrEmpty(args.Data))
                            {
                                Console.WriteLine(args.Data);
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
                        Console.WriteLine($"Unwrap '{subDirectories[0].Name}' to '{destDirectory}'");
                        IOUtil.MoveDirectory(subDirectories[0].FullName, destDirectory, stagingDirectory2, CancellationToken.None);
                    }
                } else {
                    IOUtil.MoveDirectory(stagingDirectory, destDirectory, stagingDirectory2, CancellationToken.None); 
                }
            } finally {
                IOUtil.DeleteDirectory(tempDirectory, CancellationToken.None);
            }
        }
        internal static async Task<string> GetAgent(Program.Parameters parameters, string name, string version, CancellationToken token) {
            // Allow versions like v3.0.0 and not only 3.0.0
            version = version.Substring(version.IndexOf("v") + 1);
            var azagent = name == "azagent";
            var externalsPath = Path.Join(GharunUtil.GetLocalStorage());
            var os = GetHostOS();
            var arch = GetHostArch();
            string platform = os + "/" + arch;
            var exeExtension = os == "windows" ? ".exe" : "";
            var prefix = azagent ? "Agent" : "Runner";
            string file = Path.Combine(externalsPath, name, version, "bin", $"{prefix}.Listener{exeExtension}");
            if(!File.Exists(file)) {
                Console.WriteLine($"{file} executable not found locally");
                // Note use the vsts package, because container operations have node6 hardcoded as trampoline
                Func<string, string, string> AURL = azagent ? (arch, ext) => $"https://vstsagentpackage.azureedge.net/agent/{version}/vsts-agent-{arch}-{version}.{ext}" : (arch, ext) => $"https://github.com/actions/runner/releases/download/v{version}/actions-runner-{arch}-{version}.{ext}";
                var _tools = new Dictionary<string, Func<string, Task>> {
                    { "windows/386", dest => DownloadTool(parameters, AURL("win-x86", "zip"), dest, token, unwrap: false)},
                    { "windows/amd64", dest => DownloadTool(parameters, AURL("win-x64", "zip"), dest, token, unwrap: false)},
                    { "windows/arm64", dest => DownloadTool(parameters, AURL("win-arm64", "zip"), dest, token, unwrap: false)},
                    { "linux/amd64", dest => DownloadTool(parameters, AURL("linux-x64", "tar.gz"), dest, token, unwrap: false)},
                    { "linux/arm", dest => DownloadTool(parameters, AURL("linux-arm", "tar.gz"), dest, token, unwrap: false)},
                    { "linux/arm64", dest => DownloadTool(parameters, AURL("linux-arm64", "tar.gz"), dest, token, unwrap: false)},
                    { "osx/amd64", dest => DownloadTool(parameters, AURL("osx-x64", "tar.gz"), dest, token, unwrap: false)},
                    { "osx/arm64", dest => DownloadTool(parameters, AURL("osx-arm64", "tar.gz"), dest, token, unwrap: false)},
                };
                if(_tools.TryGetValue(platform, out Func<string, Task> download)) {
                    await download(Path.Combine(externalsPath, name, version));
                    if(!File.Exists(file)) {
                        throw new Exception("runner executable, not found after download");
                    }
                    Console.WriteLine("runner executable downloaded, continue workflow");
                } else {
                    throw new Exception("Failed to get runner executable, use --runner-path to use an external runner");
                }
            }
            return file;
        }
    }
}