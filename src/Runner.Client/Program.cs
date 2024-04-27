using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using System.Linq;
using GitHub.Runner.Sdk;
using System.Reflection;
using System.Threading;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO.Pipes;
using System.Threading.Channels;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.ComponentModel;
using GitHub.Services.WebApi;
using System.CommandLine.Builder;
using System.CommandLine.Binding;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using GitHub.Runner.Common;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using Runner.Server.Azure.Devops;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.Services.Common;

namespace Runner.Client
{
    class Program
    {
        public class Job {
            public Guid JobId { get; set; }
            public long RequestId { get; set; }
            public Guid TimeLineId { get; set; }
            public Guid SessionId { get; set; }

            public string repo { get; set; }
            public string name { get; set; }
            public string workflowname { get; set; }
            public long runid { get; set; }
            public List<string> errors {get;set;}

            public bool ContinueOnError {get;set;}
            public bool Cancelled { get; internal set; }

            public bool Finished  {get;set;}
            public JobCompletedEvent JobCompletedEvent {get;set;}
        }

        private class TimeLineEvent {
            public Guid timelineId {get;set;}
            public List<TimelineRecord> timeline {get;set;}
        }

        private class WebConsoleEvent {
            public Guid timelineId {get;set;}
            public Guid recordId {get;set;}
            public TimelineRecordFeedLinesWrapper record {get;set;}
        }

        public class TraceWriter : GitHub.Runner.Sdk.ITraceWriter
        {
            private bool verbose;
            public TraceWriter(bool verbose = false) {
                this.verbose = verbose;
            }
            public void Info(string message)
            {
                if(verbose) {
                    Console.WriteLine(message);
                }
            }

            public void Verbose(string message)
            {
                if(verbose) {
                    Console.WriteLine(message);
                }
            }
        }

        private class ArtifactResponse {
            public string containerId {get;set;}
            public int size {get;set;}
            public string signedContent {get;set;}
            public string fileContainerResourceUrl {get;set;}
            public string type {get;set;}
            public string name {get;set;}
            public string url {get;set;}
        }

        private class DownloadInfo {
            public string path {get;set;}
            public string itemType {get;set;}
            public int fileLength {get;set;}
            public string contentLocation {get;set;}
        }

        private class Parameters {
            public string[] WorkflowFiles { get; set; }
            public string Server { get; set; }
            public string Payload { get; set; }
            public string Event { get; set; }
            public string[] Env { get; set; }
            public string[] EnvFile { get; set; }
            public string[] Secrets { get; set; }
            public string[] SecretFiles { get; set; }
            public string Job { get; set; }
            public string[] Matrix { get; set; }
            public bool List { get; set; }
            public string Workflows { get; set; }
            public string[] Platform { get; set; }
            public string Actor { get; set; }
            public bool Watch { get; set; }
            public bool Quiet { get; set; }
            public bool Privileged { get; set; }
            public string Userns { get; set; }
            public string ContainerPlatform { get; set; }
            public string DefaultBranch { get; set; }
            public string Directory { get; set; }
            public bool Verbose { get; set; }
            public int? Parallel { get; set; }
            public bool StartServer { get; set; }
            public bool StartRunner { get; set; }
            public bool NoCopyGitDir { get; set; }
            public bool KeepRunnerDirectory { get; set; }
            public bool NoSharedToolcache { get; set; }
            public bool KeepContainer { get; set; }
            public bool NoReuse { get; set; }
            public string GitServerUrl { get; set; }
            public string GitApiServerUrl { get; set; }
            public string GitGraphQlServerUrl { get; set; }
            public string GitTarballUrl { get; set; }
            public string GitZipballUrl { get; set; }
            public bool RemoteCheckout { get; set; }
            public string ArtifactOutputDir { get; set; }
            public string LogOutputDir { get; set; }
            public bool NoDefaultPayload  { get; set; }
            public string Token { get; set; }
            public string Repository { get; set; }
            public string Ref { get; set; }
            public string Sha { get; set; }
            public string[] Vars { get; set; }
            public string[] VarFiles { get; set; }
            public string[] EnvironmentSecretFiles { get; set; }
            public string[] EnvironmentVarFiles { get; set; }
            public string[] EnvironmentVars { get; set; }
            public string[] EnvironmentSecrets { get; set; }
            public string[] Inputs { get; set; }
            public string[] InputFiles { get; set; }
            public string RunnerDirectory { get; set; }
            public bool GitHubConnect { get; set; }
            public string GitHubConnectToken { get; set; }
            public string RunnerPath { get; set; }
            public string RunnerVersion { get; set; }
            public bool Interactive { get; internal set; }
            public string[] LocalRepositories { get; set; }
            public bool Trace { get; set; }

            public Parameters ShallowCopy()
            {
                return (Parameters) this.MemberwiseClone();
            }
        }

        class WorkflowEventArgs {
            public long runid {get;set;}
            public bool Success {get;set;}
        }

        class TimeLineEntry {
            public List<TimelineRecord> TimeLine {get;set;}
            public ConsoleColor Color {get;set;}
            public Guid RecordId {get;set;}
            public List<WebConsoleEvent> Pending {get;set;}
            public string JobName {get;set;}
            public string WorkflowName {get;set;}
        }

        struct RepoDownload {
            public string Url {get;set;}
            public bool Submodules {get;set;}
            public bool NestedSubmodules {get;set;}
            public string Repository {get;set;}
            public string Format {get;set;}
            public string Path {get;set;}
        };

        private static string ReadSecret() {
            StringBuilder input = new StringBuilder();
            ConsoleKeyInfo keyInfo;
            do {
                keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0){
                    input.Remove(input.Length - 1, 1);
                }
                else if (keyInfo.Key != ConsoleKey.Enter) {
                    input.Append(keyInfo.KeyChar);
                }
            } while(keyInfo.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return input.ToString();
        }

        private static void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        private static void PrintException(Exception ex)
        {
            if (ex != null)
            {
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine("Stacktrace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                PrintException(ex.InnerException);
            }
        }

        private static async Task<bool> IsIgnored(string dir, string path) {
            if(path.StartsWith(".git/") || path.StartsWith(".git\\")) {
                return true;
            }
            try {
                bool ret = false;
                EventHandler<ProcessDataReceivedEventArgs> handleoutput = (s, e) => {
                    var files = e.Data.Split('\0');
                    foreach(var file in files) {
                        if(file == "") break;
                        if(file == path) {
                            ret = true;
                        }
                    }
                };
                GitHub.Runner.Sdk.ProcessInvoker gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(false));
                gitinvoker.OutputDataReceived += handleoutput;
                var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var git = WhichUtil.Which("git", true);
                var channel = Channel.CreateBounded<string>(1);
                if(!channel.Writer.TryWrite(path + "\0")) {
                    return false;
                }
                channel.Writer.Complete();
                await gitinvoker.ExecuteAsync(dir, git, "check-ignore -z --stdin", new Dictionary<string, string>(), true, null, false, channel, CancellationToken.None);
                return ret;
            } catch {
                return false;
            }
        }

        private static async Task<int> CreateRunner(string binpath, Parameters parameters, List<Task> listener, Channel<bool> workerchannel, CancellationTokenSource source) {
            EventHandler<ProcessDataReceivedEventArgs> _out = (s, e) => {
                Console.WriteLine(e.Data);
            };
#if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
            var dotnet = WhichUtil.Which("dotnet", true);
            string ext = ".dll";
#else
            string ext = IOUtil.ExeExtension;
#endif
            var runner = Path.Join(binpath, $"Runner.Listener{ext}");
            var file = runner;
#if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
            file = dotnet;
#endif
            var agentname = Path.GetRandomFileName();
            string tmpdir = Path.Combine(Path.GetFullPath(parameters.RunnerDirectory), agentname);
            Directory.CreateDirectory(tmpdir);
            try {
                int attempt = 1;
                while(!source.IsCancellationRequested) {
                    try {
                        var inv = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
                        if(parameters.Verbose) {
                            inv.OutputDataReceived += _out;
                            inv.ErrorDataReceived += _out;
                        }
                        
                        var systemEnv = System.Environment.GetEnvironmentVariables();
                        var runnerEnv = new Dictionary<string, string>();
                        foreach(var e in systemEnv.Keys) {
                            runnerEnv[e as string] = systemEnv[e] as string;
                        }
                        runnerEnv["RUNNER_SERVER_CONFIG_ROOT"] = tmpdir;
                        runnerEnv["GHARUN_CHANGE_PROCESS_GROUP"] = "1";
                        if(!parameters.NoSharedToolcache && Environment.GetEnvironmentVariable("RUNNER_TOOL_CACHE") == null) {
                            runnerEnv["RUNNER_TOOL_CACHE"] = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "tool_cache");
                        }
                        if(parameters.ContainerPlatform != null) {
                            runnerEnv["RUNNER_CONTAINER_ARCH"] = parameters.ContainerPlatform;
                        }
                        if(parameters.Privileged) {
                            runnerEnv["RUNNER_CONTAINER_PRIVILEGED"] = "1";
                        }
                        if(parameters.Userns != null) {
                            runnerEnv["RUNNER_CONTAINER_USERNS"] = parameters.Userns;
                        }
                        if(parameters.KeepContainer) {
                            runnerEnv["RUNNER_CONTAINER_KEEP"] = "1";
                        }

                        var arguments = $"Configure --name {agentname} --unattended --url {parameters.Server}/runner/server --token {parameters.Token ?? "empty"} --labels container-host --work w";
#if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
                        arguments = $"\"{runner}\" {arguments}";
#endif
                        
                        var code = await inv.ExecuteAsync(binpath, file, arguments, runnerEnv, true, null, true, CancellationTokenSource.CreateLinkedTokenSource(source.Token, new CancellationTokenSource(60 * 1000).Token).Token);
                        int execAttempt = 1;                    
                        while(true) {
                            try {
                                var runnerlistener = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
                                if(parameters.Verbose) {
                                    runnerlistener.OutputDataReceived += _out;
                                    runnerlistener.ErrorDataReceived += _out;
                                }
                                
                                var runToken = CancellationTokenSource.CreateLinkedTokenSource(source.Token);
                                using(var timer = new Timer(obj => {
                                    runToken.Cancel();
                                }, null, 60 * 1000, -1)) {
                                    runnerlistener.OutputDataReceived += (s, e) => {
                                        if(e.Data.Contains("Listening for Jobs")) {
                                            timer.Change(-1, -1);
                                            workerchannel.Writer.WriteAsync(true);
                                        }
                                    };
                                    if(source.IsCancellationRequested) {
                                        return 1;
                                    }
                                    arguments = $"Run{(parameters.KeepContainer || parameters.NoReuse ? " --once" : "")}";
    #if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
                                    arguments = $"\"{runner}\" {arguments}";
    #endif
                                    await runnerlistener.ExecuteAsync(binpath, file, arguments, runnerEnv, true, null, true, runToken.Token);
                                    break;
                                }
                            } catch {
                                if(execAttempt++ <= 3) {
                                    await Task.Delay(500);
                                } else {
                                    Console.Error.WriteLine("Failed to start actions runner after 3 attempts");
                                    int delattempt = 1;
                                    while(true) {
                                        try {
                                            Directory.Delete(tmpdir, true);
                                            break;
                                        } catch {
                                            if(delattempt++ >= 3) {
                                                await Console.Error.WriteLineAsync($"Failed to cleanup {tmpdir} after 3 attempts");
                                                break;
                                            } else {
                                                await Task.Delay(500);
                                            }
                                        }
                                    }
                                    return 1;
                                }
                            }
                        }
                        break;
                    } catch {
                        if(attempt++ <= 3) {
                            await Task.Delay(500);
                        } else {
                            Console.Error.WriteLine("Failed to auto-configure actions runner after 3 attempts");
                            int delattempt = 1;
                            while(true) {
                                try {
                                    Directory.Delete(tmpdir, true);
                                    break;
                                } catch {
                                    if(delattempt++ >= 3) {
                                        await Console.Error.WriteLineAsync($"Failed to cleanup {tmpdir} after 3 attempts");
                                        break;
                                    } else {
                                        await Task.Delay(500);
                                    }
                                }
                            }
                            return 1;
                        }
                    }
                }
            } finally {
                Console.WriteLine("Stopped Runner");
                if(!parameters.KeepContainer && !parameters.KeepRunnerDirectory) {
                    int delattempt = 1;
                    while(true) {
                        try {
                            Directory.Delete(tmpdir, true);
                            break;
                        } catch {
                            if(!Directory.Exists(tmpdir)) {
                                break;
                            }
                            if(delattempt++ >= 3) {
                                await Console.Error.WriteLineAsync($"Failed to cleanup {tmpdir} after 3 attempts");
                                break;
                            } else {
                                await Task.Delay(500);
                            }
                        }
                    }
                };
            }
            if(parameters.KeepContainer || parameters.NoReuse) {
                if(!source.IsCancellationRequested) {
                    Console.WriteLine("Recreate Runner");
                    if(await CreateRunner(binpath, parameters, listener, workerchannel, source) != 0 && !source.IsCancellationRequested) {
                        Console.WriteLine("Failed to recreate Runner, exiting...");
                        source.Cancel();
                    }
                }
            }
            return 0;
        }

