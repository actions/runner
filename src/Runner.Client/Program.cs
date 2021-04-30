using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Net.Http;
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

namespace Runner.Client
{
    class Program
    {

        private class JobListItem {
            public string Name {get;set;}
            public string[] Needs {get;set;}
        }

        private class HookResponse {
            public string repo {get;set;}
            public long run_id {get;set;}
            public bool skipped {get;set;}
            public bool failed {get;set;}
            public List<JobListItem> jobList {get;set;}
        }

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

        private class TraceWriter : GitHub.Runner.Sdk.ITraceWriter
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

        private class Parameters {
            public string[] workflow { get; set; }
            public string server { get; set; }
            public string payload { get; set; }

            public string eventpath { get => payload; set => payload = value; }
            public string Event { get; set; }
            public string[] env { get; set; }
            public string envFile { get; set; }
            public string[] secret { get; set; }
            public string secretFile { get; set; }
            public string job { get; set; }
            public string[] matrix { get; set; }
            public bool list { get; set; }
            public string workflows { get; set; }
            public string[] platform { get; set; }
            public string actor { get; set; }
            public bool watch { get; set; }
            public bool quiet { get; set; }
            public bool privileged { get; set; }
            public string userns { get; set; }
            public string containerArchitecture { get; set; }
            public string defaultbranch { get; set; }
            public string directory { get; set; }
            public bool verbose { get; set; }
            public int parallel { get; set; }
            public bool StartServer { get; set; }
            public bool StartRunner { get; set; }
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
        }

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
        static int Main(string[] args)
        {
            if(System.OperatingSystem.IsWindowsVersionAtLeast(10)) {
                WindowsUtils.EnableVT();
            }
            
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
            };

            var secretOpt = new Option<string>(
                new[] { "-s", "--secret" },
                description: "secret for you workflow, overrides keys from the secrets file");
            secretOpt.Argument.Arity = new ArgumentArity(0, ArgumentArity.MaximumArity);
            var envOpt = new Option<string>(
                new[] { "--env" },
                description: "env for you workflow, overrides keys from the env file");
            envOpt.Argument.Arity = new ArgumentArity(0, ArgumentArity.MaximumArity);
            var matrixOpt = new Option<string>(
                new[] { "-m", "--matrix" },
                description: "matrix filter e.g. '-m Key:value', use together with '--job'. Use multiple times to filter more jobs");
            matrixOpt.Argument.Arity = new ArgumentArity(0, ArgumentArity.MaximumArity);
            
            var workflowOption = new Option<string>(
                "--workflow",
                description: "Workflow to run");
            workflowOption.Argument.Arity = new ArgumentArity(1, ArgumentArity.MaximumArity);

            var platformOption = new Option<string>(
                new[] { "-P", "--platform" },
                description: "Platform mapping to run in a docker image or host");
            platformOption.Argument.Arity = new ArgumentArity(0, ArgumentArity.MaximumArity);
            var rootCommand = new RootCommand
            {
                workflowOption,
                new Option<string>(
                    "--server",
                    description: "Runner.Server address"),
                new Option<string>(
                    new[] { "-e", "--payload", "--eventpath" },
                    "Webhook payload to send to the Runner"),
                new Option<string>(
                    "--event",
                    getDefaultValue: () => "push",
                    description: "Which event to send to a worker"),
                envOpt,
                new Option<string>(
                    "--env-file",
                    getDefaultValue: () => ".env",
                    description: "env overrides for you workflow"),
                secretOpt,
                new Option<string>(
                    "--secret-file",
                    getDefaultValue: () => ".secrets",
                    description: "secrets for you workflow"),
                new Option<string>(
                    new[] {"-j", "--job"},
                    description: "job to run"),
                matrixOpt,
                new Option<bool>(
                    new[] { "-l", "--list"},
                    getDefaultValue: () => false,
                    description: "list jobs for the selected event"),
                new Option<string>(
                    new[] { "-W", "--workflows"},
                    getDefaultValue: () => ".github/workflows",
                    description: "workflow file or directory which contains workflows, only used if no --workflow option is set"),
                platformOption,
                new Option<string>(
                    new[] {"-a" , "--actor"},
                    "The login of the user that initiated the workflow run"),
                new Option<bool>(
                    new[] {"-w", "--watch"},
                    "Run automatically on every file change"),
                new Option<bool>(
                    new[] {"-q", "--quiet"},
                    "Display no progress in the cli"),
                new Option<bool>(
                    "--privileged",
                    "Run docker container under privileged mode"),
                new Option<string>(
                    "--userns",
                    "Run docker container under a specfic linux user namespace"),
                new Option<string>(
                    "--container-architecture",
                    "Run docker container architecture, if docker supports it"),
                new Option<string>(
                    "--defaultbranch",
                    description: "The default branch of your workflow run"),
                new Option<string>(
                    new[] {"-C", "--directory"},
                    "change the working directory before running"),
                new Option<bool>(
                    new[] {"-v", "--verbose"},
                    "Run automatically on every file change"),
                new Option<int>(
                    "--parallel",
                    getDefaultValue: () => 4,
                    description: "Run n parallel runners, ignored if --server is used"),
            };

            rootCommand.Description = "Send events to your runner";

            // Note that the parameters of the handler method are matched according to the names of the options
            Func<Parameters, Task<int>> handler = async (parameters) =>
            {
                if(parameters.list) {
                    parameters.parallel = 0;
                }
                if(parameters.actor == null) {
                    parameters.actor = "runnerclient";
                }
                ConcurrentQueue<string> added = new ConcurrentQueue<string>();
                ConcurrentQueue<string> changed = new ConcurrentQueue<string>();
                ConcurrentQueue<string> removed = new ConcurrentQueue<string>();
                CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken token = source.Token;
                Console.CancelKeyPress += (s, e) => {
                    e.Cancel = !source.IsCancellationRequested;
                    source.Cancel();
                };
                List<Task> listener = new List<Task>();
                try {
                    if(parameters.server == null || parameters.StartServer || parameters.StartRunner) {
                        var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        EventHandler<ProcessDataReceivedEventArgs> _out = (s, e) => {
                            Console.WriteLine(e.Data);
                        };
                        if(parameters.StartRunner) {
                            if(parameters.server == null) {
                                parameters.server = "http://localhost:5000";
                            }
                        } else {
                            if(parameters.server == null) {
                                foreach(var ip in Dns.GetHostAddresses(Dns.GetHostName())) {
                                    if(!IPAddress.IsLoopback(ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                                        var builder = new UriBuilder();
                                        builder.Host = ip.ToString();
                                        builder.Scheme = "http";
                                        builder.Port = 0;
                                        parameters.server = builder.Uri.ToString().Trim('/');
                                        break;
                                    }
                                }
                                if(parameters.server == null) {
                                    parameters.server = "http://localhost:0";
                                }
                            }
                            GitHub.Runner.Sdk.ProcessInvoker invoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                            if(parameters.verbose) {
                                invoker.OutputDataReceived += _out;
                                invoker.ErrorDataReceived += _out;
                            }
                            var server = Path.Join(binpath, $"Runner.Server{IOUtil.ExeExtension}");
                            string serverconfigfileName = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
                            JObject serverconfig = new JObject();
                            var connectionopts = new JObject();
                            connectionopts["sqlite"] = /* "Data Source=Agents.db;"; */"Data Source=:memory:;";
                            serverconfig["ConnectionStrings"] = connectionopts;
                            
                            serverconfig["Kestrel"] = JObject.FromObject(new { Endpoints = new { Http = new { Url = parameters.server } } });
                            serverconfig["Runner.Server"] = JObject.FromObject(new { 
                                GitServerUrl = "https://github.com",
                                GitApiServerUrl = "https://api.github.com",
                                GitGraphQlServerUrl = "https://api.github.com/graphql",
                            });
                            try {
                                JObject orgserverconfig = JObject.Parse(await File.ReadAllTextAsync(Path.Join(binpath, "appconfig.json"), Encoding.UTF8));
                                orgserverconfig.Merge(serverconfig, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                                serverconfig = orgserverconfig;
                            } catch {

                            }
                            await File.WriteAllTextAsync(serverconfigfileName, serverconfig.ToString());
                            using (AnonymousPipeServerStream pipeServer =
                                new AnonymousPipeServerStream(PipeDirection.In,
                                HandleInheritability.Inheritable))
                            {
                                var servertask = invoker.ExecuteAsync(binpath, server, "", new Dictionary<string, string>() { {"RUNNER_SERVER_APP_JSON_SETTINGS_FILE", serverconfigfileName }, { "RUNNER_CLIENT_PIPE", pipeServer.GetClientHandleAsString() }}, false, null, true, token).ContinueWith(x => {
                                    Console.WriteLine("Stopped Server");
                                    File.Delete(serverconfigfileName);
                                });
                                listener.Add(servertask);
                                using (StreamReader rd = new StreamReader(pipeServer))
                                {
                                    var line = rd.ReadLine();
                                    parameters.server = line;
                                }
                            }
                        }

                        var workerchannel = Channel.CreateBounded<bool>(1);
                        // Parallel.For(0, parameters.parallel, () => {});
                        for(int i = 0; i < parameters.parallel; i++) {
                            var runner = Path.Join(binpath, $"Runner.Listener{IOUtil.ExeExtension}");
                            string tmpdir = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
                            Directory.CreateDirectory(tmpdir);
                            int atempt = 1;
                            while(true) {
                                try {
                                    var inv = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                    if(parameters.verbose) {
                                        inv.OutputDataReceived += _out;
                                        inv.ErrorDataReceived += _out;
                                    }

                                    var runnerEnv = new Dictionary<string, string>() { {"RUNNER_SERVER_CONFIG_ROOT", tmpdir }, { "RUNNER_TOOL_CACHE", Environment.GetEnvironmentVariable("RUNNER_TOOL_CACHE") ?? Path.Combine(new DirectoryInfo(binpath).Parent.FullName, "_tool_cache") }};
                                    if(parameters.containerArchitecture != null) {
                                        runnerEnv["RUNNER_CONTAINER_ARCH"] = parameters.containerArchitecture;
                                    }
                                    if(parameters.privileged) {
                                        runnerEnv["RUNNER_CONTAINER_PRIVILEGED"] = "1";
                                    }
                                    if(parameters.userns != null) {
                                        runnerEnv["RUNNER_CONTAINER_USERNS"] = parameters.userns;
                                    }
                                    
                                    // Agent-{Guid.NewGuid().ToString()}
                                    var code = await inv.ExecuteAsync(binpath, runner, $"Configure --name Agent{i} --unattended --url {parameters.server}/runner/server --token empty --labels container-host", runnerEnv, true, null, true, token);
                                    var runnerlistener = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                    if(parameters.verbose) {
                                        runnerlistener.OutputDataReceived += _out;
                                        runnerlistener.ErrorDataReceived += _out;
                                    }
                                    runnerlistener.OutputDataReceived += (s, e) => {
                                        if(e.Data.Contains("Listen")) {
                                            workerchannel.Writer.WriteAsync(true);
                                        }
                                    };
                                    listener.Add(runnerlistener.ExecuteAsync(binpath, runner, $"Run", runnerEnv, false, null, true, token).ContinueWith(async x => {
                                        Console.WriteLine("Stopped Worker");
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
                                    }));
                                    break;
                                } catch {
                                    if(atempt++ <= 3) {
                                        await Task.Delay(500);
                                    } else {
                                        Console.Error.WriteLine("Failed to auto-configure actions runners after 3 attempts");
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
                        if(parameters.parallel > 0) {
                            await workerchannel.Reader.ReadAsync();
                        }

                        if(parameters.StartServer || parameters.StartRunner) {
                            if(parameters.StartServer) {
                                Console.WriteLine($"The server is listening on {parameters.server}");
                            }
                            Console.WriteLine($"Press any key or CTRL+C to stop the {(parameters.StartServer ? "server" : "runners")}");

                            try {
                                Task.Run(() => {
                                    Console.In.ReadLine();
                                    source.Cancel();
                                });
                                await Task.Delay(-1, token);
                            } catch {

                            }
                            return 0;
                        }
                    }
                    bool first = true;
                    while(!source.IsCancellationRequested && (parameters.watch || first)) {
                        var ret = await Task.Run<int>(async () => {
                            List<string> addedFiles = new List<string>();
                            List<string> changedFiles = new List<string>();
                            List<string> removedFiles = new List<string>();
                            if(!first) {
                                using(FileSystemWatcher watcher = new FileSystemWatcher(parameters.directory ?? ".") {IncludeSubdirectories = true}) {
                                    watcher.Created += (s, f) => {
                                        var path = Path.GetRelativePath(parameters.directory ?? ".", f.FullPath);
                                        Console.WriteLine($"Added {path}");
                                        added.Enqueue(path);
                                    };
                                    watcher.Deleted += (s, f) => {
                                        var path = Path.GetRelativePath(parameters.directory ?? ".", f.FullPath);
                                        Console.WriteLine($"Removed {path}");
                                        removed.Enqueue(path);
                                    };
                                    watcher.Changed += (s, f) => {
                                        var path = Path.GetRelativePath(parameters.directory ?? ".", f.FullPath);
                                        Console.WriteLine($"Changed {path}");
                                        changed.Enqueue(path);
                                    };
                                    watcher.Renamed += (s, f) => {
                                        var path = Path.GetRelativePath(parameters.directory ?? ".", f.FullPath);
                                        var oldpath = Path.GetRelativePath(parameters.directory ?? ".", f.OldFullPath);
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
                                        } while(!(added.TryDequeue(out addedFile) && !await IsIgnored(parameters.directory ?? ".", addedFile)) && !(changed.TryDequeue(out changedFile) && !await IsIgnored(parameters.directory ?? ".", changedFile)) && !(removed.TryDequeue(out removedFile) && !await IsIgnored(parameters.directory ?? ".", removedFile)));
                                    } catch(TaskCanceledException) {

                                    }
                                    while(addedFile != null || added.TryDequeue(out addedFile)) {
                                        if(!await IsIgnored(parameters.directory ?? ".", addedFile)) {
                                            addedFiles.Add(addedFile);
                                        }
                                        addedFile = null;
                                    }
                                    while(changedFile != null || changed.TryDequeue(out changedFile)) {
                                        if(!await IsIgnored(parameters.directory ?? ".", changedFile)) {
                                            changedFiles.Add(changedFile);
                                        }
                                        changedFile = null;
                                    }
                                    while(removedFile != null || removed.TryDequeue(out removedFile)) {
                                        if(!await IsIgnored(parameters.directory ?? ".", removedFile)) {
                                            removedFiles.Add(removedFile);
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
                            var workflows = parameters.workflow;
                            if(workflows == null || workflows.Length == 0) {
                                if(Directory.Exists(parameters.workflows)) {
                                    try {
                                        workflows = Directory.GetFiles(parameters.workflows, "*.y?ml", new EnumerationOptions { RecurseSubdirectories = false, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true }).Where(f => f.EndsWith(".yml") || f.EndsWith(".yaml")).ToArray();
                                        if((workflows == null || workflows.Length == 0)) {
                                            Console.Error.WriteLine($"No workflow *.yml file found inside of {parameters.workflows}");
                                            return 1;
                                        }   
                                    } catch {
                                        Console.Error.WriteLine($"Failed to read directory {parameters.workflows}");
                                        return 1;
                                    }
                                } else if (File.Exists(parameters.workflows)) {
                                    workflows = new[] { parameters.workflows };
                                } else {
                                    Console.Error.WriteLine($"No such file or directory {parameters.workflows}");
                                    return 1;
                                }
                            }
                            try {
                                var client = new HttpClient();
                                client.DefaultRequestHeaders.Add("X-GitHub-Event", parameters.Event);
                                var b = new UriBuilder(parameters.server);
                                var query = new QueryBuilder();
                                b.Path = "runner/host/_apis/v1/Message/schedule";
                                var mp = new MultipartFormDataContent();
                                List<Stream> workflowsToDispose = new List<Stream>();
                                HttpResponseMessage resp = null;
                                try {
                                    foreach(var w in workflows) {
                                        try {
                                            var workflow = File.OpenRead(w);
                                            workflowsToDispose.Add(workflow);
                                            mp.Add(new StreamContent(workflow), w, w);
                                        } catch {
                                            Console.WriteLine($"Failed to read file: {w}");
                                            return 1;
                                        }
                                    }
                                    
                                    List<string> wenv = new List<string>();
                                    List<string> wsecrets = new List<string>();
                                    try {
                                        wenv.AddRange(await File.ReadAllLinesAsync(parameters.envFile, Encoding.UTF8));
                                    } catch {
                                        if(parameters.envFile != ".env") {
                                            Console.WriteLine($"Failed to read file: {parameters.envFile}");
                                        }
                                    }
                                    try {
                                        wsecrets.AddRange(await File.ReadAllLinesAsync(parameters.secretFile, Encoding.UTF8));
                                    } catch {
                                        if(parameters.secretFile != ".secrets") {
                                            Console.WriteLine($"Failed to read file: {parameters.secretFile}");
                                        }
                                    }
                                    if(parameters.job != null) {
                                        query.Add("job", parameters.job);
                                    }
                                    if(parameters.matrix?.Length > 0) {
                                        if(parameters.job == null) {
                                            Console.WriteLine("--matrix is only supported together with --job");
                                            return 1;
                                        }
                                        query.Add("matrix", parameters.matrix);
                                    }
                                    if(parameters.list) {
                                        query.Add("list", "1");
                                    }
                                    if(parameters.platform?.Length > 0) {
                                        query.Add("platform", parameters.platform);
                                    }
                                    if(parameters.env?.Length > 0) {
                                        foreach (var e in parameters.env) {
                                            if(e.IndexOf('=') > 0) {
                                                wenv.Add(e);
                                            } else {
                                                var envvar = Environment.GetEnvironmentVariable(e);
                                                if(envvar == null) {
                                                    await Console.Out.WriteAsync($"{e}=");
                                                    envvar = await Console.In.ReadLineAsync();
                                                }
                                                wenv.Add($"{e}={envvar}");
                                            }
                                        }
                                    }
                                    if(parameters.secret?.Length > 0) {
                                        foreach (var e in parameters.secret) {
                                            if(e.IndexOf('=') > 0) {
                                                wsecrets.Add(e);
                                            } else {
                                                var envvar = Environment.GetEnvironmentVariable(e);
                                                if(envvar == null) {
                                                    await Console.Out.WriteAsync($"{e}=");
                                                    envvar = ReadSecret();
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
                                    b.Query = query.ToQueryString().ToString().TrimStart('?');
                                    JObject payloadContent = new JObject();
                                    var acommits = new JArray();
                                    payloadContent["commits"] = acommits;
                                    var sha = "4544205a385319fe846d5df4ed2e3b8173569d78";
                                    var bf = "0000000000000000000000000000000000000000";
                                    var user = JObject.FromObject(new { login = parameters.actor, name = parameters.actor, email = $"{parameters.actor}@runner.server.localhost", id = 976638, type = "user" });
                                    var commit = JObject.FromObject(new { message = "Untraced changes", id = sha, added = addedFiles, removed = removedFiles, modified = changedFiles });
                                    acommits.AddFirst(commit);
                                    payloadContent["head_commit"] = commit;
                                    payloadContent["sender"] = user;
                                    payloadContent["pusher"] = user;
                                    var repoowner = user;
                                    payloadContent["before"] = bf;
                                    payloadContent["after"] = sha;
                                    string reponame = "Unknown";
                                    string repofullname = "Unknown/Unknown";
                                    try {
                                        string line = null;
                                        EventHandler<ProcessDataReceivedEventArgs> handleoutput = (s, e) => {
                                            if(line == null) {
                                                line = e.Data;
                                            }
                                        };
                                        GitHub.Runner.Sdk.ProcessInvoker gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                        gitinvoker.OutputDataReceived += handleoutput;
                                        var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                                        var git = WhichUtil.Which("git", true);
                                        await gitinvoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, "tag --points-at HEAD", new Dictionary<string, string>(), source.Token);
                                        if(line != null) {
                                            payloadContent["ref"] = "refs/tags/" + line;
                                        }
                                        gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                        gitinvoker.OutputDataReceived += handleoutput;
                                        await gitinvoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, "symbolic-ref HEAD", new Dictionary<string, string>(), source.Token);
                                        if(line != null) {
                                            var _ref = line;
                                            if(!payloadContent.ContainsKey("ref")) {
                                                payloadContent["ref"] = _ref;
                                            }
                                            line = null;
                                            gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                            gitinvoker.OutputDataReceived += handleoutput;
                                            await gitinvoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, $"for-each-ref --format='%(upstream:short)' {_ref}", new Dictionary<string, string>(), source.Token);
                                            if(line != null && line != "") {
                                                var remote = line.Substring(0, line.IndexOf('/'));
                                                if(parameters.defaultbranch == null) {
                                                    line = null;
                                                    gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                                    gitinvoker.OutputDataReceived += handleoutput;
                                                    await gitinvoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, $"symbolic-ref refs/remotes/{remote}/HEAD", new Dictionary<string, string>(), source.Token);
                                                    if(line != null && line.StartsWith($"refs/remotes/{remote}/")) {
                                                        var defbranch = line.Substring($"refs/remotes/{remote}/".Length);
                                                        parameters.defaultbranch = defbranch;
                                                    }
                                                }
                                                line = null;
                                                gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                                gitinvoker.OutputDataReceived += handleoutput;
                                                await gitinvoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, $"remote get-url {remote}", new Dictionary<string, string>(), source.Token);
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
                                                        reponame = repo;
                                                    }
                                                }
                                            }
                                        } else {
                                            await Console.Error.WriteLineAsync("No default github.ref found");
                                        }
                                        line = null;
                                        gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                        gitinvoker.OutputDataReceived += handleoutput;
                                        await gitinvoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, "rev-parse HEAD", new Dictionary<string, string>(), source.Token);
                                        if(line != null) {
                                            payloadContent["after"] = line;
                                            line = null;
                                        } else {
                                            await Console.Error.WriteLineAsync("Couldn't retrive github.sha");
                                        }

                                    } catch {
                                        await Console.Error.WriteLineAsync("Failed to detect git repo the github context may have invalid values");
                                    }

                                    var repository = JObject.FromObject(new { owner = repoowner, default_branch = parameters.defaultbranch ?? "main", master_branch = parameters.defaultbranch ?? "master", name = reponame, full_name = repofullname });
                                    payloadContent["repository"] = repository;
                                    
                                    if(parameters.payload != null) {
                                        try {
                                            // 
                                            var filec = await File.ReadAllTextAsync(parameters.payload, Encoding.UTF8);
                                            var obj = JObject.Parse(filec);

                                            payloadContent.Merge(obj, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                                            // var commits = obj.GetValue("commits");
                                            // JArray acommits = null;
                                            // if(commits is JArray a) {
                                            //     acommits = a;
                                            // } else {
                                            //     acommits = new JArray();
                                            //     obj["commits"] = acommits;
                                            // }
                                            // var sha = "4544205a385319fe846d5df4ed2e3b8173569d78";
                                            // var bf = "0000000000000000000000000000000000000000";
                                            // var user = JObject.FromObject(new { login = parameters.actor, name = parameters.actor, email = $"{parameters.actor}@runner.server.localhost", id = 976638, type = "user" });
                                            // var commit = JObject.FromObject(new { message = "Untraced changes", id = sha });
                                            // var repository = JObject.FromObject(new { owner = user, default_branch = parameters.defaultbranch ?? "main", master_branch = parameters.defaultbranch ?? "master", name = "repo", full_name = "local/repo" });
                                            // acommits.AddFirst(commit);
                                            // obj["head_commit"] = commit;
                                            // JToken val;
                                            // if(!obj.TryGetValue("sender", out val)) {
                                            //     obj["sender"] = user;
                                            // }
                                            // if(!obj.TryGetValue("pusher", out val)) {
                                            //     obj["pusher"] = user;
                                            // }
                                            // if(!obj.TryGetValue("repository", out val)) {
                                            //     obj["repository"] = user;
                                            // }
                                            // if(!obj.TryGetValue("before", out val)) {
                                            //     obj["before"] = bf;
                                            // }
                                            // if(!obj.TryGetValue("after", out val)) {
                                            //     obj["after"] = sha;
                                            // }
                                            
                                        } catch {
                                            Console.WriteLine($"Failed to read file: {parameters.payload}");
                                            return 1;
                                        }
                                    }
                                    mp.Add(new StringContent(payloadContent.ToString()), "event", "event.json");

                                    // GitHub.Runner.Sdk.ProcessInvoker invoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                    // string stashcommitRef = null;
                                    // EventHandler<ProcessDataReceivedEventArgs> createStash = (s, e) => {
                                    //     stashcommitRef = e.Data;
                                    // };
                                    // invoker.OutputDataReceived += createStash;
                                    // var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                                    // var git = WhichUtil.Which("git", true);
                                    // await invoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, "stash create", new Dictionary<string, string>(), CancellationToken.None);
                                    // invoker.OutputDataReceived -= createStash;

                                    // await invoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, $"stash store {stashcommitRef} -m Runner", new Dictionary<string, string>(), CancellationToken.None);
                                    resp = await client.PostAsync(b.Uri.ToString(), mp);
                                } finally {
                                    foreach(var fstream in workflowsToDispose) {
                                        await fstream.DisposeAsync();
                                    }
                                }
                                var s = await resp.Content.ReadAsStringAsync();
                                var hr = JsonConvert.DeserializeObject<HookResponse[]>(s);
                                if(hr.All(h => h.skipped)) {
                                    Console.WriteLine("All workflow were skipped, due to filters");
                                    return  0;
                                }
                                if(parameters.list) {
                                    bool ok = false;
                                    for(int workflowNo = 0; workflowNo < hr.Length; workflowNo++) {
                                        if(hr[workflowNo].jobList != null) {
                                            Console.WriteLine($"Found {hr[workflowNo].jobList.Count} matched Job(s) in {(workflows?.Length > workflowNo ? workflows[workflowNo] : ("workflow " + workflowNo))}");
                                            foreach(var j in hr[workflowNo].jobList) {
                                                Console.WriteLine(j.Name);
                                            }
                                            ok = true;
                                        }
                                    }
                                    if(ok) {
                                        return 0;
                                    } else {
                                        Console.WriteLine("Failed to enumerate jobs");
                                        return 1;
                                    }
                                }

                                var b2 = new UriBuilder(b.ToString());
                                query = new QueryBuilder();
                                query.Add("repo", hr.First().repo);
                                query.Add("runid", hr.Select(h => h.run_id.ToString()));
                                b2.Query = query.ToString().TrimStart('?');
                                b2.Path = "runner/host/_apis/v1/Message";
                                var sr = await client.GetStringAsync(b2.ToString());
                                List<Job> jobs = JsonConvert.DeserializeObject<List<Job>>(sr);
                                Dictionary<Guid, TimeLineEntry> timelineRecords = new Dictionary<Guid, TimeLineEntry>();
                                int col = 0;
                                bool hasErrors = false;
                                foreach(Job j in jobs) {
                                    Console.WriteLine($"Running Job: {j.name}");
                                    if(j.errors?.Count > 0) {
                                        hasErrors = true;
                                        foreach (var error in j.errors) {
                                            Console.Error.WriteLine($"Error: {error}");
                                        }
                                    } else if(j.Cancelled) {
                                        hasErrors = true;
                                        Console.Error.WriteLine($"Error: Cancelled");
                                    }
                                    else if(j.TimeLineId != Guid.Empty) {
                                        try {
                                            var content = await client.GetStringAsync(parameters.server + $"/runner/server/_apis/v1/Timeline/{j.TimeLineId.ToString()}");
                                            timelineRecords[j.TimeLineId] = new TimeLineEntry() { TimeLine = JsonConvert.DeserializeObject<List<TimelineRecord>>(content), Color = (ConsoleColor) col + 1, Pending = new List<WebConsoleEvent>() };
                                            col = (col + 1) % 14;
                                        }
                                        catch (HttpRequestException) {
                                        }
                                    }
                                }

                                var timeLineWebConsoleLog = new UriBuilder(parameters.server + "/runner/server/_apis/v1/TimeLineWebConsoleLog");
                                var timeLineWebConsoleLogQuery = new QueryBuilder();
                                timeLineWebConsoleLogQuery.Add("runid", hr.Select(h => h.run_id.ToString()));
                                var pendingWorkflows = new List<long>();
                                timeLineWebConsoleLog.Query = timeLineWebConsoleLogQuery.ToString().TrimStart('?');
                                var eventstream = await client.GetStreamAsync(timeLineWebConsoleLog.ToString());
                                foreach(var h in hr) {
                                    if(!h.skipped && !h.failed) {
                                        var workflowstat = new UriBuilder(b.ToString());
                                        workflowstat.Path = "runner/host/_apis/v1/Message/WorkflowStatus/" + h.run_id;
                                        var workflowres = await client.GetStringAsync(workflowstat.ToString());
                                        if(workflowres == "") {
                                            pendingWorkflows.Add(h.run_id);
                                        }
                                        else {
                                            var _workflow = JsonConvert.DeserializeObject<WorkflowEventArgs>(workflowres);
                                            Console.WriteLine($"Workflow {_workflow.runid} finished with status {(_workflow.Success ? "Success" : "Failure")}");
                                            if(!_workflow.Success) {
                                                hasErrors = true;
                                            }
                                        }
                                    }
                                }
                                if(pendingWorkflows.Count == 0) {
                                    if(hasErrors) {
                                        Console.WriteLine("All Workflows finished, at least one workflow failed");
                                    } else {
                                        Console.WriteLine("All Workflows finished successfully");
                                    }
                                    return hasErrors ? 1 : 0;
                                }
                                using(TextReader reader = new StreamReader(eventstream)) {
                                    while(!source.IsCancellationRequested) {
                                        var line = await reader.ReadLineAsync();
                                        if(line == null) {
                                            break;
                                        }
                                        var data = await reader.ReadLineAsync();
                                        data = data.Substring("data: ".Length);
                                        await reader.ReadLineAsync();
                                        if(!parameters.quiet && line == "event: log") {
                                            var e = JsonConvert.DeserializeObject<WebConsoleEvent>(data);
                                            TimeLineEntry rec;
                                            if(!timelineRecords.TryGetValue(e.timelineId, out rec)) {
                                                timelineRecords[e.timelineId] = new TimeLineEntry() { Color = (ConsoleColor) col + 1, Pending = new List<WebConsoleEvent>() { e } };
                                                col = (col + 1) % 14;
                                                continue;
                                            } else if(rec.RecordId != e.record.StepId) {
                                                if(rec.RecordId != Guid.Empty && rec.TimeLine != null) {
                                                    var record = rec.TimeLine.Find(r => r.Id == rec.RecordId);
                                                    if(record == null || !record.Result.HasValue) {
                                                        rec.Pending.Add(e);
                                                        continue;
                                                    }
                                                    Console.WriteLine($"\x1b[{(int)rec.Color + 30}m[{rec.TimeLine[0].Name}] \x1b[0m{record.Result.Value.ToString()}: {record.Name}");
                                                }
                                                rec.RecordId = e.record.StepId;
                                                if(rec.TimeLine != null) {
                                                    var record = rec.TimeLine.Find(r => r.Id == e.record.StepId);
                                                    if(record == null) {
                                                        rec.Pending.Add(e);
                                                        rec.RecordId = Guid.Empty;
                                                        continue;
                                                    }
                                                    Console.WriteLine($"\x1b[{(int)rec.Color + 30}m[{rec.TimeLine[0].Name}] \x1b[0mRunning: {record.Name}");
                                                }
                                            }
                                            var old = Console.ForegroundColor;
                                            Console.ForegroundColor = rec.Color;
                                            foreach (var webconsoleline in e.record.Value) {
                                                if(webconsoleline.StartsWith("##[section]")) {
                                                    Console.WriteLine("******************************************************************************");
                                                    Console.WriteLine(webconsoleline.Substring("##[section]".Length));
                                                    Console.WriteLine("******************************************************************************");
                                                } else {
                                                    Console.WriteLine($"\x1b[{(int)rec.Color + 30}m|\x1b[0m {webconsoleline}");
                                                }
                                            }
                                            Console.ForegroundColor = old;
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
                                                    if(timelineRecords[e.timelineId].RecordId != Guid.Empty && timelineRecords[e.timelineId].TimeLine != null) {
                                                        var record = timelineRecords[e.timelineId].TimeLine.Find(r => r.Id == timelineRecords[e.timelineId].RecordId);
                                                        if(record == null || !record.Result.HasValue) {
                                                            break;
                                                        }
                                                        Console.WriteLine($"\x1b[{(int)timelineRecords[e.timelineId].Color + 30}m[{timelineRecords[e.timelineId].TimeLine[0].Name}] \x1b[0m{record.Result.Value.ToString()}: {record.Name}");
                                                    }
                                                    timelineRecords[e.timelineId].RecordId = e2.record.StepId;
                                                    if(timelineRecords[e.timelineId].TimeLine != null) {
                                                        var record = timelineRecords[e.timelineId].TimeLine.Find(r => r.Id == timelineRecords[e.timelineId].RecordId);
                                                        if(record == null) {
                                                            timelineRecords[e.timelineId].RecordId = Guid.Empty;
                                                            break;
                                                        }
                                                        Console.WriteLine($"\x1b[{(int)timelineRecords[e.timelineId].Color + 30}m[{timelineRecords[e.timelineId].TimeLine[0].Name}] \x1b[0mRunning: {record.Name}");
                                                    }
                                                }
                                                var old = Console.ForegroundColor;
                                                Console.ForegroundColor = timelineRecords[e.timelineId].Color;
                                                foreach (var webconsoleline in e2.record.Value) {
                                                    if(webconsoleline.StartsWith("##[section]")) {
                                                        Console.WriteLine("******************************************************************************");
                                                        Console.WriteLine(webconsoleline.Substring("##[section]".Length));
                                                        Console.WriteLine("******************************************************************************");
                                                    } else {
                                                        Console.WriteLine($"\x1b[{(int)timelineRecords[e.timelineId].Color + 30}m|\x1b[0m {webconsoleline}");
                                                    }
                                                }
                                                Console.ForegroundColor = old;
                                                timelineRecords[e.timelineId].Pending.RemoveAt(0);
                                            }
                                            if(!parameters.quiet && timelineRecords[e.timelineId].RecordId != Guid.Empty && timelineRecords != null && e.timeline[0].State == TimelineRecordState.Completed) {
                                                var record = e.timeline.Find(r => r.Id == timelineRecords[e.timelineId].RecordId);
                                                if(record != null && record.Result.HasValue) {
                                                    Console.WriteLine($"\x1b[{(int)timelineRecords[e.timelineId].Color + 30}m[{e.timeline[0].Name}] \x1b[0m{record.Result.Value.ToString()}: {record.Name}");
                                                }
                                            }
                                            // if(timelineRecords.Count >= rj && timelineRecords.Values.All(r => r[0].State == TimelineRecordState.Completed)) {
                                            //     var b3 = new UriBuilder(b.ToString());
                                            //     query = new QueryBuilder();
                                            //     query.Add("repo", hr.First().repo);
                                            //     query.Add("runid", hr.Select(h => h.run_id.ToString()));
                                            //     query.Add("depending", "1");
                                            //     b3.Query = query.ToString().TrimStart('?');
                                            //     b3.Path = "runner/host/_apis/v1/Message";
                                            //     var sr2 = await client.GetStringAsync(b3.ToString());
                                            //     List<Job> jobs2 = JsonConvert.DeserializeObject<List<Job>>(sr2);
                                            //     if(jobs2?.Count > 0) {
                                            //         continue;
                                            //     }
                                            //     sr2 = await client.GetStringAsync(b2.ToString());
                                            //     jobs2 = JsonConvert.DeserializeObject<List<Job>>(sr2);
                                            //     if(jobs2.Count > jobs.Count) {
                                            //         foreach(var j in jobs2) {
                                            //             if(j.errors == null || j.errors.Count == 0) {
                                            //                 continue;
                                            //             }
                                            //             bool cont = false;
                                            //             foreach(var j2 in jobs) {
                                            //                 if(j.JobId == j2.JobId) {
                                            //                     cont = true;
                                            //                     break;
                                            //                 }
                                            //             }
                                            //             if(cont) {
                                            //                 continue;
                                            //             }
                                            //             hasErrors = true;
                                            //             foreach(var error in j.errors) {
                                            //                 Console.Error.WriteLine($"Error: {error}");
                                            //             }
                                            //         }
                                            //         jobs = jobs2;
                                            //     }
                                            //     var nc = jobs2.Count(j => !(j.errors?.Count > 0)) - jf;
                                            //     if(nc > rj) {
                                            //         rj = nc;
                                            //         if(timelineRecords.Count < rj) {
                                            //             continue;
                                            //         }
                                            //     }
                                            //     return !hasErrors && timelineRecords.Values.All(r => r[0].Result == TaskResult.Succeeded || r[0].Result == TaskResult.SucceededWithIssues || r[0].Result == TaskResult.Skipped || (jobs.Find(j => j.JobId == r[0].Id)?.ContinueOnError ?? false) ) ? 0 : 1;
                                            // }
                                        }
                                        if(line == "event: repodownload") {
                                            var repodownload = new MultipartFormDataContent();
                                            List<Stream> streamsToDispose = new List<Stream>();
                                            try {
                                                try {
                                                    EventHandler<ProcessDataReceivedEventArgs> handleoutput = (s, e) => {
                                                        var files = e.Data.Split('\0');
                                                        foreach(var file in files) {
                                                            if(file == "") break;
                                                            var modeend = file.IndexOf(' ');
                                                            var filebeg = file.IndexOf('\t') + 1;
                                                            var mode = modeend == 6 ? file.Substring(3, modeend - 3) : "644";
                                                            var filename = file.Substring(filebeg);
                                                            try {
                                                                var fs = File.OpenRead(Path.Combine(parameters.directory ?? ".", filename));
                                                                streamsToDispose.Add(fs);
                                                                repodownload.Add(new StreamContent(fs), mode + ":" + filename, filename);
                                                            }
                                                            catch {

                                                            }
                                                        }
                                                    };
                                                    GitHub.Runner.Sdk.ProcessInvoker gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                                    gitinvoker.OutputDataReceived += handleoutput;
                                                    var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                                                    var git = WhichUtil.Which("git", true);
                                                    await gitinvoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, "ls-files -z -s", new Dictionary<string, string>(), source.Token);
                                                    gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                                    gitinvoker.OutputDataReceived += (s, e) => {
                                                        var files = e.Data.Split('\0');
                                                        foreach(var filename in files) {
                                                            if(filename == "") break;
                                                            var relpath = Path.Combine(parameters.directory ?? ".", filename);
                                                            var mode = "644";
                                                            if(!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                                                                try {
                                                                    var finfo = new Mono.Unix.UnixFileInfo(relpath);
                                                                    if(finfo.FileAccessPermissions.HasFlag(Mono.Unix.FileAccessPermissions.UserExecute)) {
                                                                        mode = "755";
                                                                    }
                                                                }
                                                                catch {

                                                                }
                                                            }
                                                            try {
                                                                var fs = File.OpenRead(relpath);
                                                                streamsToDispose.Add(fs);
                                                                repodownload.Add(new StreamContent(fs), mode + ":" + filename, filename);
                                                            } catch {

                                                            }
                                                        }
                                                    };
                                                    await gitinvoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, "ls-files -z -o --exclude-standard", new Dictionary<string, string>(), source.Token);
                                                } catch {
                                                    foreach(var fstream in streamsToDispose) {
                                                        await fstream.DisposeAsync();
                                                    }
                                                    streamsToDispose.Clear();
                                                    repodownload.Dispose();
                                                    repodownload = new MultipartFormDataContent();
                                                }
                                                if(streamsToDispose.Count == 0) {
                                                    foreach(var w in Directory.EnumerateFiles(parameters.directory ?? ".", "*", new EnumerationOptions { RecurseSubdirectories = true, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true })) {
                                                        var relpath = Path.GetRelativePath(parameters.directory ?? ".", w).Replace('\\', '/');
                                                        var file = File.OpenRead(w);
                                                        streamsToDispose.Add(file);
                                                        var mode = "644";
                                                        if(!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                                                            try {
                                                                var finfo = new Mono.Unix.UnixFileInfo(w);
                                                                if(finfo.FileAccessPermissions.HasFlag(Mono.Unix.FileAccessPermissions.UserExecute)) {
                                                                    mode = "755";
                                                                }
                                                            }
                                                            catch {

                                                            }
                                                        }
                                                        repodownload.Add(new StreamContent(file), mode + ":" + relpath, relpath);
                                                    }
                                                }
                                                repodownload.Headers.ContentType.MediaType = "application/octet-stream";
                                                await client.PostAsync(parameters.server + data, repodownload, token);
                                            } finally {
                                                foreach(var fstream in streamsToDispose) {
                                                    await fstream.DisposeAsync();
                                                }
                                                repodownload.Dispose();
                                            }
                                        }
                                        if(line == "event: workflow") {
                                            
                                            var _workflow = JsonConvert.DeserializeObject<WorkflowEventArgs>(data);
                                            Console.WriteLine($"Workflow {_workflow.runid} finished with status {(_workflow.Success ? "Success" : "Failure")}");
                                            if(pendingWorkflows.Remove(_workflow.runid)) {
                                                hasErrors |= !_workflow.Success;
                                                if(pendingWorkflows.Count == 0) {
                                                    if(hasErrors) {
                                                        Console.WriteLine("All Workflows finished, at least one workflow failed");
                                                    } else {
                                                        Console.WriteLine("All Workflows finished successfully");
                                                    }
                                                    if(parameters.watch) {
                                                        Console.WriteLine("Waiting for file changes");
                                                        break;
                                                    }
                                                    return hasErrors ? 1 : 0;
                                                }
                                            }
                                        }
                                        // if(line == "event: finish") {
                                        //     var ev = JsonConvert.DeserializeObject<JobCompletedEvent>(data);
                                        //     if(ev.Result == TaskResult.Canceled || ev.Result == TaskResult.Abandoned || ev.Result == TaskResult.Failed) {
                                        //         hasErrors = true;
                                        //     }
                                        // }
                                    //     if(line == "event: finish") {
                                    //         var ev = JsonConvert.DeserializeObject<JobCompletedEvent>(data);
                                    //         // if(job.Result == TaskResult.Abandoned)
                                    //         foreach(var j in jobs) {
                                    //             if(j.JobId == ev.JobId) {
                                    //                 j.Finished = true;
                                    //                 Console.WriteLine($"Job {j.workflowname} - {j.name} Finished {ev.Result}");
                                    //                 if(ev.Result == TaskResult.Canceled || ev.Result == TaskResult.Abandoned || ev.Result == TaskResult.Failed) {
                                    //                     hasErrors = true;
                                    //                 }
                                    //                 if(j.TimeLineId != Guid.Empty) {
                                    //                     timelineRecords.Remove(j.TimeLineId);
                                    //                     rj--;
                                    //                     jf++;
                                    //                 }

                                    //                 break;
                                    //             }
                                    //         }

                                    //         if(timelineRecords.Count >= rj && timelineRecords.Values.All(r => r[0].State == TimelineRecordState.Completed)) {
                                    //             var b3 = new UriBuilder(b.ToString());
                                    //             query = new QueryBuilder();
                                    //             query.Add("repo", hr.First().repo);
                                    //             query.Add("runid", hr.Select(h => h.run_id.ToString()));
                                    //             query.Add("depending", "1");
                                    //             b3.Query = query.ToString().TrimStart('?');
                                    //             b3.Path = "runner/host/_apis/v1/Message";
                                    //             var sr2 = await client.GetStringAsync(b3.ToString());
                                    //             List<Job> jobs2 = JsonConvert.DeserializeObject<List<Job>>(sr2);
                                    //             if(jobs2?.Count > 0) {
                                    //                 continue;
                                    //             }
                                    //             sr2 = await client.GetStringAsync(b2.ToString());
                                    //             jobs2 = JsonConvert.DeserializeObject<List<Job>>(sr2);
                                    //             if(jobs2.Count > jobs.Count) {
                                    //                 foreach(var j in jobs2) {
                                    //                     if(j.errors == null || j.errors.Count == 0) {
                                    //                         continue;
                                    //                     }
                                    //                     bool cont = false;
                                    //                     foreach(var j2 in jobs) {
                                    //                         if(j.JobId == j2.JobId) {
                                    //                             cont = true;
                                    //                             break;
                                    //                         }
                                    //                     }
                                    //                     if(cont) {
                                    //                         continue;
                                    //                     }
                                    //                     hasErrors = true;
                                    //                     foreach(var error in j.errors) {
                                    //                         Console.Error.WriteLine($"Error: {error}");
                                    //                     }
                                    //                 }
                                    //                 jobs = jobs2;
                                    //             }
                                    //             var nc = jobs2.Count(j => !(j.errors?.Count > 0)) - jf;
                                    //             if(nc > rj) {
                                    //                 rj = nc;
                                    //                 if(timelineRecords.Count < rj) {
                                    //                     continue;
                                    //                 }
                                    //             }
                                    //             return !hasErrors && timelineRecords.Values.All(r => r[0].Result == TaskResult.Succeeded || r[0].Result == TaskResult.SucceededWithIssues || r[0].Result == TaskResult.Skipped || (jobs.Find(j => j.JobId == r[0].Id)?.ContinueOnError ?? false) ) ? 0 : 1;
                                    //         }
                                    //         // var b3 = new UriBuilder(b.ToString());
                                    //         // query = new QueryBuilder();
                                    //         // query.Add("repo", hr.First().repo);
                                    //         // query.Add("runid", hr.Select(h => h.run_id.ToString()));
                                    //         // query.Add("depending", "1");
                                    //         // b3.Query = query.ToString().TrimStart('?');
                                    //         // b3.Path = "runner/host/_apis/v1/Message";
                                    //         // var sr2 = await client.GetStringAsync(b3.ToString());
                                    //         // List<Job> jobs2 = JsonConvert.DeserializeObject<List<Job>>(sr2);
                                    //         // bool cont2 = jobs2?.Count > 0;
                                    //         // sr2 = await client.GetStringAsync(b2.ToString());
                                    //         // jobs2 = JsonConvert.DeserializeObject<List<Job>>(sr2);
                                    //         // if(jobs2.Count > jobs.Count) {
                                    //         //     foreach(var j in jobs2) {
                                    //         //         if(j.errors == null || j.errors.Count == 0) {
                                    //         //             continue;
                                    //         //         }
                                    //         //         bool cont = false;
                                    //         //         foreach(var j2 in jobs) {
                                    //         //             if(j.JobId == j2.JobId) {
                                    //         //                 cont = true;
                                    //         //                 break;
                                    //         //             }
                                    //         //         }
                                    //         //         if(cont) {
                                    //         //             continue;
                                    //         //         }
                                    //         //         hasErrors = true;
                                    //         //         foreach(var error in j.errors) {
                                    //         //             Console.Error.WriteLine($"Error: {error}");
                                    //         //         }
                                    //         //     }
                                    //         //     jobs = jobs2;
                                    //         // }
                                    //         // var nc = jobs2.Count(j => !(j.errors?.Count > 0) && !j.Cancelled);
                                    //         // rj = nc;
                                    //         // if(cont2) {
                                    //         //     continue;
                                    //         // }
                                    //         // if(timelineRecords.Count >= rj && timelineRecords.Values.All(r => r[0].State == TimelineRecordState.Completed)) {
                                    //         //     return !hasErrors && timelineRecords.Values.All(r => r[0].Result == TaskResult.Succeeded || r[0].Result == TaskResult.SucceededWithIssues || r[0].Result == TaskResult.Skipped || (jobs.Find(j => j.JobId == r[0].Id)?.ContinueOnError ?? false) ) ? 0 : 1;
                                    //         // }
                                    //     }
                                    }
                                }
                            } catch (Exception except) {
                                // System.Diagnostics.Process.Start()
                                // GitHub.Runner.Sdk.ProcessInvoker invoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                // var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                                // await invoker.ExecuteAsync(binpath, Path.Join(binpath, $"Runner.Server{IOUtil.ExeExtension}"), "", new Dictionary<string, string>(), CancellationToken.None);
                                Console.WriteLine($"Exception: {except.Message}, {except.StackTrace}");
                                return 0;
                            }
                            return 0;
                        });
                        if(!parameters.watch) {
                            return ret;
                        }
                    }
                } finally {
                    source.Cancel();
                    await Task.WhenAll(listener);
                }
                // await invoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, $"stash drop {stashcommitRef}", new Dictionary<string, string>(), CancellationToken.None);
                return 0;
            };

            foreach(var ev in validevents) {
                var cmd = new Command(ev, "");
                rootCommand.AddCommand(cmd);
                Func<Parameters, Task<int>> handler2 = (parameters) => {
                    parameters.Event = ev;
                    return handler(parameters);
                };
                cmd.Handler = CommandHandler.Create(handler2);
                foreach(var opt in rootCommand.Options) {
                    if(!opt.Aliases.Contains("--event")) {
                        cmd.AddOption(opt);
                    }
                }
            }
            var startserver = new Command("startserver", "Starts a server listening on the supplied address or selects a random free http address");
            rootCommand.AddCommand(startserver);
            Func<Parameters, Task<int>> sthandler = p => {
                p.StartServer = true;
                p.parallel = 0;
                return handler(p);
            };
            startserver.Handler = CommandHandler.Create(sthandler);

            var startrunner = new Command("startrunner", "Configures and runs n runner");
            rootCommand.AddCommand(startrunner);
            Func<Parameters, Task<int>> thandler = p => {
                p.StartRunner = true;
                return handler(p);
            };
            startrunner.Handler = CommandHandler.Create(thandler);

            foreach(var opt in rootCommand.Options) {
                if(opt.Aliases.Contains("--server") || opt.Aliases.Contains("--verbose")) {
                    startserver.AddOption(opt);
                    startrunner.AddOption(opt);
                } else if(opt.Aliases.Contains("--parallel") || opt.Aliases.Contains("--privileged") || opt.Aliases.Contains("--userns") || opt.Aliases.Contains("--container-architecture")) {
                    startrunner.AddOption(opt);
                }
            }

            rootCommand.Handler = CommandHandler.Create(handler);

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
            foreach(var config in configs) {
                try {
                    var content = File.ReadAllLines(config);
                    foreach(var line in content) {
                        if(line.StartsWith("-")) {
                            cargs.AddRange(cfgregex.Split(line, 2));
                        }
                    }
                } catch {

                }
            }
            cargs.AddRange(args);
            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(cargs.ToArray()).Result;
        }
    }
}

// Console.WriteLine("Try starting Runner.Server, Please wait...");
//                     var proc = Process.StartProcess("Runner.Server", $"--server {server}");
//                     proc.EnableRaisingEvents = true;