        private static async Task<int> CreateExternalRunner(string binpath, Parameters parameters, List<Task> listener, Channel<bool> workerchannel, CancellationTokenSource source) {
            EventHandler<ProcessDataReceivedEventArgs> _out = (s, e) => {
                Console.WriteLine(e.Data);
            };
            var azure = string.Equals(parameters.Event, "azpipelines", StringComparison.OrdinalIgnoreCase);
            var prefix = azure ? "Agent" : "Runner";
            string ext = IOUtil.ExeExtension;
            var root = Path.GetFullPath(parameters.RunnerPath);
            var listenerexe = Path.Join(root, "bin", $"{prefix}.Listener{ext}");
            var agentname = Path.GetRandomFileName();
            string tmpdir = Path.Combine(Path.GetFullPath(parameters.RunnerDirectory), agentname);
            Directory.CreateDirectory(Path.Join(tmpdir, "bin"));
            var bindir = Path.Join(root, "bin");
            foreach(var bfile in Directory.EnumerateFileSystemEntries(bindir)) {
                var fname = bfile.Substring(bindir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var destfile = Path.Join(tmpdir, "bin", fname);
                // copy .exe on windows alters the assembly path
                // copy .dll on linux alters the assembly path, not shure about the executable stub
                if(fname.StartsWith(prefix + ".") && (fname.EndsWith(".exe") || fname.EndsWith(".dll") || !fname.Substring(prefix.Length + 1).Contains("."))) {
                    File.Copy(bfile, destfile);
                } else {
                    if(Directory.Exists(bfile)) {
                        Directory.CreateSymbolicLink(destfile, bfile);
                    } else {
                        File.CreateSymbolicLink(destfile, bfile);
                    }
                }
            }
            Directory.CreateSymbolicLink(Path.Join(tmpdir, "externals"), Path.Join(root, "externals"));
            // for the azure pipelines agent on linux, doesn't exist on windows
            File.CreateSymbolicLink(Path.Join(tmpdir, "license.html"), Path.Join(root, "license.html"));

            var runner = Path.Join(tmpdir, "bin", $"{prefix}.Listener{ext}");
            var file = runner;
            try {
                int attempt = 1;
                while(!source.IsCancellationRequested) {
                    try {
                        var inv = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
                        if(parameters.Verbose) {
                            inv.OutputDataReceived += _out;
                            inv.ErrorDataReceived += _out;
                        }
                        var systemEnv = System.Environment.GetEnvironmentVariables();
                        var runnerEnv = new Dictionary<string, string>();
                        foreach(var e in systemEnv.Keys) {
                            runnerEnv[e as string] = systemEnv[e] as string;
                        }
                        if(azure) {
                            // Backward compat with old runner.server < 3.11.3
                            runnerEnv["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
                            // the 3.x.x azure agents don't use PredefinedCulturesOnly https://learn.microsoft.com/en-US/dotnet/core/runtime-config/globalization#predefined-cultures, however actions/runner added it
                            runnerEnv["DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY"] = "false";
                        } else {
                            // the official runner drops the port from the host
                            runnerEnv["RUNNER_SERVER_CONFIG_ROOT"] = tmpdir;
                        }
                        var toolCacheEnv = azure ? "AGENT_TOOLSDIRECTORY" : "RUNNER_TOOL_CACHE";
                        if(!parameters.NoSharedToolcache && !runnerEnv.TryGetValue(toolCacheEnv, out _)) {
                            runnerEnv[toolCacheEnv] = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "tool_cache", GitHub.Runner.Sdk.GharunUtil.GetHostOS());
                        }

                        // PAT Auth is only possible via https for azure devops agents, fallback to negotiate with dummy credentials otherwise it wouldn't work on linux without https
                        var arguments = azure ? parameters.Server.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ?  $"configure --auth pat --token {parameters.Token ?? "empty"} --unattended --agent {agentname} --url {parameters.Server} --work w" : $"configure --auth negotiate --userName token --password {parameters.Token ?? "empty"} --unattended --agent {agentname} --url {parameters.Server} --work w" : $"configure --name {agentname} --unattended --url {parameters.Server}/runner/server --token {parameters.Token ?? "empty"} --labels container-host --work w";
                        if(!azure) {
                            // Use embedded runner to configure external github agent, otherwise only port 80 or 443 are possible to use for configuration
                        #if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
                            arguments = $"\"{Path.Join(binpath, "Runner.Listener.dll")}\" {arguments}";
                            file = WhichUtil.Which("dotnet", true);
                        #else
                            file = Path.Join(binpath, $"Runner.Listener{ext}");
                        #endif
                        }
                        var code = await inv.ExecuteAsync(tmpdir, file, arguments, runnerEnv, true, null, true, CancellationTokenSource.CreateLinkedTokenSource(source.Token, new CancellationTokenSource(60 * 1000).Token).Token);
                        int execAttempt = 1;
                        var success = false;
                        // unset RUNNER_SERVER_CONFIG_ROOT to not appear in jobs created by external runners
                        runnerEnv.Remove("RUNNER_SERVER_CONFIG_ROOT");
                        while(true) {
                            file = runner;
                            try {
                                var runnerlistener = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
                                if(parameters.Verbose) {
                                    runnerlistener.OutputDataReceived += _out;
                                    runnerlistener.ErrorDataReceived += _out;
                                }
                                
                                var runToken = CancellationTokenSource.CreateLinkedTokenSource(source.Token);
                                using(var timer = new Timer(obj => {
                                    runToken.Cancel();
                                }, null, 60 * 1000, -1)) {
                                    runnerlistener.OutputDataReceived += (s, e) => {
                                        if(e.Data.Contains("Listen")) {
                                            timer.Change(-1, -1);
                                            success = true;
                                            workerchannel.Writer.WriteAsync(true);
                                        }
                                    };
                                    if(source.IsCancellationRequested) {
                                        return 1;
                                    }
                                    arguments = $"run{(parameters.KeepContainer || parameters.NoReuse ? " --once" : "")}";
                                    // Wrap listener to avoid that ctrl-c is sent to the runner
                                    if(!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                                        #if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
                                            arguments = $"\"{Path.Join(binpath, "Runner.Client.dll")}\" spawn \"{file}\" {arguments}";
                                            file = WhichUtil.Which("dotnet", true);
                                        #else
                                            arguments = $"spawn \"{file}\" {arguments}";
                                            file = Path.Join(binpath, $"Runner.Client{ext}");
                                        #endif
                                    }
                                    ((Func<Task>)(async () => {
                                        try {
                                            var client = new HttpClient();
                                            var isagentonline = new UriBuilder(parameters.Server);
                                            isagentonline.Path = $"_apis/v1/Message/isagentonline";
                                            var query = new QueryBuilder();
                                            query.Add("name", agentname);
                                            isagentonline.Query = query.ToString().TrimStart('?');
                                            for(int i = 0; i < 60; i++) {
                                                await Task.Delay(1000, source.Token);
                                                var resp = await client.GetAsync(isagentonline.ToString());
                                                if(resp.IsSuccessStatusCode) {
                                                    timer.Change(-1, -1);
                                                    success = true;
                                                    workerchannel.Writer.WriteAsync(true);
                                                    break;
                                                }
                                            }
                                        } catch {

                                        }
                                    }))();
                                    await runnerlistener.ExecuteAsync(tmpdir, file, arguments, runnerEnv, true, null, true, runToken.Token);
                                    break;
                                }
                            } catch {
                                if(success) {
                                    if(source.IsCancellationRequested) {
                                        return 1;
                                    }
                                    Console.Error.WriteLine("runner crashed after listening for jobs");
                                } else {
                                    if(execAttempt++ <= 3) {
                                        await Task.Delay(500);
                                    } else {
                                        Console.Error.WriteLine("Failed to start actions runner after 3 attempts");
                                        int delattempt = 1;
                                        while(true) {
                                            try {
                                                Directory.Delete(tmpdir, true);
                                                break;
                                            } catch {
                                                if(delattempt++ >= 3) {
                                                    await Console.Error.WriteLineAsync($"Failed to cleanup {tmpdir} after 3 attempts");
                                                    break;
                                                } else {
                                                    await Task.Delay(500);
                                                }
                                            }
                                        }
                                        return 1;
                                    }
                                }
                            }
                        }
                        break;
                    } catch {
                        if(attempt++ <= 3) {
                            await Task.Delay(500);
                        } else {
                            Console.Error.WriteLine("Failed to auto-configure actions runner after 3 attempts");
                            int delattempt = 1;
                            while(true) {
                                try {
                                    Directory.Delete(tmpdir, true);
                                    break;
                                } catch {
                                    if(delattempt++ >= 3) {
                                        await Console.Error.WriteLineAsync($"Failed to cleanup {tmpdir} after 3 attempts");
                                        break;
                                    } else {
                                        await Task.Delay(500);
                                    }
                                }
                            }
                            return 1;
                        }
                    }
                }
            } finally {
                Console.WriteLine("Stopped Runner");
                if(!parameters.KeepContainer && !parameters.KeepRunnerDirectory) {
                    int delattempt = 1;
                    while(true) {
                        try {
                            Directory.Delete(tmpdir, true);
                            break;
                        } catch {
                            if(!Directory.Exists(tmpdir)) {
                                break;
                            }
                            if(delattempt++ >= 3) {
                                await Console.Error.WriteLineAsync($"Failed to cleanup {tmpdir} after 3 attempts");
                                break;
                            } else {
                                await Task.Delay(500);
                            }
                        }
                    }
                };
            }
            if(parameters.KeepContainer || parameters.NoReuse) {
                if(!source.IsCancellationRequested) {
                    Console.WriteLine("Recreate Runner");
                    if(await CreateExternalRunner(binpath, parameters, listener, workerchannel, source) != 0 && !source.IsCancellationRequested) {
                        Console.WriteLine("Failed to recreate Runner, exiting...");
                        source.Cancel();
                    }
                }
            }
            return 0;
        }

        private static string GetJobUrl(string baseUrl, Guid id) {
            var b2 = new UriBuilder(baseUrl);
            var query = new QueryBuilder();
            query.Add("jobid", id.ToString());
            b2.Query = query.ToString().TrimStart('?');
            b2.Path = "_apis/v1/Message";
            return b2.ToString();
        }

        private static string GetCancelWorkflowUrl(string baseUrl, long runid) {
            var b2 = new UriBuilder(baseUrl);
            b2.Path = $"_apis/v1/Message/cancelWorkflow/{runid}";
            return b2.ToString();
        }

        private static string GetForceCancelWorkflowUrl(string baseUrl, long runid) {
            var b2 = new UriBuilder(baseUrl);
            b2.Path = $"_apis/v1/Message/forceCancelWorkflow/{runid}";
            return b2.ToString();
        }

        private static bool forceColor = false;
        private static bool isGithubActions = false;

        private static string escapeWorkflowCommands(string source) {
            if(isGithubActions) {
                // Add a Zero Width Space to escape the workflow command
                // Without this github actions interprets these commands
                return source.Replace("##[", "#​#​[​");
            }
            // Otherwise revert that again, e.g running Runner.Client tests locally
            return source.Replace("#​#​[​", "##[");
        }

        private static void WriteLogLine(int color, string tag, string message) {
            tag = escapeWorkflowCommands(tag);
            message = escapeWorkflowCommands(message);
            if(forceColor) {
                Console.WriteLine($"\x1b[{(int)color + 30}m[{tag}]\x1b[0m {message}");
            } else {
                Console.ResetColor();
                Console.ForegroundColor = (ConsoleColor)color;
                Console.Write("[" + tag + "] ");
                Console.ResetColor();
                Console.WriteLine(message);
            }
        }

        private static void WriteLogLine(int color, string message) {
            message = escapeWorkflowCommands(message);
            if(forceColor) {
                Console.WriteLine($"\x1b[{(int)color + 30}m|\x1b[0m {message}");
            } else {
                Console.ResetColor();
                Console.ForegroundColor = (ConsoleColor)color;
                Console.Write("| ");
                Console.ResetColor();
                Console.WriteLine(message);
            }
        }

        private delegate Task LoadEntries(ref TimeLineEntry entry, List<Job> jobs, Guid id);

        private class MyCustomBinder : BinderBase<Parameters>
        {
            private Func<BindingContext, Parameters> bind;
            public MyCustomBinder(Func<BindingContext, Parameters> bind) {
                this.bind = bind;
            }
            protected override Parameters GetBoundValue(BindingContext bindingContext) => bind.Invoke(bindingContext);
        }

        private static bool? GetEnvironmentBoolean(string name, bool? defMismatch = null) {
            var val = System.Environment.GetEnvironmentVariable(name);
            if(val == null) {
                return null;
            }
            if(string.Equals(val, "True", StringComparison.OrdinalIgnoreCase) || string.Equals(val, "Y", StringComparison.OrdinalIgnoreCase) || string.Equals(val, "1", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            if(string.Equals(val, "False", StringComparison.OrdinalIgnoreCase) || string.Equals(val, "N", StringComparison.OrdinalIgnoreCase) || string.Equals(val, "0", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
            return defMismatch;
        }

        private static bool MatchRepository(string l, string r)
        {
            var lnameAndRef = l.Split("=", 2).FirstOrDefault()?.Split("@", 2);
            var rnameAndRef = r.Split("@", 2);
            // Fallback to exact match, this should allow bad repository names to match for local azure tasks
            return lnameAndRef?.Length == 2 && rnameAndRef?.Length == 2 ? string.Equals(lnameAndRef[0], rnameAndRef[0], StringComparison.OrdinalIgnoreCase) && lnameAndRef[1] == rnameAndRef[1] : l == r;
        }

        private class AzurePipelinesExpander {
    
            public class MyFileProvider : IFileProvider
            {
                public MyFileProvider(Parameters handle) {
                    this.handle = handle;
                }
                private Parameters handle;
                public Task<string> ReadFile(string repositoryAndRef, string path)
                {
                    var reporoot = repositoryAndRef == null ? Path.GetFullPath(handle.Directory ?? ".") : (from r in handle.LocalRepositories where MatchRepository(r, repositoryAndRef) select Path.GetFullPath(r.Substring($"{repositoryAndRef}=".Length))).LastOrDefault();
                    if(string.IsNullOrEmpty(reporoot)) {
                        return null;
                    }
                    return File.ReadAllTextAsync(Path.Join(reporoot, path), Encoding.UTF8);
                }
            }

            public class TraceWriter : GitHub.DistributedTask.ObjectTemplating.ITraceWriter {
                public void Error(string format, params object[] args)
                {
                    if(args?.Length == 1 && args[0] is Exception ex) {
                        Console.Error.WriteLine(string.Format("{0} {1}", format, ex.Message));
                        return;
                    }
                    try {
                        Console.Error.WriteLine(args?.Length > 0 ? string.Format(format, args) : format);
                    } catch {
                        Console.Error.WriteLine(format);
                    }
                }

                public void Info(string format, params object[] args)
                {
                    try {
                        Console.WriteLine(args?.Length > 0 ? string.Format(format, args) : format);
                    } catch {
                        Console.WriteLine(format);
                    }
                }

                public void Verbose(string format, params object[] args)
                {
                    try {
                        Console.WriteLine(args?.Length > 0 ? string.Format(format, args) : format);
                    } catch {
                        Console.WriteLine(format);
                    }
                }
            }

            private class VariablesProvider : IVariablesProvider {
                public Dictionary<string, Dictionary<string, string>> Variables { get; set; }
                public IDictionary<string, string> GetVariablesForEnvironment(string name = null) {
                    return Variables?.TryGetValue(name ?? "", out var dict) == true ? dict : null;
                }
            }

            public static async Task<string> ExpandCurrentPipeline(Parameters handle, string currentFileName) {
                var (secs, vars) = await ReadSecretsAndVariables(handle);
                var context = new Runner.Server.Azure.Devops.Context {
                    FileProvider = new MyFileProvider(handle),
                    TraceWriter = handle.Quiet ? new EmptyTraceWriter() : new TraceWriter(),
                    Flags = GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives | GitHub.DistributedTask.Expressions2.ExpressionFlags.AllowAnyForInsert,
                    VariablesProvider = new VariablesProvider { Variables = vars }
                };
                Dictionary<string, TemplateToken> cparameters = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                if(handle.InputFiles?.Length > 0) {
                    foreach(var file in handle.InputFiles) {
                        Util.ReadEnvFile(file, (varname, varval) => cparameters[varname] = AzurePipelinesUtils.ConvertStringToTemplateToken(varval));
                    }
                }
                if(handle.Inputs != null) {
                    foreach(var input in handle.Inputs) {
                        var opt = input;
                        var subopt = opt.Split('=', 2);
                        string varname = subopt[0];
                        string varval = subopt.Length == 2 ? subopt[1] : null;
                        cparameters[varname] = AzurePipelinesUtils.ConvertStringToTemplateToken(varval);
                    }
                }
                var template = await AzureDevops.ReadTemplate(context, currentFileName, cparameters);
                var pipeline = await new Runner.Server.Azure.Devops.Pipeline().Parse(context.ChildContext(template, currentFileName), template);
                return pipeline.ToYaml();
            }
        }
        
        static int Main(string[] args)
        {
            if(System.OperatingSystem.IsWindowsVersionAtLeast(10)) {
                WindowsUtils.EnableVT();
            }
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            // dotnet doesn't enable color on github actions, but we are still able to emit ansi color codes
            isGithubActions = GetEnvironmentBoolean("GITHUB_ACTIONS") == true;
            forceColor = GetEnvironmentBoolean("NO_COLOR", true) != true && (GetEnvironmentBoolean("FORCE_COLOR", true) == true || isGithubActions);
            
            //$content = Get-Content <path to raw https://github.com/github/docs/blob/main/content/actions/reference/events-that-trigger-workflows.md>
            //$content -match "#### ``.*``"
            //$events -replace "#### ``(.*)``",'"$1",'
            var validevents = new [] {
                "schedule",
                "workflow_dispatch",
                "repository_dispatch",
                "check_run",
                "check_suite",
                "create",
                "delete",
                "deployment",
                "deployment_status",
                "fork",
                "gollum",
                "issue_comment",
                "issues",
                "label",
                "milestone",
                "page_build",
                "project",
                "project_card",
                "project_column",
                "public",
                "pull_request",
                "pull_request_review",
                "pull_request_review_comment",
                "pull_request_target",
                "push",
                "registry_package",
                "release",
                "status",
                "watch",
                "workflow_run",
                // Azure Pipelines Experiments
                "azexpand",
                "azpipelines",
            };

            var secretOpt = new Option<string[]>(
                new[] { "-s", "--secret" },
                description: "Secret for your workflow, overrides keys from your secrets file. E.g. `-s Name` or `-s Name=Value`. You will be asked for a value if you add `--secret name`, but no environment variable with name `name` exists") {
                    AllowMultipleArgumentsPerToken = false,
                    Arity = ArgumentArity.ZeroOrMore
                };
            var envOpt = new Option<string[]>(
                new[] { "--env" },
                description: "Environment variable for your workflow, overrides keys from your env file. E.g. `--env Name` or `--env Name=Value`. You will be asked for a value if you add `--env name`, but no environment variable with name `name` exists") {
                    AllowMultipleArgumentsPerToken = false,
                    Arity = ArgumentArity.ZeroOrMore
                };
            var envFile = new Option<string[]>(
                "--env-file",
                description: "Environment variables for your workflow");
            var matrixOpt = new Option<string[]>(
                new[] { "-m", "--matrix" },
                description: "Matrix filter e.g. `-m Key:value`, use together with `--job <job>`. Use multiple times to filter more specifically. If you want to force a value to be a string you need to quote it, e.g. `\"-m Key:\\\"1\\\"\"` or `\"-m Key:\"\"1\"\"\"` (requires shell escaping)") {
                    AllowMultipleArgumentsPerToken = false,
                    Arity = ArgumentArity.ZeroOrMore
                };
            
            var workflowOption = new Option<string[]>(
                "--workflow",
                description: "Workflow(s) to run. Use multiple times to execute more workflows parallel") {
                    AllowMultipleArgumentsPerToken = false,
                    Arity = ArgumentArity.ZeroOrMore
                };

            var platformOption = new Option<string[]>(
                new[] { "-P", "--platform" },
                description: "Platform mapping to run the workflow in a docker container (similar behavior as using the container property of a workflow job, the container property of a job will take precedence over your specified docker image) or host. E.g. `-P ubuntu-latest=ubuntu:latest` (Docker Linux Container), `-P ubuntu-latest=-self-hosted` (Local Machine), `-P windows-latest=-self-hosted` (Local Machine), `-P windows-latest=mcr.microsoft.com/windows/servercore:ltsc2022` (Docker Windows container, windows only), `-P macos-latest=-self-hosted` (Local Machine) or with multiple labels `-P self-hosted,testmachine,anotherlabel=-self-hosted` (Local Machine)") {
                    AllowMultipleArgumentsPerToken = false,
                    Arity = ArgumentArity.ZeroOrMore
                };
            var serverOpt = new Option<string>(
                "--server",
                description: "Runner.Server address, e.g. `http://localhost:5000` or `https://localhost:5001`");
            var payloadOpt = new Option<string>(
                new[] { "-e", "--payload", "--eventpath" },
                "Webhook payload to send to the Runner");
            var noDefaultPayloadOpt = new Option<bool>(
                "--no-default-payload",
                "Do not provide or merge autogenerated payload content, will pass your unmodified payload to the runner");
            var eventOpt = new Option<string>(
                "--event",
                getDefaultValue: () => "push",
                description: "Which event to send to a worker, ignored if you use subcommands which overriding the event");
            var varOpt = new Option<string[]>(
                "--var",
                description: "Variables for your workflow, varname=valvalue");
            var varFileOpt = new Option<string[]>(
                "--var-file",
                description: "Variables for your workflow, filename.yml");
            var secretFileOpt = new Option<string[]>(
                "--secret-file",
                description: "Secrets for your workflow");
            var environmentSecretOpt = new Option<string[]>(
                "--environment-secret",
                description: "Environment Secrets with name name for your workflow, name=secretname=secretvalue");
            var environmentSecretFileOpt = new Option<string[]>(
                "--environment-secret-file",
                description: "Environment Secrets with name name for your workflow, name=filename.yml");
            var environmentVarOpt = new Option<string[]>(
                "--environment-var",
                description: "Environment Variables with name name for your workflow, name=varname=valvalue");
            var environmentVarFileOpt = new Option<string[]>(
                "--environment-var-file",
                description: "Environment Variables with name name for your workflow, name=filename.yml");
            var jobOpt = new Option<string>(
                new[] {"-j", "--job"},
                description: "Job to run. If multiple jobs have the same name in multiple workflows, all matching jobs will run. Use together with `--workflow <workflow>` to run exact one job");
            var listOpt = new Option<bool>(
                new[] { "-l", "--list"},
                description: "List jobs for the selected event (defaults to push)");
            var workflowsOpt = new Option<string>(
                new[] { "-W", "--workflows"},
                description: "Workflow file or directory which contains workflows, only used if no `--workflow <workflow>` option is set");
            var actorOpt = new Option<string>(
                new[] {"-a" , "--actor"},
                "The login of the user who initiated the workflow run, ignored if already in your event payload");
            var watchOpt = new Option<bool>(
                new[] {"-w", "--watch"},
                "Run automatically on every file change");
            var interactiveOpt = new Option<bool>(
                new[] {"--interactive"},
                "Run interactively");
            var traceOpt = new Option<bool>(
                new[] {"--trace"},
                "Client Trace of console log events, to debug missing live logs");
            var quietOpt = new Option<bool>(
                new[] {"-q", "--quiet"},
                "Display no progress in the cli");
            var privilegedOpt = new Option<bool>(
                "--privileged",
                "Run the docker container under privileged mode, only applies to container jobs using this Runner fork");
            var usernsOpt = new Option<string>(
                "--userns",
                "Change the docker container linux user namespace, only applies to container jobs using this Runner fork");
            var containerPlatformOpt = new Option<string>(
                new [] { "--container-architecture", "--container-platform" },
                "Change the docker container platform, if docker supports it. Only applies to container jobs using this Runner fork");
            var keepContainerOpt = new Option<bool>(
                "--keep-container",
                "Do not clean up docker container after job, this leaks resources");
            var defaultbranchOpt = new Option<string>(
                "--defaultbranch",
                description: "The default branch of your workflow run, ignored if already in your event payload");
            var DirectoryOpt = new Option<string>(
                new[] {"-C", "--directory"},
                "Change the directory of your local repository, provided file or directory names are still resolved relative to your current working directory");
            var verboseOpt = new Option<bool>(
                new[] {"-v", "--verbose"},
                "Print more details like server / runner logs to stdout");
            var parallelOpt = new Option<int?>(
                "--parallel",
                description: "Run n parallel runners");
            var noCopyGitDirOpt = new Option<bool>(
                "--no-copy-git-dir",
                description: "Avoid copying the .git folder into the runner if it exists");
            var keepRunnerDirectoryOpt = new Option<bool>(
                "--keep-runner-directory",
                description: "Skip deleting temporary runner directories");
            var runnerDirectoryOpt = new Option<string>(
                "--runner-directory",
                getDefaultValue: () => Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "a"),
                description: "Custom runner directory, can be used to avoid filepath length constraints");
            var noSharedToolCacheOpt = new Option<bool>(
                "--no-shared-toolcache",
                description: "Do not share toolcache between runners, a shared toolcache may cause workflow failures");
            var noReuseOpt = new Option<bool>(
                "--no-reuse",
                "Do not reuse a configured self-hosted runner, creates a new instance after a job completes");
            var gitServerUrlOpt = new Option<string>(
                "--git-server-url",
                getDefaultValue: () => "https://github.com",
                description: "Url to github or gitea instance");
            var gitApiServerUrlOpt = new Option<string>(
                "--git-api-server-url",
                description: "Url to github or gitea api. ( e.g https://api.github.com )");
            var gitGraphQlOpt = new Option<string>(
                "--git-graph-ql-server-url",
                description: "Url to github graphql api. ( e.g https://api.github.com/graphql )");
            var gitTarballUrlOpt = new Option<string>(
                "--git-tarball-url",
                description: "Url to github or gitea tarball api url, defaults to `<git-server-url>/{0}/archive/{1}.tar.gz`. `{0}` is replaced by `<owner>/<repo>`, `{1}` is replaced by branch, tag or sha");
            var gitZipballUrlOpt = new Option<string>(
                "--git-zipball-url",
                description: "Url to github or gitea zipball api url, defaults to `<git-server-url>/{0}/archive/{1}.zip`. `{0}` is replaced by `<owner>/<repo>`, `{1}` is replaced by branch, tag or sha");
             var githubConnectOpt = new Option<bool>(
                "--github-connect",
                description: "Allow all actions from https://github.com with an GHES instance");
            var githubConnectTokenOpt = new Option<string>(
                "--github-connect-token",
                description: "Provide an optional Personal Access Token for https://github.com");
            var runnerPathOpt = new Option<string>(
                "--runner-path",
                description: "Use this specfic runner instead of spinning up the embedded runner");
            var runnerVersionOpt = new Option<string>(
                "--runner-version",
                description: "Use this runner version instead of spinning up the embedded runner");
            var remoteCheckoutOpt = new Option<bool>(
                "--remote-checkout",
                description: "Do not inject localcheckout into your workflows, always use the original actions/checkout");
            var artifactOutputDirOpt = new Option<string>(
                "--artifact-output-dir",
                description: "Output folder for all artifacts produced by this runs");
            var logOutputDirOpt = new Option<string>(
                "--log-output-dir",
                description: "Output folder for all logs produced by this runs");
            var repositoryOpt = new Option<string>(
                "--repository",
                description: "Custom github.repository");
            var shaOpt = new Option<string>(
                "--sha",
                description: "Custom github.sha");
            var refOpt = new Option<string>(
                "--ref",
                description: "Custom github.ref");
            var workflowInputsOpt = new Option<string[]>(
                new[] {"-i", "--input"},
                description: "Inputs to add to the payload. E.g. `--input name=value`");
            var workflowInputFilesOpt = new Option<string[]>(
                new[] {"--input-file"},
                description: "Inputs for your Workflow");
            var localrepositoriesOpt = new Option<string[]>(
                "--local-repository",
                description: "Redirect dependent repositories to the local filesystem. E.g `--local-repository org/name@ref=/path/to/repository`");
            var rootCommand = new RootCommand
            {
                workflowOption,
                serverOpt,
                payloadOpt,
                noDefaultPayloadOpt,
                eventOpt,
                envOpt,
                envFile,
                varOpt,
                varFileOpt,
                secretOpt,
                secretFileOpt,
                environmentSecretOpt,
                environmentSecretFileOpt,
                environmentVarOpt,
                environmentVarFileOpt,
                jobOpt,
                matrixOpt,
                listOpt,
                workflowsOpt,
                platformOption,
                actorOpt,
                watchOpt,
                interactiveOpt,
                traceOpt,
                quietOpt,
                privilegedOpt,
                usernsOpt,
                containerPlatformOpt,
                keepContainerOpt,
                DirectoryOpt,
                verboseOpt,
                parallelOpt,
                noCopyGitDirOpt,
                keepRunnerDirectoryOpt,
                runnerDirectoryOpt,
                noSharedToolCacheOpt,
                noReuseOpt,
                gitServerUrlOpt,
                gitApiServerUrlOpt,
                gitGraphQlOpt,
                gitTarballUrlOpt,
                gitZipballUrlOpt,
                githubConnectOpt,
                githubConnectTokenOpt,
                runnerPathOpt,
                runnerVersionOpt,
                remoteCheckoutOpt,
                artifactOutputDirOpt,
                logOutputDirOpt,
                repositoryOpt,
                shaOpt,
                refOpt,
                localrepositoriesOpt,
            };

            rootCommand.Description = "Run your workflows locally.";

            var spawn = new Command("spawn", "executes a process and changes it's process group") { IsHidden = true };
            var spawnargs = new Argument<string[]>();
            spawn.AddArgument(spawnargs);
            Func<string[], Task<int>> spawnHandler = async (string[] args) => {
                if(!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                    try {
                        if (Mono.Unix.Native.Syscall.setpgid(0, 0) != 0) {
                            Console.WriteLine($"Failed to change Process Group");
                        }
                    } catch {
                        Console.WriteLine($"Failed to change Process Group exception");
                    }
                }
                var proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = args[0];
                for(int i = 1; i < args.Length; i++) {
                    proc.StartInfo.ArgumentList.Add(args[i]);
                }
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();
                var stdout = proc.StandardOutput.BaseStream.CopyToAsync(System.Console.OpenStandardOutput());
                var stderr = proc.StandardError.BaseStream.CopyToAsync(System.Console.OpenStandardError());
                var stdin = System.Console.OpenStandardInput().CopyToAsync(proc.StandardInput.BaseStream);
                await Task.WhenAll(stdout, stderr);
                proc.WaitForExit();
                return proc.ExitCode;
            };
            spawn.SetHandler(spawnHandler, spawnargs);
            rootCommand.Add(spawn);

            // Note that the parameters of the handler method are matched according to the names of the options
            Func<Parameters, Task<int>> handler = async (parameters) =>
            {
                var expandAzurePipeline = string.Equals(parameters.Event, "azexpand", StringComparison.OrdinalIgnoreCase);
                if(parameters.Parallel == null && !parameters.StartServer && !parameters.List && !expandAzurePipeline) {
                    parameters.Parallel = 1;
                }
                if(parameters.Actor == null) {
                    parameters.Actor = "runnerclient";
                }
                List<string> errors = new List<string>();
                if(parameters.Matrix?.Length > 0) {
                    if(parameters.Job == null) {
                        errors.Add("--matrix is only supported together with --job");
                    }
                    foreach(var p in parameters.Matrix) {
                        if(!p.Contains(":")) {
                            errors.Add($"Invalid Argument for `--matrix`: `{p}`, missing `:`");
                        }
                    }
                }
                if(parameters.Platform?.Length > 0) {
                    foreach(var p in parameters.Platform) {
                        if(!p.Contains("=")) {
                            errors.Add($"Invalid Argument for `--platform`: `{p}`, missing `=`");
                        }
                    }
                }
                if(parameters.Inputs?.Length > 0) {
                    foreach(var input in parameters.Inputs) {
                        var kv = input.Split('=', 2);
                        if(kv?.Length != 2) {
                            errors.Add($"Invalid Argument for `--input`: `{input}`, missing `=`");
                        }
                    }
                }
                if(errors.Count > 0) {
                    foreach(var error in errors) {
                        Console.Error.WriteLine(error);
                    }
                    return 1;
                }
                ConcurrentQueue<string> added = new ConcurrentQueue<string>();
                ConcurrentQueue<string> changed = new ConcurrentQueue<string>();
                ConcurrentQueue<string> removed = new ConcurrentQueue<string>();
                CancellationTokenSource source = new CancellationTokenSource();
                Action cancelWorkflow = null;
                CancellationToken token = source.Token;
                bool canceled = false;
                Console.CancelKeyPress += (s, e) => {
                    if(cancelWorkflow != null) {
                        e.Cancel = true;
                        cancelWorkflow.Invoke();
                        return;
                    }
                    e.Cancel = !canceled;
                    Console.WriteLine($"CTRL+C received {(e.Cancel ? "Shutting down... CTRL+C again to Terminate" : "Terminating")}");
                    canceled = true;
                    source.Cancel();
                };
                if(parameters.Parallel > 0) {
                    var azure = string.Equals(parameters.Event, "azpipelines", StringComparison.OrdinalIgnoreCase);
                    if(string.IsNullOrEmpty(parameters.RunnerVersion) && string.IsNullOrEmpty(parameters.RunnerPath) && azure) {
                        parameters.RunnerVersion = "3.234.0";
                    }
                    if(!string.IsNullOrEmpty(parameters.RunnerVersion)) {
                        parameters.RunnerPath = Directory.GetParent(await ExternalToolHelper.GetAgent(azure ? "azagent" : "runner", parameters.RunnerVersion, source.Token)).Parent.FullName;
                    }
                }
                List<Task> listener = new List<Task>();
                try {
                    if(!expandAzurePipeline && (parameters.Server == null || parameters.StartServer || parameters.StartRunner)) {
                        var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        EventHandler<ProcessDataReceivedEventArgs> _out = (s, e) => {
                            Console.WriteLine(e.Data);
                        };
                        if(parameters.StartRunner) {
                            if(parameters.Server == null) {
                                parameters.Server = "http://localhost:5000";
                            }
                        } else {
                            Console.WriteLine("Starting Server...");
                            if(parameters.Server == null) {
                                try {
                                    // From https://stackoverflow.com/a/27376368
                                    using (System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
                                    {
                                        socket.Connect("8.8.8.8", 65530);
                                        IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                                        var builder = new UriBuilder();
                                        builder.Host = endPoint.Address.ToString();
                                        builder.Scheme = "http";
                                        builder.Port = 0;
                                        parameters.Server = builder.Uri.ToString().Trim('/');
                                    }
                                } catch {
                                }
                                if(parameters.Server == null) {
                                    try {
                                        foreach(var ip in Dns.GetHostAddresses(Dns.GetHostName())) {
                                            if(!IPAddress.IsLoopback(ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                                                var builder = new UriBuilder();
                                                builder.Host = ip.ToString();
                                                builder.Scheme = "http";
                                                builder.Port = 0;
                                                parameters.Server = builder.Uri.ToString().Trim('/');
                                                break;
                                            }
                                        }
                                    } catch {
                                    }
                                }
                                if(parameters.Server == null) {
                                    Console.WriteLine("Failed to autodetect non loopback ip, docker actions will fail to connect");
                                    parameters.Server = "http://localhost:0";
                                }
                            }
                            GitHub.Runner.Sdk.ProcessInvoker invoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
                            if(parameters.Verbose) {
                                invoker.OutputDataReceived += _out;
                                invoker.ErrorDataReceived += _out;
                            }
#if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
                            var dotnet = WhichUtil.Which("dotnet", true);
                            string ext = ".dll";
#else
                            string ext = IOUtil.ExeExtension;
#endif
                            var server = Path.Join(binpath, $"Runner.Server{ext}");
                            var file = server;
                            var arguments = "";
#if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
                            file = dotnet;
                            arguments = $"\"{server}\"";
#endif
                            string serverconfigfileName = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
                            JObject serverconfig = new JObject();
                            var connectionopts = new JObject();
                            connectionopts["sqlite"] = "";
                            serverconfig["ConnectionStrings"] = connectionopts;
                            
                            serverconfig["Kestrel"] = JObject.FromObject(new { Endpoints = new { Http = new { Url = parameters.Server } } });
                            var rsconfig = new { 
                                GitServerUrl = parameters.GitServerUrl,
                                GitApiServerUrl = parameters.GitApiServerUrl,
                                GitGraphQlServerUrl = parameters.GitGraphQlServerUrl,
                                ActionDownloadUrls = new [] {
                                    new {
                                        TarballUrl = parameters.GitTarballUrl ?? parameters.GitServerUrl + "/{0}/archive/{1}.tar.gz",
                                        ZipballUrl = parameters.GitZipballUrl ?? parameters.GitServerUrl + "/{0}/archive/{1}.zip",
                                        GitApiServerUrl = "",
                                        GITHUB_TOKEN = "",
                                        ReturnWithoutResolvingSha = false,
                                    }
                                }.ToList(),
                                DefaultWebUIView = "allworkflows"
                            };
                            if(parameters.GitHubConnect) {
                                rsconfig.ActionDownloadUrls.Add(new {
                                    TarballUrl = "https://github.com/{0}/archive/{1}.tar.gz",
                                    ZipballUrl = "https://github.com/{0}/archive/{1}.zip",
                                    GitApiServerUrl = "https://api.github.com",
                                    GITHUB_TOKEN = parameters.GitHubConnectToken,
                                    ReturnWithoutResolvingSha = string.IsNullOrEmpty(parameters.GitHubConnectToken),
                                });
                            }
                            serverconfig["Runner.Server"] = JObject.FromObject(rsconfig);
                            await File.WriteAllTextAsync(serverconfigfileName, serverconfig.ToString());
                            using (AnonymousPipeServerStream pipeServer =
                                new AnonymousPipeServerStream(PipeDirection.In,
                                HandleInheritability.Inheritable))
                            {
                                var runToken = new CancellationTokenSource();
                                using(var timer = new Timer(obj => {
                                    runToken.Cancel();
                                }, null, 60 * 1000, -1)) {
                                    var servertask = Task.Run(async () => {
                                        using (AnonymousPipeServerStream shutdownPipe =
                                            new AnonymousPipeServerStream(PipeDirection.Out,
                                            HandleInheritability.Inheritable)) {
                                            Task.Run(async () => {
                                                try {
                                                    await Task.Delay(-1, token);
                                                } catch {

                                                }
                                                using (StreamWriter sr = new StreamWriter(shutdownPipe))
                                                {
                                                    sr.WriteLine("shutdown");
                                                }
                                            });
                                            var systemEnv = System.Environment.GetEnvironmentVariables();
                                            var serverEnv = new Dictionary<string, string>();
                                            foreach(var e in systemEnv.Keys) {
                                                serverEnv[e as string] = systemEnv[e] as string;
                                            }
                                            foreach(var kv in new Dictionary<string, string>{ {"RUNNER_SERVER_APP_JSON_SETTINGS_FILE", serverconfigfileName }, { "RUNNER_CLIENT_PIPE", pipeServer.GetClientHandleAsString() }, { "RUNNER_CLIENT_PIPE_IN", shutdownPipe.GetClientHandleAsString() }, { "GHARUN_CHANGE_PROCESS_GROUP", "1" }}) {
                                                serverEnv[kv.Key] = kv.Value;
                                            }
                                            if(parameters.Verbose) {
                                                Console.WriteLine($"serverEnv: {(serverEnv.Select(kv => $"{kv.Key}={kv.Value}").Aggregate((l, nl) => string.IsNullOrEmpty(l) ? nl : $"{l}\n{nl}"))}");
                                            }
                                            var x = await invoker.ExecuteAsync(binpath, file, arguments, serverEnv, false, null, true, runToken.Token);
                                            Console.WriteLine("Stopped Server");
                                            File.Delete(serverconfigfileName);
                                        }
                                    });
                                    listener.Add(servertask);
                                    var serveriptask = Task.Run(() => {
                                        using (StreamReader rd = new StreamReader(pipeServer))
                                        {
                                            var line = rd.ReadLine();
                                            timer.Change(-1, -1);
                                            parameters.Server = line;
                                            Console.WriteLine($"The server is listening on {line}");
                                        }
                                    });
                                    if(await Task.WhenAny(serveriptask, servertask) == servertask) {
                                        if(!canceled) {
                                            Console.Error.WriteLine("Failed to start server, rerun with `-v` to find out what is wrong");
                                        }
                                        return 1;
                                    }
                                }
                            }
                        }

                        if(parameters.Parallel > 0) {
                            Console.WriteLine($"Starting {parameters.Parallel} Runner{(parameters.Parallel != 1 ? "s" : "")}...");
                            var workerchannel = Channel.CreateBounded<bool>(1);
                            for(int i = 0; i < parameters.Parallel; i++) {
                                if(string.IsNullOrEmpty(parameters.RunnerPath)) {
                                    listener.Add(CreateRunner(binpath, parameters, listener, workerchannel, source));
                                } else {
                                    listener.Add(CreateExternalRunner(binpath, parameters, listener, workerchannel, source));
                                }
                            }
                            var task = workerchannel.Reader.ReadAsync().AsTask();
                            if(await Task.WhenAny(listener.Append(task)) != task) {
                                if(!canceled) {
                                    Console.Error.WriteLine("Fatal: Failed to start Runner or Server crashed");
                                }
                                return 1;
                            }
                            Console.WriteLine($"First runner is listening for jobs");
                        }

                        if(parameters.StartServer || parameters.StartRunner) {
                            Console.WriteLine($"Press {(Debugger.IsAttached ? "Enter or CTRL+C" : "CTRL+C")} to stop the {(parameters.StartServer ? "Server" : (parameters.Parallel != 1 ? "Runners" : "Runner"))}");

                            try {
                                if(Debugger.IsAttached) {
                                    await Task.WhenAny(Task.Run(() => {
                                        Console.In.ReadLine();
                                    }), Task.Delay(-1, token));
                                } else {
                                    await Task.Delay(-1, token);
                                }
                            } catch {

                            }
                            return 0;
                        }
                    }
                    bool first = true;
                    bool skipAskToSaveArtifact = false;
                    var orgParameters = parameters.ShallowCopy();
                    while(!source.IsCancellationRequested && (parameters.Interactive || parameters.Watch || first)) {
                        cancelWorkflow = null;
                        var ret = await Task.Run<int>(async () => {
                            Action<QueryBuilder> queryParam = null;
                            if(parameters.Interactive) {
                                string command = "";
                                parameters.List = false;
                                parameters.Job = null;
                                parameters.Matrix = null;
                                Console.Write("gharun> ");
                                try {
                                    command = await Console.In.ReadLineAsync().WithCancellation(source.Token);
                                } catch {
                                    command = "";
                                }
                                var interactiveCommand = new Command("gharun>");
                                var runCommand = new Command("run", "Create a new run, this is the default action of this tool");
                                runCommand.Add(jobOpt);
                                runCommand.Add(matrixOpt);
                                runCommand.SetHandler((job, matrix) => {
                                    parameters.Job = job;
                                    parameters.Matrix = matrix;
                                    queryParam = _ => {};
                                }, jobOpt, matrixOpt);

                                interactiveCommand.Add(runCommand);
                                var rerunCommand = new Command("rerun", "Rerun an completed workflow instead of starting a new one, you can keep completed jobs and artifacts during reruns");
                                var failedOpt = new Option<bool>(
                                    new [] { "-f", "--failed" }, "rerun all failed jobs"
                                );
                                var resetArtifactsOpt = new Option<bool>(
                                    new [] { "-r", "--resetArtifacts" }, "rerun without previous artifacts"
                                );
                                var refreshOpt = new Option<bool>(
                                    new [] { "--refresh" }, "refresh workflow runs new jobs added to the workflow"
                                );
                                var runIdOpt = new Argument<int>(
                                    "runid", "Which workflow to rerun"
                                );
                                var jobIdOpt = new Option<string>(new [] { "-j", "--jobid" }, "Which job to rerun");
                                rerunCommand.Add(runIdOpt);
                                rerunCommand.Add(jobIdOpt);
                                rerunCommand.Add(failedOpt);
                                rerunCommand.Add(resetArtifactsOpt);
                                rerunCommand.Add(refreshOpt);
                                rerunCommand.SetHandler((runId, jobId, failed, resetArtifacts, refresh) => {
                                    queryParam = qb => {
                                        qb.Add("runid", runId.ToString());
                                        if(!string.IsNullOrEmpty(jobId)) {
                                            qb.Add("jobId", jobId);
                                        }
                                        qb.Add("failed", failed.ToString());
                                        qb.Add("resetArtifacts", resetArtifacts.ToString());
                                        qb.Add("refresh", refresh.ToString());
                                    };
                                }, runIdOpt, jobIdOpt, failedOpt, resetArtifactsOpt, refreshOpt);

                                interactiveCommand.Add(rerunCommand);

                                var listCommand = new Command("list", "Lists jobs of the selected workflows");
                                listCommand.Add(jobOpt);
                                listCommand.Add(matrixOpt);
                                listCommand.SetHandler((job, matrix) => {
                                    parameters.Job = job;
                                    parameters.Matrix = matrix;
                                    parameters.List = true;
                                    queryParam = _ => {};
                                }, jobOpt, matrixOpt);
                                interactiveCommand.Add(listCommand);

                                var deleteCommand = new Command("delete", "Delete Artifacts or Cache");
                                var deleteCacheCommand = new Command("cache", "Delete Cache, can be used to recreate the Cache and free storage");
                                deleteCacheCommand.SetHandler(async () => {
                                    var client = new HttpClient();
                                    var deleteUri = new UriBuilder(parameters.Server);
                                    deleteUri.Path = "_apis/artifactcachemanagement/cache";
                                    await client.DeleteAsync(deleteUri.ToString());
                                });
                                deleteCommand.Add(deleteCacheCommand);
                                var deleteArtifactsCommand = new Command("artifacts", "Delete all Artifacts and free storage");
                                deleteArtifactsCommand.SetHandler(async () => {
                                    var client = new HttpClient();
                                    var deleteUri = new UriBuilder(parameters.Server);
                                    deleteUri.Path = "_apis/artifactcachemanagement/artifacts";
                                    await client.DeleteAsync(deleteUri.ToString());
                                });
                                deleteCommand.Add(deleteArtifactsCommand);
                                interactiveCommand.Add(deleteCommand);

                                var updateCommand = new Command("update", "Update provided env and secrets");
                                updateCommand.Add(workflowOption);
                                updateCommand.Add(DirectoryOpt);
                                updateCommand.Add(payloadOpt);
                                updateCommand.Add(envOpt);
                                updateCommand.Add(secretOpt);
                                var eventOpt = new Option<string>(
                                    "--event",
                                    description: "Change the event to trigger the workflow");
                                updateCommand.Add(eventOpt);
                                var resetOpt = new Option<bool>(
                                    "--reset",
                                    description: "Discard cached cli options");
                                updateCommand.Add(resetOpt);
                                var cleanOpt = new Option<bool>(
                                    "--clean",
                                    description: "Discard cached cli and initial provided options, initial provided options can be restored by --reset");
                                updateCommand.Add(cleanOpt);
                                var envFileOpt = new Option<string[]>(
                                    "--env-file",
                                    description: "Environment variables for your workflow");
                                updateCommand.Add(envFileOpt);
                                updateCommand.Add(varOpt);
                                updateCommand.Add(varFileOpt);
                                var secretFileOpt = new Option<string[]>(
                                    "--secret-file",
                                    description: "Secrets for your workflow");
                                updateCommand.Add(secretFileOpt);
                                updateCommand.Add(environmentSecretOpt);
                                updateCommand.Add(environmentSecretFileOpt);
                                updateCommand.Add(environmentVarOpt);
                                updateCommand.Add(environmentVarFileOpt);
                                updateCommand.Add(workflowInputsOpt);
                                updateCommand.Add(workflowInputFilesOpt);
                                updateCommand.SetHandler(_ => {}, new MyCustomBinder(bindingContext => {
                                    var pResult = bindingContext.ParseResult;
                                    if(pResult.GetValueForOption(cleanOpt)) {
                                        parameters = orgParameters.ShallowCopy();
                                        parameters.Payload = null;
                                        parameters.Directory = null;
                                        parameters.WorkflowFiles = null;
                                        parameters.Env = null;
                                        parameters.Secrets = null;
                                        parameters.EnvFile = null;
                                        parameters.Vars = null;
                                        parameters.VarFiles = null;
                                        parameters.SecretFiles = null;
                                        parameters.EnvironmentSecrets = null;
                                        parameters.EnvironmentSecretFiles = null;
                                        parameters.EnvironmentVarFiles = null;
                                        parameters.Inputs = null;
                                        parameters.InputFiles = null;
                                        parameters.EnvironmentVars = null;
                                    } else if(pResult.GetValueForOption(resetOpt)) {
                                        parameters = orgParameters.ShallowCopy();
                                    }
                                    parameters.Event = pResult.GetValueForOption(eventOpt) ?? parameters.Event;
                                    parameters.Payload = pResult.GetValueForOption(payloadOpt) ?? parameters.Payload;
                                    parameters.Directory = pResult.GetValueForOption(DirectoryOpt) ?? parameters.Directory;
                                    parameters.WorkflowFiles = parameters.WorkflowFiles.SafeConcatArray(pResult.GetValueForOption(workflowOption));
                                    parameters.Env = parameters.Env.SafeConcatArray(pResult.GetValueForOption(envOpt));
                                    parameters.Secrets = parameters.Secrets.SafeConcatArray(pResult.GetValueForOption(secretOpt));
                                    parameters.EnvFile = parameters.EnvFile.SafeConcatArray(pResult.GetValueForOption(envFile));
                                    parameters.Vars = parameters.Vars.SafeConcatArray(pResult.GetValueForOption(varOpt));
                                    parameters.VarFiles = parameters.VarFiles.SafeConcatArray(pResult.GetValueForOption(varFileOpt));
                                    parameters.SecretFiles = parameters.SecretFiles.SafeConcatArray(pResult.GetValueForOption(secretFileOpt));
                                    parameters.EnvironmentSecrets = parameters.EnvironmentSecrets.SafeConcatArray(pResult.GetValueForOption(environmentSecretOpt));
                                    parameters.EnvironmentSecretFiles = parameters.EnvironmentSecretFiles.SafeConcatArray(pResult.GetValueForOption(environmentSecretFileOpt));
                                    parameters.EnvironmentVarFiles = parameters.EnvironmentVarFiles.SafeConcatArray(pResult.GetValueForOption(environmentVarFileOpt));
                                    parameters.Inputs = parameters.Inputs.SafeConcatArray(pResult.GetValueForOption(workflowInputsOpt));
                                    parameters.InputFiles = parameters.Inputs.SafeConcatArray(pResult.GetValueForOption(workflowInputFilesOpt));
                                    parameters.EnvironmentVars = parameters.EnvironmentVars.SafeConcatArray(pResult.GetValueForOption(environmentVarOpt));
                                    return parameters;
                                }));
                                interactiveCommand.Add(updateCommand);

                                var exitCommand = new Command("exit", "Stop this program, the server and the runner");
                                exitCommand.AddAlias("quit");
                                exitCommand.AddAlias("Quit");
                                exitCommand.SetHandler(() => {
                                    source.Cancel();
                                });
                                interactiveCommand.Add(exitCommand);
                                
                                if(source.IsCancellationRequested) {
                                    return 0;
                                }
                                await interactiveCommand.InvokeAsync(command);
                                if(queryParam == null) {
                                    return 0;
                                }
                            }
                            List<string> addedFiles = new List<string>();
                            List<string> changedFiles = new List<string>();
                            List<string> removedFiles = new List<string>();
                            if(!first && !parameters.Interactive) {
                                using(FileSystemWatcher watcher = new FileSystemWatcher(parameters.Directory ?? ".") {IncludeSubdirectories = true}) {
                                    watcher.Created += (s, f) => {
                                        var path = Path.GetRelativePath(parameters.Directory ?? ".", f.FullPath);
                                        Console.WriteLine($"Added {path}");
                                        added.Enqueue(path);
                                    };
                                    watcher.Deleted += (s, f) => {
                                        var path = Path.GetRelativePath(parameters.Directory ?? ".", f.FullPath);
                                        Console.WriteLine($"Removed {path}");
                                        removed.Enqueue(path);
                                    };
                                    watcher.Changed += (s, f) => {
                                        var path = Path.GetRelativePath(parameters.Directory ?? ".", f.FullPath);
                                        Console.WriteLine($"Changed {path}");
                                        changed.Enqueue(path);
                                    };
                                    watcher.Renamed += (s, f) => {
                                        var path = Path.GetRelativePath(parameters.Directory ?? ".", f.FullPath);
                                        var oldpath = Path.GetRelativePath(parameters.Directory ?? ".", f.OldFullPath);
                                        Console.WriteLine($"Renamed {oldpath} to {path}");
                                        if(oldpath != f.OldFullPath) {
                                            removed.Enqueue(oldpath);
                                        }
                                        if(path != f.FullPath) {
                                            added.Enqueue(path);
                                        }
                                    };
                                    watcher.Error += OnError;
                                    watcher.EnableRaisingEvents = true;

                                    Console.WriteLine("Watching for changes");

                                    string addedFile = null;
                                    string changedFile = null;
                                    string removedFile = null;
                                    try {
                                        do {
                                            await Task.Delay(2000, source.Token);
                                        } while(!(added.TryDequeue(out addedFile) && !await IsIgnored(parameters.Directory ?? ".", addedFile)) && !(changed.TryDequeue(out changedFile) && !await IsIgnored(parameters.Directory ?? ".", changedFile)) && !(removed.TryDequeue(out removedFile) && !await IsIgnored(parameters.Directory ?? ".", removedFile)));
                                    } catch(TaskCanceledException) {

                                    }
                                    while(addedFile != null || added.TryDequeue(out addedFile)) {
                                        if(!await IsIgnored(parameters.Directory ?? ".", addedFile)) {
                                            addedFiles.Add(addedFile.Replace('\\', '/'));
                                        }
                                        addedFile = null;
                                    }
                                    while(changedFile != null || changed.TryDequeue(out changedFile)) {
                                        if(!await IsIgnored(parameters.Directory ?? ".", changedFile)) {
                                            changedFiles.Add(changedFile.Replace('\\', '/'));
                                        }
                                        changedFile = null;
                                    }
                                    while(removedFile != null || removed.TryDequeue(out removedFile)) {
                                        if(!await IsIgnored(parameters.Directory ?? ".", removedFile)) {
                                            removedFiles.Add(removedFile.Replace('\\', '/'));
                                        }
                                        removedFile = null;
                                    }
                                    watcher.EnableRaisingEvents = false;
                                    if(source.IsCancellationRequested) {
                                        return 0;
                                    }
                                }
                            }
                            first = false;
                            var workflows = parameters.WorkflowFiles;
                            if(workflows == null || workflows.Length == 0) {
                                var pWorkflows = parameters.Workflows;
                                if(string.IsNullOrEmpty(pWorkflows)) {
                                    pWorkflows = Path.Join(parameters.Directory ?? ".", ".github/workflows");
                                }
                                if(Directory.Exists(pWorkflows)) {
                                    try {
                                        workflows = Directory.GetFiles(pWorkflows, "*.yml", new EnumerationOptions { RecurseSubdirectories = false, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true }).Concat(Directory.GetFiles(pWorkflows, "*.yaml", new EnumerationOptions { RecurseSubdirectories = false, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true })).ToArray();
                                        if((workflows == null || workflows.Length == 0)) {
                                            Console.Error.WriteLine($"No workflow *.yml / *.yaml file found inside of {pWorkflows}");
                                            return 1;
                                        }
                                    } catch {
                                        Console.Error.WriteLine($"Failed to read directory {pWorkflows}");
                                        return 1;
                                    }
                                } else if (File.Exists(pWorkflows)) {
                                    workflows = new[] { pWorkflows };
                                } else {
                                    Console.Error.WriteLine($"No such file or directory {pWorkflows}");
                                    return 1;
                                }
                            }
                            if(expandAzurePipeline) {
                                try {
                                    foreach(var workflow in workflows) {
                                        var name = Path.GetRelativePath(parameters.Directory ?? ".", workflow).Replace('\\', '/');
                                        var res = await AzurePipelinesExpander.ExpandCurrentPipeline(parameters, name);
                                        Console.WriteLine(res);
                                    }
                                    return 0;
                                } catch (Exception except) {
                                    Console.WriteLine($"Exception: {except.Message}, {except.StackTrace}");
                                    return 1;
                                } finally {
                                    cancelWorkflow = null;
                                }
                            }
                            try {
                                HttpClientHandler handler = new HttpClientHandler() {
                                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                                };  
                                var client = new HttpClient(handler);
                                client.DefaultRequestHeaders.Add("X-GitHub-Event", parameters.Event);
                                var b = new UriBuilder(parameters.Server);
                                var query = new QueryBuilder();
                                b.Path = "_apis/v1/Message/schedule2";
                                var mp = new MultipartFormDataContent();
                                List<Stream> workflowsToDispose = new List<Stream>();
                                HttpResponseMessage resp = null;
                                try {
                                    foreach(var w in workflows) {
                                        try {
                                            var workflow = File.OpenRead(w);
                                            workflowsToDispose.Add(workflow);
                                            var name = Path.GetRelativePath(parameters.Directory ?? ".", w).Replace('\\', '/');
                                            mp.Add(new StreamContent(workflow), name, name);
                                        } catch(Exception ex) {
                                            Console.WriteLine($"Failed to read file: {w}, Details: {ex.Message}");
                                            return 1;
                                        }
                                    }
                                    
                                    List<string> wenv = new List<string>();
                                    List<string> wsecrets = new List<string>();
                                    if(parameters.EnvFile?.Length > 0) {
                                        foreach(var file in parameters.EnvFile) {
                                            try {
                                                wenv.AddRange(Util.ReadEnvFile(file));
                                            } catch(Exception ex) {
                                                Console.WriteLine($"Failed to read file: {file}, Details: {ex.Message}");
                                                return 1;
                                            }
                                        }
                                    }
                                    if(parameters.SecretFiles?.Length > 0) {
                                        foreach(var file in parameters.SecretFiles) {
                                            try {
                                                wsecrets.AddRange(Util.ReadEnvFile(file));
                                            } catch(Exception ex) {
                                                Console.WriteLine($"Failed to read file: {file}, Details: {ex.Message}");
                                                return 1;
                                            }
                                        }
                                    }
                                    if(parameters.Job != null) {
                                        query.Add("job", parameters.Job);
                                    }
                                    if(parameters.Matrix?.Length > 0) {
                                        query.Add("matrix", parameters.Matrix);
                                    }
                                    if(parameters.List) {
                                        query.Add("list", "1");
                                    }
                                    if(parameters.Platform?.Length > 0) {
                                        query.Add("platform", parameters.Platform);
                                    }
                                    if(parameters.LocalRepositories?.Length > 0) {
                                        query.Add("taskNames", (from r in parameters.LocalRepositories select r.Substring(0, r.IndexOf('='))));
                                    }
                                    if(parameters.Env?.Length > 0) {
                                        for(int i = 0; i < parameters.Env.Length; i++ ) {
                                            var e = parameters.Env[i];
                                            if(e.IndexOf('=') > 0) {
                                                wenv.Add(e);
                                            } else {
                                                var envvar = Environment.GetEnvironmentVariable(e);
                                                if(envvar == null) {
                                                    await Console.Out.WriteAsync($"{e}=");
                                                    envvar = await Console.In.ReadLineAsync();
                                                    parameters.Env[i] = $"{e}={envvar}";
                                                }
                                                wenv.Add($"{e}={envvar}");
                                            }
                                        }
                                    }
                                    if(parameters.Secrets?.Length > 0) {
                                        for(int i = 0; i < parameters.Secrets.Length; i++ ) {
                                            var e = parameters.Secrets[i];
                                            if(e.IndexOf('=') > 0) {
                                                wsecrets.Add(e);
                                            } else {
                                                var envvar = Environment.GetEnvironmentVariable(e);
                                                if(envvar == null) {
                                                    await Console.Out.WriteAsync($"{e}=");
                                                    envvar = ReadSecret();
                                                    parameters.Secrets[i] = $"{e}={envvar}";
                                                }
                                                wsecrets.Add($"{e}={envvar}");
                                            }
                                        }
                                    }
                                    if(wenv.Count > 0) {
                                        query.Add("env", wenv);
                                    }
                                    if(wsecrets.Count > 0) {
                                        query.Add("secrets", wsecrets);
                                    }
                                    if(parameters.RemoteCheckout) {
                                        query.Add("localcheckout", "false");
                                    }
                                    JObject payloadContent = new JObject();
                                    {
                                        var sha = parameters.Sha;
                                        var bf = "0000000000000000000000000000000000000000";
                                        var user = JObject.FromObject(new { login = parameters.Actor, name = parameters.Actor, email = $"{parameters.Actor}@runner.server.localhost", id = 976638, type = "user" });
                                        payloadContent["sender"] = user;
                                        var Ref = parameters.Ref;
                                        string repofullname = parameters.Repository;
                                        try {
                                            string line = null;
                                            EventHandler<ProcessDataReceivedEventArgs> handleoutput = (s, e) => {
                                                if(line == null) {
                                                    line = e.Data;
                                                }
                                            };
                                            var git = WhichUtil.Which("git", true);
                                            GitHub.Runner.Sdk.ProcessInvoker gitinvoker;
                                            if(string.IsNullOrEmpty(Ref)) {
                                                gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
                                                gitinvoker.OutputDataReceived += handleoutput;
                                                var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                                                await gitinvoker.ExecuteAsync(parameters.Directory ?? Path.GetFullPath("."), git, "tag --points-at HEAD", new Dictionary<string, string>(), source.Token);
                                                if(line != null) {
                                                    Ref = "refs/tags/" + line;
                                                }
                                            }
                                            if(string.IsNullOrEmpty(Ref) || string.IsNullOrEmpty(repofullname)) {
                                                gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
                                                gitinvoker.OutputDataReceived += handleoutput;
                                                await gitinvoker.ExecuteAsync(parameters.Directory ?? Path.GetFullPath("."), git, "symbolic-ref HEAD", new Dictionary<string, string>(), source.Token);
                                                if(line != null) {
                                                    var _ref = line;
                                                    if(string.IsNullOrEmpty(Ref)) {
                                                        Ref = _ref;
                                                    }
                                                    if(string.IsNullOrEmpty(repofullname)) {
                                                        line = null;
                                                        gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
                                                        gitinvoker.OutputDataReceived += handleoutput;
                                                        await gitinvoker.ExecuteAsync(parameters.Directory ?? Path.GetFullPath("."), git, $"for-each-ref --format=%(upstream:short) {_ref}", new Dictionary<string, string>(), source.Token);
                                                        if(line != null && line != "") {
                                                            var remote = line.Substring(0, line.IndexOf('/'));
                                                            if(parameters.DefaultBranch == null) {
                                                                line = null;
                                                                gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
                                                                gitinvoker.OutputDataReceived += handleoutput;
                                                                await gitinvoker.ExecuteAsync(parameters.Directory ?? Path.GetFullPath("."), git, $"symbolic-ref refs/remotes/{remote}/HEAD", new Dictionary<string, string>(), source.Token);
                                                                if(line != null && line.StartsWith($"refs/remotes/{remote}/")) {
                                                                    var defbranch = line.Substring($"refs/remotes/{remote}/".Length);
                                                                    parameters.DefaultBranch = defbranch;
                                                                }
                                                            }
                                                            line = null;
                                                            gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
                                                            gitinvoker.OutputDataReceived += handleoutput;
                                                            await gitinvoker.ExecuteAsync(parameters.Directory ?? Path.GetFullPath("."), git, $"remote get-url {remote}", new Dictionary<string, string>(), source.Token);
                                                            if(line != null) {
                                                                Regex repoRegex = new Regex("^.*[:/\\\\]([^:/\\\\]+)[/\\\\]([^:/\\\\]+)(.git)?$", RegexOptions.IgnoreCase);
                                                                var repoMatchResult = repoRegex.Match(line);
                                                                if(repoMatchResult.Success) {
                                                                    var owner = repoMatchResult.Groups[1].Value;
                                                                    var repo = repoMatchResult.Groups[2].Value;
                                                                    if(repo.EndsWith(".git")) {
                                                                        repo = repo.Substring(0, repo.Length - 4);
                                                                    }
                                                                    repofullname = owner + "/" +  repo;
                                                                }
                                                            }
                                                        }
                                                    }
                                                } else {
                                                    await Console.Error.WriteLineAsync("No default github.ref found");
                                                }
                                            }
                                            if(string.IsNullOrEmpty(sha)) {
                                                line = null;
                                                gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
                                                gitinvoker.OutputDataReceived += handleoutput;
                                                await gitinvoker.ExecuteAsync(parameters.Directory ?? Path.GetFullPath("."), git, "rev-parse HEAD", new Dictionary<string, string>(), source.Token);
                                                if(line != null && line != "HEAD" /* on failure git returns HEAD instead of a sha */) {
                                                    sha = line;
                                                    line = null;
                                                } else {
                                                    await Console.Error.WriteLineAsync("Couldn't retrieve github.sha");
                                                }
                                            }
                                        } catch {
                                            await Console.Error.WriteLineAsync("Failed to detect git repo the github context may have invalid values");
                                        }
                                        if(string.IsNullOrEmpty(repofullname) || !repofullname.Contains('/')) {
                                            repofullname = "Unknown/Unknown";
                                        }
                                        if(string.IsNullOrEmpty(Ref)) {
                                            Ref = "refs/heads/main";
                                        }
                                        if(string.IsNullOrEmpty(sha)) {
                                            sha = "0000000000000000000000000000000000000000";
                                        }
                                        query.Add("Ref", Ref);
                                        query.Add("Sha", sha);
                                        query.Add("Repository", repofullname);
                                        if(parameters.Event == "push") {
                                            var acommits = new JArray();
                                            var commit = JObject.FromObject(new { message = "", id = sha, added = addedFiles, removed = removedFiles, modified = changedFiles });
                                            acommits.AddFirst(commit);
                                            payloadContent["head_commit"] = commit;
                                            payloadContent["commits"] = acommits;
                                            payloadContent["pusher"] = user;
                                            payloadContent["before"] = bf;
                                            payloadContent["ref"] = Ref;
                                            payloadContent["after"] = sha;
                                        }
                                        var reposownername = repofullname.Split('/', 2)[0];
                                        var repoowner = String.Equals(parameters.Actor, reposownername, StringComparison.OrdinalIgnoreCase) ? user : JObject.FromObject(new { login = reposownername, name = reposownername, email = $"{reposownername}@runner.server.localhost", id = 976639, type = "user" });
                                        var repository = JObject.FromObject(new { owner = repoowner, default_branch = parameters.DefaultBranch ?? "main", master_branch = parameters.DefaultBranch ?? "master", name = repofullname.Split('/', 2)[1], full_name = repofullname });
                                        payloadContent["repository"] = repository;
                                    }
                                    
                                    if(!string.IsNullOrEmpty(parameters.Payload)) {
                                        try {
                                            // 
                                            var filec = await File.ReadAllTextAsync(parameters.Payload, Encoding.UTF8);
                                            var obj = JObject.Parse(filec);

                                            if(parameters.NoDefaultPayload) {  
                                                payloadContent = obj;
                                            } else {
                                                payloadContent.Merge(obj, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                                            }
                                        } catch(Exception ex) {
                                            Console.WriteLine($"Failed to read file: {parameters.Payload}, Details: {ex.Message}");
                                            return 1;
                                        }
                                    } else if(parameters.NoDefaultPayload) {
                                        payloadContent = new JObject();
                                    }
                                    if(parameters.Event == "workflow_dispatch") {
                                        var inputs = payloadContent.TryGetValue("inputs", out var oinputs) ? (JObject)oinputs : new JObject();
                                        payloadContent["inputs"] = inputs;
                                        if(parameters.InputFiles?.Length > 0) {
                                            foreach(var file in parameters.InputFiles) {
                                                try {
                                                    Util.ReadEnvFile(file, (k, v) => inputs[k] = v);
                                                } catch(Exception ex) {
                                                    Console.WriteLine($"Failed to read file: {file}, Details: {ex.Message}");
                                                    return 1;
                                                }
                                            }
                                        }
                                        if(parameters.Inputs?.Length > 0) {
                                            foreach(var input in parameters.Inputs) {
                                                var kv = input.Split('=', 2);
                                                inputs[kv[0]] = kv[1];
                                            }
                                        }
                                    }
                                    mp.Add(new StringContent(payloadContent.ToString()), "event", "event.json");
                                    var (envSecrets, envVars) = await ReadSecretsAndVariables(parameters);
                                    var ffVars = envVars.GetOrAddValue("", () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                                    if(!ffVars.ContainsKey("system.runner.server.parameters")) {
                                        var inputs = new JObject();
                                        if(parameters.InputFiles?.Length > 0) {
                                            foreach(var file in parameters.InputFiles) {
                                                try {
                                                    Util.ReadEnvFile(file, (k, v) => inputs[k] = v);
                                                } catch(Exception ex) {
                                                    Console.WriteLine($"Failed to read file: {file}, Details: {ex.Message}");
                                                    return 1;
                                                }
                                            }
                                        }
                                        if(parameters.Inputs?.Length > 0) {
                                            foreach(var input in parameters.Inputs) {
                                                var kv = input.Split('=', 2);
                                                inputs[kv[0]] = kv[1];
                                            }
                                        }
                                        ffVars["system.runner.server.parameters"] = inputs.ToString();
                                    }
                                    foreach(var envVarKv in envSecrets) {
                                        var ser = new YamlDotNet.Serialization.SerializerBuilder().Build();
                                        mp.Add(new StringContent(ser.Serialize(envVarKv.Value)), "actions-environment-secrets", $"{envVarKv.Key}.secrets");
                                    }
                                    foreach(var envVarKv in envVars) {
                                        var ser = new YamlDotNet.Serialization.SerializerBuilder().Build();
                                        mp.Add(new StringContent(ser.Serialize(envVarKv.Value)), "actions-environment-variables", $"{envVarKv.Key}.vars");
                                    }
                                    queryParam?.Invoke(query);
                                    b.Query = query.ToQueryString().ToString().TrimStart('?');
                                    resp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, b.Uri.ToString()) { Content = mp }, HttpCompletionOption.ResponseHeadersRead);
                                    resp.EnsureSuccessStatusCode();
                                } finally {
                                    foreach(var fstream in workflowsToDispose) {
                                        await fstream.DisposeAsync();
                                    }
                                }
                                
                                Dictionary<Guid, TimeLineEntry> timelineRecords = new Dictionary<Guid, TimeLineEntry>();
                                int col = 0;
                                bool hasErrors = false;
                                bool hasAny = false;
                                // Track if we are receiving duplicated finish jobs
                                List<Job> jobs = new List<Job>();
                                var forceCancelWorkflow = () => {
                                    cancelWorkflow = null;
                                    Console.WriteLine($"CTRL+C received Force Cancel Running Jobs");
                                    var runIds = (from j in jobs.ToArray() select j.runid).ToHashSet();
                                    foreach(var runId in runIds) {
                                        client.PostAsync(GetForceCancelWorkflowUrl(b.ToString(), runId), null, CancellationToken.None);
                                    }
                                };
                                cancelWorkflow = () => {
                                    cancelWorkflow = forceCancelWorkflow;
                                    Console.WriteLine($"CTRL+C received Cancel Running Jobs");
                                    var runIds = (from j in jobs.ToArray() select j.runid).ToHashSet();
                                    foreach(var runId in runIds) {
                                        client.PostAsync(GetCancelWorkflowUrl(b.ToString(), runId), null, CancellationToken.None);
                                    }
                                };
                                
                                List<Guid> alreadyFinished = new List<Guid>();
                                LoadEntries loadWorkflowName = (ref TimeLineEntry rec, List<Job> jobs, Guid id) => {
                                    if(rec?.WorkflowName == null) {
                                        var _job = (from job in jobs where job.JobId == id select job).FirstOrDefault();
                                        bool customrec = rec == null;
                                        if(customrec) {
                                            rec = new TimeLineEntry() { TimeLine = new List<TimelineRecord>() { new TimelineRecord() { Id = id, Name = _job?.name } }, Color = (ConsoleColor) col + 1, Pending = new List<WebConsoleEvent>() };
                                            col = (col + 1) % 14;
                                        }
                                        rec.WorkflowName = _job?.workflowname;
                                        if(rec.WorkflowName == null) {
                                            var _rec = rec;
                                            return Task.Run(async () => {
                                                var _job = JsonConvert.DeserializeObject<Job>(await client.GetStringAsync(GetJobUrl(b.ToString(), id)));
                                                if(_job != null) {
                                                    jobs.Add(_job);
                                                    // Cancel Workflows started after we cancelled the whole thing
                                                    if(cancelWorkflow == null && _job?.runid != null) {
                                                        client.PostAsync(GetForceCancelWorkflowUrl(b.ToString(), _job.runid), null, CancellationToken.None);
                                                    }
                                                }
                                                _rec.WorkflowName = _job?.workflowname;
                                            });
                                        }
                                    }
                                    return Task.CompletedTask;
                                };
                                Func<JobCompletedEvent, Task> printFinishJob = async ev => {
                                    if(alreadyFinished.Contains(ev.JobId)) {
                                        return;
                                    }
                                    alreadyFinished.Add(ev.JobId);
                                    var rec = (from r in timelineRecords where r.Value.TimeLine?[0]?.Id == ev.JobId select r.Value).FirstOrDefault();
                                    try {
                                        await loadWorkflowName(ref rec, jobs, ev.JobId);
                                    } catch {
                                        
                                    }
                                    WriteLogLine((int)rec.Color, $"{(rec.WorkflowName != null ? $"{rec.WorkflowName} / " : "")}{rec.TimeLine[0].Name}", $"Job Completed with Status: {ev.Result.ToString()}");
                                };
                                var eventstream = resp.Content.ReadAsStream();
                            
                                try {
                                    using(TextReader reader = new StreamReader(eventstream)) {
                                        while(!source.IsCancellationRequested) {
                                            var line = await reader.ReadLineAsync();
                                            if(line == null) {
                                                break;
                                            }
                                            var data = await reader.ReadLineAsync();
                                            data = data.Substring("data: ".Length);
                                            await reader.ReadLineAsync();
                                            if(line == "event: ") {
                                                break;
                                            }
                                            if(parameters.Trace) {
                                                Console.WriteLine($"##[Trace]{line}");
                                                Console.WriteLine($"##[Trace]{data}");
                                            }
                                            if(!parameters.Quiet && line == "event: log") {
                                                var e = JsonConvert.DeserializeObject<WebConsoleEvent>(data);
                                                TimeLineEntry rec;
                                                if(!timelineRecords.TryGetValue(e.timelineId, out rec)) {
                                                    timelineRecords[e.timelineId] = new TimeLineEntry() { Color = (ConsoleColor) col + 1, Pending = new List<WebConsoleEvent>() { e } };
                                                    col = (col + 1) % 14;
                                                    continue;
                                                } else if(rec.Pending?.Count > 0) {
                                                    // Fix webconsole invalid print order
                                                    rec.Pending.Add(e);
                                                    continue;
                                                } else if(rec.RecordId != e.record.StepId) {
                                                    if(rec.RecordId != Guid.Empty && rec.TimeLine != null && (rec.TimeLine.Count == 0 || rec.RecordId != rec.TimeLine[0].Id)) {
                                                        var record = rec.TimeLine.Find(r => r.Id == rec.RecordId);
                                                        if(record == null || !record.Result.HasValue) {
                                                            rec.Pending.Add(e);
                                                            continue;
                                                        }                                    
                                                        WriteLogLine((int)rec.Color, $"{(rec.WorkflowName != null ? $"{rec.WorkflowName} / " : "")}{rec.TimeLine[0].Name}", $"{record.Result.Value.ToString()}: {record.Name}");
                                                    }
                                                    rec.RecordId = e.record.StepId;
                                                    if(rec.TimeLine != null) {
                                                        var record = rec.TimeLine.Find(r => r.Id == e.record.StepId);
                                                        if(record == null) {
                                                            rec.Pending.Add(e);
                                                            rec.RecordId = Guid.Empty;
                                                            continue;
                                                        }
                                                        try {
                                                            await loadWorkflowName(ref rec, jobs, rec.TimeLine[0].Id);
                                                        } catch {
                                                            
                                                        }
                                                        WriteLogLine((int)rec.Color, $"{(rec.WorkflowName != null ? $"{rec.WorkflowName} / " : "")}{rec.TimeLine[0].Name}", $"Running: {record.Name}");
                                                    }
                                                }
                                                foreach (var webconsoleline in e.record.Value) {
                                                    WriteLogLine((int)rec.Color, webconsoleline);
                                                }
                                            }
                                            if(line == "event: timeline") {
                                                var e = JsonConvert.DeserializeObject<TimeLineEvent>(data);
                                                if(timelineRecords.ContainsKey(e.timelineId)) {
                                                    timelineRecords[e.timelineId].TimeLine = e.timeline;
                                                } else {
                                                    timelineRecords[e.timelineId] = new TimeLineEntry { TimeLine = e.timeline, Color = (ConsoleColor) col + 1, Pending = new List<WebConsoleEvent>()};
                                                    col = (col + 1) % 14;
                                                }
                                                while(timelineRecords[e.timelineId].Pending.Count > 0) {
                                                    var e2 = timelineRecords[e.timelineId].Pending[0];
                                                    if(timelineRecords[e.timelineId].RecordId != e2.record.StepId) {
                                                        if(timelineRecords[e.timelineId].RecordId != Guid.Empty && timelineRecords[e.timelineId].TimeLine != null && (timelineRecords[e.timelineId].TimeLine.Count == 0 || timelineRecords[e.timelineId].RecordId != timelineRecords[e.timelineId].TimeLine[0].Id)) {
                                                            var record = timelineRecords[e.timelineId].TimeLine.Find(r => r.Id == timelineRecords[e.timelineId].RecordId);
                                                            if(record == null || !record.Result.HasValue) {
                                                                break;
                                                            }
                                                            var rec = timelineRecords[e.timelineId];
                                                            WriteLogLine((int)rec.Color, $"{(rec.WorkflowName != null ? $"{rec.WorkflowName} / " : "")}{rec.TimeLine[0].Name}", $"{record.Result.Value.ToString()}: {record.Name}");
                                                        }
                                                        timelineRecords[e.timelineId].RecordId = e2.record.StepId;
                                                        if(timelineRecords[e.timelineId].TimeLine != null) {
                                                            var record = timelineRecords[e.timelineId].TimeLine.Find(r => r.Id == timelineRecords[e.timelineId].RecordId);
                                                            if(record == null) {
                                                                timelineRecords[e.timelineId].RecordId = Guid.Empty;
                                                                break;
                                                            }
                                                            var rec = timelineRecords[e.timelineId];
                                                            try {
                                                                await loadWorkflowName(ref rec, jobs, rec.TimeLine[0].Id);
                                                            } catch {
                                                                
                                                            }
                                                            WriteLogLine((int)rec.Color, $"{(rec.WorkflowName != null ? $"{rec.WorkflowName} / " : "")}{rec.TimeLine[0].Name}", $"Running: {record.Name}");
                                                        }
                                                    }
                                                    foreach (var webconsoleline in e2.record.Value) {
                                                        WriteLogLine((int)timelineRecords[e.timelineId].Color, webconsoleline);
                                                    }
                                                    timelineRecords[e.timelineId].Pending.RemoveAt(0);
                                                }
                                                if(!parameters.Quiet && timelineRecords[e.timelineId].RecordId != Guid.Empty && timelineRecords != null && e.timeline[0].State == TimelineRecordState.Completed) {
                                                    var record = e.timeline.Find(r => r.Id == timelineRecords[e.timelineId].RecordId);
                                                    if(record != null && record.Result.HasValue) {
                                                        var rec = timelineRecords[e.timelineId];
                                                        rec.RecordId = Guid.Empty;
                                                        WriteLogLine((int)rec.Color, $"{(rec.WorkflowName != null ? $"{rec.WorkflowName} / " : "")}{rec.TimeLine[0].Name}", $"{record.Result.Value.ToString()}: {record.Name}");
                                                    }
                                                }
                                            }
                                            if(line == "event: repodownload") {
                                                var endpoint = JsonConvert.DeserializeObject<RepoDownload>(data);
                                                Task.Run(async () => {
                                                    var reporoot = endpoint.Repository == null ? Path.GetFullPath(parameters.Directory ?? ".") : (from r in parameters.LocalRepositories where MatchRepository(r, endpoint.Repository) select Path.GetFullPath(r.Substring($"{endpoint.Repository}=".Length))).LastOrDefault();
                                                    if(string.Equals(endpoint.Format, "repoexists", StringComparison.OrdinalIgnoreCase) && endpoint.Path == null) {
                                                        if(reporoot == null) {
                                                            await client.DeleteAsync(parameters.Server + endpoint.Url, token);
                                                        } else {
                                                            await client.PostAsync(parameters.Server + endpoint.Url, new StringContent("Ok"), token);
                                                        }
                                                    } else if(reporoot != null && endpoint.Path == null && endpoint.Format == null) {
                                                        var repodownload = new MultipartFormDataContent();
                                                        List<Stream> streamsToDispose = new List<Stream>();
                                                        try {
                                                            try {
                                                                await CollectRepoFiles(reporoot, reporoot, endpoint, repodownload, streamsToDispose, 0, parameters, source);
                                                            } catch {
                                                                foreach(var fstream in streamsToDispose) {
                                                                    await fstream.DisposeAsync();
                                                                }
                                                                streamsToDispose.Clear();
                                                                repodownload.Dispose();
                                                                repodownload = new MultipartFormDataContent();
                                                            }
                                                            if(streamsToDispose.Count == 0) {
                                                                foreach(var w in Directory.EnumerateFiles(reporoot, "*", new EnumerationOptions { RecurseSubdirectories = true, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true })) {
                                                                    var relpath = Path.GetRelativePath(reporoot, w).Replace('\\', '/');
                                                                    var file = File.OpenRead(w);
                                                                    streamsToDispose.Add(file);
                                                                    var mode = "644";
                                                                    if(!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                                                                        try {
                                                                            var finfo = new Mono.Unix.UnixSymbolicLinkInfo(w);
                                                                            if(finfo.IsSymbolicLink) {
                                                                                var dest = finfo.ContentsPath;
                                                                                repodownload.Add(new StringContent(dest), "lnk:" + relpath);
                                                                                continue;
                                                                            }
                                                                            if(finfo.FileAccessPermissions.HasFlag(Mono.Unix.FileAccessPermissions.UserExecute)) {
                                                                                mode = "755";
                                                                            }
                                                                        }
                                                                        catch {

                                                                        }
                                                                    } else {
                                                                        try {
                                                                            if(new FileInfo(w).Attributes.HasFlag(FileAttributes.ReparsePoint)){
                                                                                var dest = ReadSymlinkWindows(w);
                                                                                repodownload.Add(new StringContent(dest.Replace('\\', '/')), "lnk:" + relpath);
                                                                                continue;
                                                                            }
                                                                        } catch {

                                                                        }
                                                                    }
                                                                    repodownload.Add(new StreamContent(file), mode + ":" + relpath, relpath);
                                                                }
                                                            }
                                                            repodownload.Headers.ContentType.MediaType = "application/octet-stream";
                                                            await client.PostAsync(parameters.Server + endpoint.Url, repodownload, token);
                                                        } finally {
                                                            foreach(var fstream in streamsToDispose) {
                                                                await fstream.DisposeAsync();
                                                            }
                                                            repodownload.Dispose();
                                                        }
                                                    } else if(reporoot != null && endpoint.Path != null && string.Equals(endpoint.Format, "file", StringComparison.OrdinalIgnoreCase)) {
                                                        var w = Path.Join(reporoot, endpoint.Path);
                                                        try {
                                                            using(var file = File.OpenRead(w)) {
                                                                var content = new StreamContent(file);
                                                                content.Headers.ContentType = new MediaTypeHeaderValue("text/plain") { CharSet = "utf8" };
                                                                (await client.PostAsync(parameters.Server + endpoint.Url, content, token)).EnsureSuccessStatusCode();
                                                            }
                                                        } catch {
                                                            await client.DeleteAsync(parameters.Server + endpoint.Url, token);
                                                        }
                                                    } else if(reporoot != null && (string.Equals(endpoint.Format, "zip", StringComparison.OrdinalIgnoreCase) || string.Equals(endpoint.Format, "taskzip", StringComparison.OrdinalIgnoreCase))) {
                                                        try {
                                                            bool taskzip = string.Equals(endpoint.Format, "taskzip", StringComparison.OrdinalIgnoreCase);
                                                            using(var stream = new MemoryStream()) {
                                                                using(var zipFile = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create, true)) {
                                                                    foreach(var w in Directory.EnumerateFiles(reporoot, "*", new EnumerationOptions { RecurseSubdirectories = true, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true })) {
                                                                        var relpath = Path.GetRelativePath(reporoot, w).Replace('\\', '/');
                                                                        var entry = zipFile.CreateEntry(taskzip ? $"{relpath}" : $"archive/{relpath}", System.IO.Compression.CompressionLevel.NoCompression);
                                                                        using(var file = File.OpenRead(w))
                                                                        using(var fileout = entry.Open()) {
                                                                            file.CopyTo(fileout);
                                                                        }
                                                                    }
                                                                }
                                                                stream.Position = 0;
                                                                (await client.PostAsync(parameters.Server + endpoint.Url, new StreamContent(stream), token)).EnsureSuccessStatusCode();
                                                            }
                                                        } catch {
                                                            await client.DeleteAsync(parameters.Server + endpoint.Url, token);
                                                        }
                                                    } else if(reporoot != null && string.Equals(endpoint.Format, "tar", StringComparison.OrdinalIgnoreCase)) {
                                                        try {
                                                            string tar = WhichUtil.Which("tar", require: true);
                                                            var cwd = Directory.GetParent(reporoot);
                                                            using(var proc = new System.Diagnostics.Process()) {
                                                                proc.StartInfo.FileName = tar;
                                                                proc.StartInfo.Arguments = $"czf - \"{Path.GetFileName(reporoot).Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
                                                                proc.StartInfo.WorkingDirectory = cwd.FullName;
                                                                proc.StartInfo.RedirectStandardOutput = true;
                                                                proc.Start();
                                                                (await client.PostAsync(parameters.Server + endpoint.Url, new StreamContent(proc.StandardOutput.BaseStream), token)).EnsureSuccessStatusCode();
                                                                proc.WaitForExit();
                                                            }
                                                            
                                                        } catch {
                                                            await client.DeleteAsync(parameters.Server + endpoint.Url, token);
                                                        }
                                                    } else {
                                                        await client.DeleteAsync(parameters.Server + endpoint.Url, token);
                                                    }
                                                });
                                            }
                                            if(line == "event: workflow") {
                                                var _workflow = JsonConvert.DeserializeObject<WorkflowEventArgs>(data);
                                                var rec = (from j in jobs where j.runid == _workflow.runid && j.JobId == j.TimeLineId select (from r in timelineRecords where r.Value.TimeLine?[0]?.Id == j.TimeLineId select r.Value).FirstOrDefault()).FirstOrDefault();
                                                TaskResult result = TaskResult.Failed;
                                                if(_workflow.Success) {
                                                    result = TaskResult.Succeeded;
                                                }
                                                if(rec != null) {
                                                    WriteLogLine((int)rec.Color, $"{(rec.WorkflowName != null ? $"{rec.WorkflowName} / " : "")}{rec.TimeLine[0].Name}", $"Workflow {_workflow.runid} Completed with Status: {result.ToString()}");
                                                } else {
                                                    Console.WriteLine($"Workflow {_workflow.runid} finished with status {result.ToString()}");
                                                }
                                                hasErrors |= !_workflow.Success;
                                                hasAny = true;
                                            }
                                            if(line == "event: job") {
                                                var job = JsonConvert.DeserializeObject<Job>(data);
                                                jobs.Add(job);
                                            }
                                            if(line == "event: finish") {
                                                var ev = JsonConvert.DeserializeObject<JobCompletedEvent>(data);
                                                await printFinishJob(ev);
                                            }
                                        }
                                    }
                                } finally {
                                    if(!string.IsNullOrEmpty(parameters.ArtifactOutputDir) || !(Console.IsInputRedirected || Console.IsOutputRedirected || skipAskToSaveArtifact)) {
                                        var runIds = (from j in jobs select j.runid).ToHashSet();
                                        foreach(var runId in runIds) {
                                            try {
                                                var artifactUri = new UriBuilder(parameters.Server);
                                                artifactUri.Path = $"/_apis/pipelines/workflows/{runId}/artifacts";
                                                var artifacts = JsonConvert.DeserializeObject<VssJsonCollectionWrapper<ArtifactResponse[]>>(await client.GetStringAsync(artifactUri.ToString()));
                                                if(artifacts.Count > 0 && string.IsNullOrEmpty(parameters.ArtifactOutputDir)) {
                                                    await Console.Out.WriteAsync($"Where do you want to store your generated Workflow Artifacts? ( Leave empty to discard them ): ");
                                                    parameters.ArtifactOutputDir = await Console.In.ReadLineAsync();
                                                    if(string.IsNullOrEmpty(parameters.ArtifactOutputDir)) {
                                                        skipAskToSaveArtifact = true;
                                                        break;
                                                    }
                                                }
                                                foreach(var artifact in artifacts.Value) {
                                                    try {
                                                        var artfactBasePath = Path.Combine(parameters.ArtifactOutputDir, runId.ToString(), artifact.name);
                                                        Directory.CreateDirectory(artfactBasePath);
                                                        Console.WriteLine($"Downloading {runId}/{artifact.name}");
                                                        var files = JsonConvert.DeserializeObject<VssJsonCollectionWrapper<DownloadInfo[]>>(await client.GetStringAsync(artifact.fileContainerResourceUrl));
                                                        foreach(var file in files.Value) {
                                                            try {
                                                                var destpath = Path.Combine(artfactBasePath, file.path.Replace('\\', '/'));
                                                                Directory.CreateDirectory(Path.GetDirectoryName(destpath));
                                                                using(var content = await client.GetStreamAsync(file.contentLocation))
                                                                using(var targetStream = new FileStream(destpath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write)) {
                                                                    await content.CopyToAsync(targetStream);
                                                                }
                                                            } catch {

                                                            }
                                                        }
                                                    } catch {

                                                    }
                                                }
                                                
                                            } catch {

                                            }
                                        }
                                    }
                                    if(parameters.LogOutputDir?.Length > 0) {
                                        Regex special = new Regex("[*'\",_&#^@\\/\r\n ]");
                                        foreach(var job in jobs) {
                                            try {
                                                var logBasePath = Path.Combine(parameters.LogOutputDir, job.runid.ToString(), special.Replace(job.name, "-"));
                                                Directory.CreateDirectory(logBasePath);
                                                Console.WriteLine($"Downloading Logs {job.runid}/{special.Replace(job.name, "-")}");
                                                var timeLineRecords = JsonConvert.DeserializeObject<List<TimelineRecord>>(await client.GetStringAsync(parameters.Server + $"/_apis/v1/Timeline/{job.TimeLineId.ToString()}"));
                                                foreach(var timeLineRecord in timeLineRecords) {
                                                    try {
                                                        if(timeLineRecord?.Log?.Id != null) {
                                                            var destpath = Path.Combine(logBasePath, timeLineRecord.Log.Id + "-" + special.Replace(timeLineRecord.Name, "-"));
                                                            Directory.CreateDirectory(Path.GetDirectoryName(destpath));
                                                            var logFileUri = new UriBuilder(parameters.Server);
                                                            logFileUri.Path = $"/_apis/v1/Logfiles/{timeLineRecord.Log.Id}";
                                                            using(var content = await client.GetStreamAsync(logFileUri.ToString()))
                                                            using(var targetStream = new FileStream(destpath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write)) {
                                                                await content.CopyToAsync(targetStream);
                                                            }
                                                        }
                                                    } catch {

                                                    }
                                                }
                                            } catch {

                                            }
                                        }
                                    }
                                }
                                if(hasErrors) {
                                    Console.WriteLine("All Workflows finished, at least one workflow failed");
                                } else if(!hasAny) {
                                    Console.WriteLine("All Workflows skipped, due to filters");
                                } else {
                                    Console.WriteLine("All Workflows finished successfully");
                                }
                                return hasErrors ? 1 : 0;
                            } catch (Exception except) {
                                Console.WriteLine($"Exception: {except.Message}, {except.StackTrace}");
                                return 1;
                            } finally {
                                cancelWorkflow = null;
                            }
                        });
                        if(!parameters.Watch && !parameters.Interactive) {
                            return ret;
                        }
                    }
                } finally {
                    source.Cancel();
                    foreach(var l in listener.ToArray()) {
                        try {
                            await l;
                        } catch {

                        }
                    }
                }
                return 0;
            };
            var tokenOpt = new Option<string>(
                "--token",
                description: "custom runner token to use");
            var binder = new MyCustomBinder(bindingContext => {
                var parameters = new Parameters();
                parameters.WorkflowFiles = bindingContext.ParseResult.GetValueForOption(workflowOption);
                parameters.Server = bindingContext.ParseResult.GetValueForOption(serverOpt);
                parameters.Payload = bindingContext.ParseResult.GetValueForOption(payloadOpt);
                parameters.NoDefaultPayload = bindingContext.ParseResult.GetValueForOption(noDefaultPayloadOpt);
                parameters.Event = bindingContext.ParseResult.GetValueForOption(eventOpt);
                parameters.Env = bindingContext.ParseResult.GetValueForOption(envOpt);
                parameters.EnvFile = bindingContext.ParseResult.GetValueForOption(envFile);
                parameters.Vars = bindingContext.ParseResult.GetValueForOption(varOpt);
                parameters.VarFiles = bindingContext.ParseResult.GetValueForOption(varFileOpt);
                parameters.Secrets = bindingContext.ParseResult.GetValueForOption(secretOpt);
                parameters.SecretFiles = bindingContext.ParseResult.GetValueForOption(secretFileOpt);
                parameters.EnvironmentSecrets = bindingContext.ParseResult.GetValueForOption(environmentSecretOpt);
                parameters.EnvironmentSecretFiles = bindingContext.ParseResult.GetValueForOption(environmentSecretFileOpt);
                parameters.EnvironmentVars = bindingContext.ParseResult.GetValueForOption(environmentVarOpt);
                parameters.EnvironmentVarFiles = bindingContext.ParseResult.GetValueForOption(environmentVarFileOpt);
                parameters.Job = bindingContext.ParseResult.GetValueForOption(jobOpt);
                parameters.Matrix = bindingContext.ParseResult.GetValueForOption(matrixOpt);
                parameters.List = bindingContext.ParseResult.GetValueForOption(listOpt);
                parameters.Workflows = bindingContext.ParseResult.GetValueForOption(workflowsOpt);
                parameters.Platform = bindingContext.ParseResult.GetValueForOption(platformOption);
                parameters.Actor = bindingContext.ParseResult.GetValueForOption(actorOpt);
                parameters.Watch = bindingContext.ParseResult.GetValueForOption(watchOpt);
                parameters.Interactive = bindingContext.ParseResult.GetValueForOption(interactiveOpt);
                parameters.Trace = bindingContext.ParseResult.GetValueForOption(traceOpt);
                parameters.Quiet = bindingContext.ParseResult.GetValueForOption(quietOpt);
                parameters.Privileged = bindingContext.ParseResult.GetValueForOption(privilegedOpt);
                parameters.Userns = bindingContext.ParseResult.GetValueForOption(usernsOpt);
                parameters.ContainerPlatform = bindingContext.ParseResult.GetValueForOption(containerPlatformOpt);
                parameters.KeepContainer = bindingContext.ParseResult.GetValueForOption(keepContainerOpt);
                parameters.Directory = bindingContext.ParseResult.GetValueForOption(DirectoryOpt);
                parameters.Verbose = bindingContext.ParseResult.GetValueForOption(verboseOpt);
                parameters.Parallel = bindingContext.ParseResult.GetValueForOption(parallelOpt);
                parameters.NoCopyGitDir = bindingContext.ParseResult.GetValueForOption(noCopyGitDirOpt);
                parameters.KeepRunnerDirectory = bindingContext.ParseResult.GetValueForOption(keepRunnerDirectoryOpt);
                parameters.RunnerDirectory = bindingContext.ParseResult.GetValueForOption(runnerDirectoryOpt);
                parameters.NoSharedToolcache = bindingContext.ParseResult.GetValueForOption(noSharedToolCacheOpt);
                parameters.NoReuse = bindingContext.ParseResult.GetValueForOption(noReuseOpt);
                parameters.GitServerUrl = bindingContext.ParseResult.GetValueForOption(gitServerUrlOpt);
                parameters.GitApiServerUrl = bindingContext.ParseResult.GetValueForOption(gitApiServerUrlOpt);
                parameters.GitGraphQlServerUrl = bindingContext.ParseResult.GetValueForOption(gitGraphQlOpt);
                if(parameters.GitApiServerUrl == null) {
                    parameters.GitApiServerUrl = parameters.GitServerUrl == "https://github.com" ? "https://api.github.com" : $"{parameters.GitServerUrl}/api/v3";
                }
                if(parameters.GitGraphQlServerUrl == null) {
                    parameters.GitGraphQlServerUrl = parameters.GitServerUrl == "https://github.com" ? "https://api.github.com/graphql" : $"{parameters.GitServerUrl}/api/graphql";
                }
                parameters.GitTarballUrl = bindingContext.ParseResult.GetValueForOption(gitTarballUrlOpt);
                parameters.GitZipballUrl = bindingContext.ParseResult.GetValueForOption(gitZipballUrlOpt);
                parameters.GitHubConnect = bindingContext.ParseResult.GetValueForOption(githubConnectOpt);
                parameters.GitHubConnectToken = bindingContext.ParseResult.GetValueForOption(githubConnectTokenOpt);
                parameters.RunnerPath = bindingContext.ParseResult.GetValueForOption(runnerPathOpt);
                parameters.RunnerVersion = bindingContext.ParseResult.GetValueForOption(runnerVersionOpt);
                parameters.RemoteCheckout = bindingContext.ParseResult.GetValueForOption(remoteCheckoutOpt);
                parameters.ArtifactOutputDir = bindingContext.ParseResult.GetValueForOption(artifactOutputDirOpt);
                parameters.LogOutputDir = bindingContext.ParseResult.GetValueForOption(logOutputDirOpt);
                parameters.Repository = bindingContext.ParseResult.GetValueForOption(repositoryOpt);
                parameters.Sha = bindingContext.ParseResult.GetValueForOption(shaOpt);
                parameters.Ref = bindingContext.ParseResult.GetValueForOption(refOpt);
                parameters.Inputs = bindingContext.ParseResult.GetValueForOption(workflowInputsOpt);
                parameters.InputFiles = bindingContext.ParseResult.GetValueForOption(workflowInputFilesOpt);
                parameters.Token = bindingContext.ParseResult.GetValueForOption(tokenOpt);
                parameters.LocalRepositories = bindingContext.ParseResult.GetValueForOption(localrepositoriesOpt);
                return parameters;
            });

            foreach(var ev in validevents) {
                var cmd = new Command(ev, $"Same as adding `--event {ev}` to the cli, overrides any `--event <event>` option.");
                rootCommand.AddCommand(cmd);
                Func<Parameters, Task<int>> handler2 = (parameters) => {
                    parameters.Event = ev;
                    return handler(parameters);
                };
                cmd.SetHandler(handler2, binder);
                foreach(var opt in rootCommand.Options) {
                    if(!opt.Aliases.Contains("--event")) {
                        cmd.AddOption(opt);
                    }
                }
                if(ev == "workflow_dispatch" || ev == "azexpand" || ev == "azpipelines") {
                    cmd.AddOption(workflowInputsOpt);
                    cmd.AddOption(workflowInputFilesOpt);
                }
            }
            var startserver = new Command("startserver", "Starts a server listening on the supplied address or selects a random free http address.");
            rootCommand.AddCommand(startserver);
            Func<Parameters, Task<int>> sthandler = p => {
                p.StartServer = true;
                return handler(p);
            };
            startserver.SetHandler(sthandler, binder);

            var startrunner = new Command("startrunner", "Configures and runs n runner.");
            rootCommand.AddCommand(startrunner);
            Func<Parameters, Task<int>> thandler = p => {
                p.StartRunner = true;
                return handler(p);
            };
            startrunner.SetHandler(thandler, binder);
            rootCommand.AddOption(tokenOpt);
            foreach(var opt in rootCommand.Options) {
                if(opt.Aliases.Contains("--server") || opt.Aliases.Contains("--verbose") || opt.Aliases.Contains("--parallel") || opt.Aliases.Contains("--privileged") || opt.Aliases.Contains("--userns") || opt.Aliases.Contains("--container-architecture") || opt.Aliases.Contains("--runner-version") || opt.Aliases.Contains("--token")) {
                    startserver.AddOption(opt);
                    startrunner.AddOption(opt);
                }
            }

            rootCommand.SetHandler(handler, binder);

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            List<string> configs = new List<string>();
            configs.Add(Path.Combine(home, ".actrc"));
            var xdgconfighome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if(xdgconfighome != null) {
                configs.Add(Path.Combine(xdgconfighome, ".actrc"));
            } else {
                configs.Add(Path.Combine(home, ".config", ".actrc"));
            }
            configs.Add(Path.Combine(".", ".actrc"));
            List<string> cargs = new List<string>();
            var cfgregex = new Regex("\\s");

            var dummyCommand = new Command("actrc");
            foreach(var opt in rootCommand.Options) {
                dummyCommand.AddOption(opt);
            }
            foreach(var config in configs) {
                try {
                    var content = File.ReadAllLines(config);
                    long n = 0;
                    foreach(var rawline in content) {
                        var line = rawline.Trim();
                        if(line.StartsWith("-")) {
                            var opt = cfgregex.Split(line, 2);
                            var pres = dummyCommand.Parse(opt);
                            if(pres.Errors?.Count > 0) {
                                foreach(var err in pres.Errors) {
                                    Console.Error.WriteLine($"Warning: Failed to parse \"{config}\" line {n}: {err.Message}");
                                }
                            } else {
                                cargs.AddRange(opt);
                            }
                        }
                        n++;
                    }
                } catch {

                }
            }
            cargs.AddRange(args);
            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args.Length == 1 && args[0] == "--version" ? args : cargs.ToArray()).Result;
        }

        private static async Task<(Dictionary<string, Dictionary<string, string>>, Dictionary<string, Dictionary<string, string>>)> ReadSecretsAndVariables(Parameters parameters)
        {
            var envSecrets = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            if(parameters.EnvironmentSecretFiles?.Length > 0) {
                foreach(var opt in parameters.EnvironmentSecretFiles) {
                    var subopt = opt.Split('=', 2);
                    string name = subopt.Length == 2 ? subopt[0] : "";
                    string filename = subopt.Length == 2 ? subopt[1] : subopt[0];
                    var dict = envSecrets[name] = envSecrets.TryGetValue(name, out var v) ? v : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    Util.ReadEnvFile(filename, (key, val) => dict[key] = val);
                }
            }
            if(parameters.EnvironmentSecrets?.Length > 0) {
                for(int i = 0; i < parameters.EnvironmentSecrets.Length; i++) {
                    var opt = parameters.EnvironmentSecrets[i];
                    var subopt = opt.Split('=', 3);
                    string name = subopt[0];
                    string varname = subopt[1];
                    string varval = subopt.Length == 3 ? subopt[2] : null;
                    if(varval == null) {
                        await Console.Out.WriteAsync($"{name}={varname}=");
                        varval = ReadSecret();
                        parameters.EnvironmentSecrets[i] = $"{name}={varname}={varval}";
                    }
                    var dict = envSecrets[name] = envSecrets.TryGetValue(name, out var v) ? v : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    dict[varname] = varval;
                }
            }
            var envVars = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            if(parameters.EnvironmentVarFiles?.Length > 0) {
                foreach(var opt in parameters.EnvironmentVarFiles) {
                    var subopt = opt.Split('=', 2);
                    string name = subopt.Length == 2 ? subopt[0] : "";
                    string filename = subopt.Length == 2 ? subopt[1] : subopt[0];
                    var dict = envVars[name] = envVars.TryGetValue(name, out var v) ? v : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    Util.ReadEnvFile(filename, (key, val) => dict[key] = val);
                }
            }
            if(parameters.VarFiles?.Length > 0) {
                foreach(var opt in parameters.VarFiles) {
                    string name = "";
                    string filename = opt;
                    var dict = envVars[name] = envVars.TryGetValue(name, out var v) ? v : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    Util.ReadEnvFile(filename, (key, val) => dict[key] = val);
                }
            }
            if(parameters.EnvironmentVars?.Length > 0) {
                for(int i = 0; i < parameters.EnvironmentVars.Length; i++) {
                    var opt = parameters.EnvironmentVars[i];
                    var subopt = opt.Split('=', 3);
                    string name = subopt[0];
                    string varname = subopt[1];
                    string varval = subopt.Length == 3 ? subopt[2] : null;
                    if(varval == null) {
                        await Console.Out.WriteAsync($"{name}={varname}=");
                        varval = await Console.In.ReadLineAsync();
                        parameters.EnvironmentVars[i] = $"{name}={varname}={varval}";
                    }
                    var dict = envVars[name] = envVars.TryGetValue(name, out var v) ? v : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    dict[varname] = varval;
                }
            }
            if(parameters.Vars?.Length > 0) {
                for(int i = 0; i < parameters.Vars.Length; i++) {
                    var opt = parameters.Vars[i];
                    var subopt = opt.Split('=', 2);
                    string name = "";
                    string varname = subopt[0];
                    string varval = subopt.Length == 2 ? subopt[1] : null;
                    if(varval == null) {
                        await Console.Out.WriteAsync($"{varname}=");
                        varval = await Console.In.ReadLineAsync();
                        parameters.Vars[i] = $"{varname}={varval}";
                    }
                    var dict = envVars[name] = envVars.TryGetValue(name, out var v) ? v : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    dict[varname] = varval;
                }
            }
            return (envSecrets, envVars);
        }

        private static async Task CollectRepoFiles(string root, string wd, RepoDownload endpoint, MultipartFormDataContent repodownload, List<Stream> streamsToDispose, long level, Parameters parameters, CancellationTokenSource source) {
            List<Func<Task>> submoduleTasks = new List<Func<Task>>();
            EventHandler<ProcessDataReceivedEventArgs> handleoutput = (s, e) => {
                var files = e.Data.Split('\0');
                foreach(var file in files) {
                    if(file == "") break;
                    var modeend = file.IndexOf(' ');
                    var shaend = file.IndexOf(' ', modeend + 1);
                    var filebeg = file.IndexOf('\t') + 1;
                    var filename = file.Substring(filebeg);
                    string mode = "644";
                    if(modeend == 6) {
                        if(file.StartsWith("100")) {
                            mode = file.Substring(3, modeend - 3);
                        } else if(file.StartsWith("160")) {
                            if(endpoint.Submodules && level == 0 || endpoint.NestedSubmodules) {
                                submoduleTasks.Add(() => CollectRepoFiles(root, Path.Combine(wd, filename), endpoint, repodownload, streamsToDispose, level + 1, parameters, source));
                            }
                            continue;
                        } else if(file.StartsWith("120")) {
                            //Symlink
                            submoduleTasks.Add(async () => {
                                GitHub.Runner.Sdk.ProcessInvoker gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
                                string dest = null;
                                gitinvoker.OutputDataReceived += (s, e) => {
                                    dest = e.Data;
                                };
                                var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                                var git = WhichUtil.Which("git", true);
                                var sha = file.Substring(modeend + 1, shaend - (modeend + 1));
                                await gitinvoker.ExecuteAsync(wd, git, $"cat-file -p {sha}", new Dictionary<string, string>(), source.Token);
                                repodownload.Add(new StringContent(dest), "lnk:" + Path.GetRelativePath(root, Path.Combine(wd, filename)).Replace('\\', '/'));
                            });
                            continue;
                            // readlink git cat-file -p sha
                        }
                    }
                    try {
                        var fs = File.OpenRead(Path.Combine(wd, filename));
                        streamsToDispose.Add(fs);
                        filename = Path.GetRelativePath(root, Path.Combine(wd, filename));
                        repodownload.Add(new StreamContent(fs), mode + ":" + filename.Replace('\\', '/'), filename.Replace('\\', '/'));
                    }
                    catch {

                    }
                }
            };
            GitHub.Runner.Sdk.ProcessInvoker gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
            gitinvoker.OutputDataReceived += handleoutput;
            var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var git = WhichUtil.Which("git", true);
            await gitinvoker.ExecuteAsync(wd, git, "ls-files -z -s", new Dictionary<string, string>(), source.Token);
            // collect all submodules
            foreach (var submodule in submoduleTasks) {
                await submodule();
            }
            gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.Verbose));
            gitinvoker.OutputDataReceived += (s, e) => {
                var files = e.Data.Split('\0');
                foreach(var filename in files) {
                    if(filename == "") break;
                    var relpath = Path.Combine(wd, filename);
                    var mode = "644";
                    if(!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                            try {
                                var finfo = new Mono.Unix.UnixSymbolicLinkInfo(relpath);
                                if(finfo.IsSymbolicLink) {
                                    var dest = finfo.ContentsPath;
                                    relpath = Path.GetRelativePath(root, Path.Combine(wd, filename));
                                    repodownload.Add(new StringContent(dest), "lnk:" + relpath);
                                    continue;
                                }
                                if(finfo.FileAccessPermissions.HasFlag(Mono.Unix.FileAccessPermissions.UserExecute)) {
                                    mode = "755";
                                }
                            }
                            catch {

                            }
                        } else {
                            try {
                                if(new FileInfo(relpath).Attributes.HasFlag(FileAttributes.ReparsePoint)){
                                    var dest = ReadSymlinkWindows(relpath);
                                    relpath = Path.GetRelativePath(root, Path.Combine(wd, filename));
                                    repodownload.Add(new StringContent(dest.Replace('\\', '/')), "lnk:" + relpath.Replace('\\', '/'));
                                    continue;
                                }
                            } catch {

                            }
                        }
                    try {
                        var fs = File.OpenRead(relpath);
                        streamsToDispose.Add(fs);
                        relpath = Path.GetRelativePath(root, Path.Combine(wd, filename));
                        repodownload.Add(new StreamContent(fs), mode + ":" + relpath.Replace('\\', '/'), relpath.Replace('\\', '/'));
                    } catch {

                    }
                }
            };
            await gitinvoker.ExecuteAsync(wd, git, "ls-files -z -o --exclude-standard", new Dictionary<string, string>(), source.Token);
            if(!parameters.NoCopyGitDir) {
                // Copy .git dir if it exists (not working with git worktree) https://github.com/ChristopherHX/runner.server/issues/34
                var gitdir = Path.Combine(wd, ".git");
                if(Directory.Exists(gitdir)) {
                    foreach(var w in Directory.EnumerateFiles(gitdir, "*", new EnumerationOptions { RecurseSubdirectories = true, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true })) {
                        var relpath = Path.GetRelativePath(root, w).Replace('\\', '/');
                        var file = File.OpenRead(w);
                        streamsToDispose.Add(file);
                        var mode = "644";
                        if(!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                            try {
                                var finfo = new Mono.Unix.UnixSymbolicLinkInfo(w);
                                if(finfo.IsSymbolicLink) {
                                    var dest = finfo.ContentsPath;
                                    repodownload.Add(new StringContent(dest), "lnk:" + relpath);
                                    continue;
                                }
                                if(finfo.FileAccessPermissions.HasFlag(Mono.Unix.FileAccessPermissions.UserExecute)) {
                                    mode = "755";
                                }
                            }
                            catch {

                            }
                        } else {
                            try {
                                if(new FileInfo(w).Attributes.HasFlag(FileAttributes.ReparsePoint)){
                                    var dest = ReadSymlinkWindows(w);
                                    repodownload.Add(new StringContent(dest.Replace('\\', '/')), "lnk:" + relpath);
                                    continue;
                                }
                            } catch {

                            }
                        }
                        repodownload.Add(new StreamContent(file), mode + ":" + relpath, relpath);
                    }
                }
            }
        }

        private static string ReadSymlinkWindows(string w) {
            try {
                var targetPath = NativeMethods.ReadSymlink(w);
                var prefix = "\\??\\";
                if(targetPath.StartsWith(prefix)) {
                    return Path.GetRelativePath(prefix + Path.GetDirectoryName(w), targetPath);
                }
                return targetPath;
            } catch {
                var finalPath = NativeMethods.GetFinalPathName(w);
                var relativeTo = Path.GetDirectoryName(w);
                var prefix = "\\\\?\\";
                if(finalPath.StartsWith(prefix)) {
                    relativeTo = prefix + relativeTo;
                }
                return Path.GetRelativePath(relativeTo, finalPath);
            }
        }

        public static class NativeMethods
        {
            private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            private const uint FILE_READ_EA = 0x0008;
            private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;

            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            static extern uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool CloseHandle(IntPtr hObject);

            public static Int32 FSCTL_GET_REPARSE_POINT = ( ((0x00000009) << 16) | ((0) << 14) | ((42) << 2) | (0) );
            public static uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;

            [StructLayout(LayoutKind.Sequential)]
            class REPARSE_DATA_BUFFER {
                public uint ReparseTag;
                public ushort ReparseDataLength;
                public ushort Reserved;
                public ushort SubstituteNameOffset;
                public ushort SubstituteNameLength;
                public ushort PrintNameOffset;
                public ushort PrintNameLength;
                public uint Flags;

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
                public byte[] PathBuffer;
            }


            [DllImport("kernel32.dll")]
            public static extern byte DeviceIoControl(IntPtr hDevice, Int32 dwIoControlCode, IntPtr lpInBuffer, Int32 nInBufferSize, IntPtr lpOutBuffer, Int32 nOutBufferSize, ref Int32 lpBytesReturned, IntPtr lpOverlapped);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr CreateFile(
                    [MarshalAs(UnmanagedType.LPTStr)] string filename,
                    [MarshalAs(UnmanagedType.U4)] uint access,
                    [MarshalAs(UnmanagedType.U4)] FileShare share,
                    IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
                    [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                    [MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
                    IntPtr templateFile);

            public static string GetFinalPathName(string path)
            {
                var h = CreateFile(path, 
                    FILE_READ_EA, 
                    FileShare.ReadWrite | FileShare.Delete, 
                    IntPtr.Zero, 
                    FileMode.Open, 
                    FILE_FLAG_BACKUP_SEMANTICS,
                    IntPtr.Zero);
                if (h == INVALID_HANDLE_VALUE)
                    throw new Win32Exception();

                try
                {
                    var sb = new StringBuilder(1024);
                    var res = GetFinalPathNameByHandle(h, sb, 1024, 0);
                    if (res == 0)
                        throw new Win32Exception();

                    return sb.ToString();
                }
                finally
                {
                    CloseHandle(h);
                }
            }

            private static uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;

            public static string ReadSymlink(string path)
            {
                var h = CreateFile(path, 
                    FILE_READ_EA, 
                    FileShare.ReadWrite | FileShare.Delete, 
                    IntPtr.Zero, 
                    FileMode.Open, 
                    FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                    IntPtr.Zero);
                if (h == INVALID_HANDLE_VALUE)
                    throw new Win32Exception();

                try
                {
                    REPARSE_DATA_BUFFER rdb = new REPARSE_DATA_BUFFER();
                    var buf = Marshal.AllocHGlobal(Marshal.SizeOf(rdb));
                    int size = 0;
                    var res = DeviceIoControl(h, FSCTL_GET_REPARSE_POINT, IntPtr.Zero, 0, buf, Marshal.SizeOf(rdb), ref size, IntPtr.Zero);
                    if (res == 0)
                        throw new Win32Exception();
                    Marshal.PtrToStructure<REPARSE_DATA_BUFFER>(buf, rdb);
                    if(rdb.ReparseTag != IO_REPARSE_TAG_SYMLINK) {
                        throw new Exception("Invalid reparse point, only symlinks are supported");
                    }
                    //var sres = Encoding.Unicode.GetString(rdb.PathBuffer, rdb.PrintNameOffset, rdb.PrintNameLength);
                    var sres2 = Encoding.Unicode.GetString(rdb.PathBuffer, rdb.SubstituteNameOffset, rdb.SubstituteNameLength);
                    return sres2;
                }
                finally
                {
                    CloseHandle(h);
                }
            }
        }
    }
}
