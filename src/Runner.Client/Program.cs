using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
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
using System.Runtime.InteropServices;
using System.ComponentModel;
using GitHub.Services.WebApi;
using System.CommandLine.Builder;
using System.CommandLine.Binding;

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
            public string containerPlatform { get; set; }
            public string containerArchitecture { get => containerPlatform; set => containerPlatform = value; }
            public string defaultbranch { get; set; }
            public string directory { get; set; }
            public bool verbose { get; set; }
            public int parallel { get; set; }
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
            public string[] EnvironmentSecretFiles { get; set; }
            public string[] Inputs { get; internal set; }
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
            public string WorkflowName {get;set;}
        }

        struct RepoDownload {
            public string Url {get;set;}
            public bool Submodules {get;set;}
            public bool NestedSubmodules {get;set;}
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
            var agentname = $"Agent-{Guid.NewGuid().ToString()}";
            string tmpdir = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "Agents", agentname);
            Directory.CreateDirectory(tmpdir);
            try {
                int attempt = 1;
                while(!source.IsCancellationRequested) {
                    try {
                        var inv = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                        if(parameters.verbose) {
                            inv.OutputDataReceived += _out;
                            inv.ErrorDataReceived += _out;
                        }
                        
                        var runnerEnv = new Dictionary<string, string>() { {"RUNNER_SERVER_CONFIG_ROOT", tmpdir }, { "GHARUN_CHANGE_PROCESS_GROUP", "1" }};
                        if(!parameters.NoSharedToolcache && Environment.GetEnvironmentVariable("RUNNER_TOOL_CACHE") == null) {
                            runnerEnv["RUNNER_TOOL_CACHE"] = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "tool_cache");
                        }
                        if(parameters.containerArchitecture != null) {
                            runnerEnv["RUNNER_CONTAINER_ARCH"] = parameters.containerArchitecture;
                        }
                        if(parameters.privileged) {
                            runnerEnv["RUNNER_CONTAINER_PRIVILEGED"] = "1";
                        }
                        if(parameters.userns != null) {
                            runnerEnv["RUNNER_CONTAINER_USERNS"] = parameters.userns;
                        }
                        if(parameters.KeepContainer) {
                            runnerEnv["RUNNER_CONTAINER_KEEP"] = "1";
                        }

                        var arguments = $"Configure --name {agentname} --unattended --url {parameters.server}/runner/server --token {parameters.Token ?? "empty"} --labels container-host";
#if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
                        arguments = $"\"{runner}\" {arguments}";
#endif
                        
                        var code = await inv.ExecuteAsync(binpath, file, arguments, runnerEnv, true, null, true, CancellationTokenSource.CreateLinkedTokenSource(source.Token, new CancellationTokenSource(60 * 1000).Token).Token);
                        int execAttempt = 1;                    
                        while(true) {
                            try {
                                var runnerlistener = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                if(parameters.verbose) {
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

        private static void WriteLogLine(int color, string tag, string message) {
            Console.ResetColor();
            Console.ForegroundColor = (ConsoleColor)color;
            Console.Write("[" + tag + "] ");
            Console.ResetColor();
            Console.WriteLine(message);
        }

        private static void WriteLogLine(int color, string message) {
            Console.ResetColor();
            Console.ForegroundColor = (ConsoleColor)color;
            Console.Write("|");
            Console.ResetColor();
            Console.WriteLine(message);
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
        static int Main(string[] args)
        {
            if(System.OperatingSystem.IsWindowsVersionAtLeast(10)) {
                WindowsUtils.EnableVT();
            }
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            
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

            var secretOpt = new Option<string[]>(
                new[] { "-s", "--secret" },
                description: "Secret for your workflow, overrides keys from your secrets file. E.g. `-s Name` or `-s Name=Value`. You will be asked for a value if you add `--secret name`, but no environment variable with name `name` exists.") {
                    AllowMultipleArgumentsPerToken = false,
                    Arity = ArgumentArity.ZeroOrMore
                };
            var envOpt = new Option<string[]>(
                new[] { "--env" },
                description: "Environment variable for your workflow, overrides keys from your env file. E.g. `--env Name` or `--env Name=Value`. You will be asked for a value if you add `--env name`, but no environment variable with name `name` exists.") {
                    AllowMultipleArgumentsPerToken = false,
                    Arity = ArgumentArity.ZeroOrMore
                };
            var envFile = new Option<string>(
                "--env-file",
                getDefaultValue: () => ".env",
                description: "Environment variables for your workflow.");
            var matrixOpt = new Option<string[]>(
                new[] { "-m", "--matrix" },
                description: "Matrix filter e.g. `-m Key:value`, use together with `--job <job>`. Use multiple times to filter more specifically. If you want to force a value to be a string you need to quote it, e.g. `\"-m Key:\\\"1\\\"\"` or `\"-m Key:\"\"1\"\"\"` (requires shell escaping)") {
                    AllowMultipleArgumentsPerToken = false,
                    Arity = ArgumentArity.ZeroOrMore
                };
            
            var workflowOption = new Option<string[]>(
                "--workflow",
                description: "Workflow(s) to run. Use multiple times to execute more workflows parallel.") {
                    AllowMultipleArgumentsPerToken = false,
                    Arity = ArgumentArity.ZeroOrMore
                };

            var platformOption = new Option<string[]>(
                new[] { "-P", "--platform" },
                description: "Platform mapping to run the workflow in a docker container (similar behavior as using the container property of a workflow job, the container property of a job will take precedence over your specified docker image) or host. E.g. `-P ubuntu-latest=ubuntu:latest` (Docker Linux Container), `-P ubuntu-latest=-self-hosted` (Local Machine), `-P windows-latest=-self-hosted` (Local Machine), `-P windows-latest=mcr.microsoft.com/windows/servercore:ltsc2022` (Docker Windows container, windows only), `-P macos-latest=-self-hosted` (Local Machine) or with multiple labels `-P self-hosted,testmachine,anotherlabel=-self-hosted` (Local Machine).") {
                    AllowMultipleArgumentsPerToken = false,
                    Arity = ArgumentArity.ZeroOrMore
                };
            var serverOpt = new Option<string>(
                "--server",
                description: "Runner.Server address, e.g. `http://localhost:5000` or `https://localhost:5001`.");
            var payloadOpt = new Option<string>(
                new[] { "-e", "--payload", "--eventpath" },
                "Webhook payload to send to the Runner.");
            var noDefaultPayloadOpt = new Option<bool>(
                "--no-default-payload",
                "Do not provide or merge autogenerated payload content, will pass your unmodified payload to the runner.");
            var eventOpt = new Option<string>(
                "--event",
                getDefaultValue: () => "push",
                description: "Which event to send to a worker, ignored if you use subcommands which overriding the event.");
            var secretFileOpt = new Option<string>(
                "--secret-file",
                getDefaultValue: () => ".secrets",
                description: "Secrets for your workflow.");
            var environmentSecretFileOpt = new Option<string[]>(
                "--environment-secret-file",
                description: "Environment Secrets with name name for your workflow, name=filename.yml.");
            var jobOpt = new Option<string>(
                new[] {"-j", "--job"},
                description: "Job to run. If multiple jobs have the same name in multiple workflows, all matching jobs will run. Use together with `--workflow <workflow>` to run exact one job.");
            var listOpt = new Option<bool>(
                new[] { "-l", "--list"},
                description: "List jobs for the selected event (defaults to push).");
            var workflowsOpt = new Option<string>(
                new[] { "-W", "--workflows"},
                description: "Workflow file or directory which contains workflows, only used if no `--workflow <workflow>` option is set.");
            var actorOpt = new Option<string>(
                new[] {"-a" , "--actor"},
                "The login of the user who initiated the workflow run, ignored if already in your event payload.");
            var watchOpt = new Option<bool>(
                new[] {"-w", "--watch"},
                "Run automatically on every file change.");
            var quietOpt = new Option<bool>(
                new[] {"-q", "--quiet"},
                "Display no progress in the cli.");
            var privilegedOpt = new Option<bool>(
                "--privileged",
                "Run the docker container under privileged mode, only applies to container jobs using this Runner fork.");
            var usernsOpt = new Option<string>(
                "--userns",
                "Change the docker container linux user namespace, only applies to container jobs using this Runner fork.");
            var containerPlatformOpt = new Option<string>(
                new [] { "--container-architecture", "--container-platform" },
                "Change the docker container platform, if docker supports it. Only applies to container jobs using this Runner fork.");
            var keepContainerOpt = new Option<bool>(
                "--keep-container",
                "Do not clean up docker container after job, this leaks resources.");
            var defaultbranchOpt = new Option<string>(
                "--defaultbranch",
                description: "The default branch of your workflow run, ignored if already in your event payload.");
            var DirectoryOpt = new Option<string>(
                new[] {"-C", "--directory"},
                "Change the directory of your local repository, provided file or directory names are still resolved relative to your current working directory.");
            var verboseOpt = new Option<bool>(
                new[] {"-v", "--verbose"},
                "Print more details like server / runner logs to stdout.");
            var parallelOpt = new Option<int>(
                "--parallel",
                getDefaultValue: () => 1,
                description: "Run n parallel runners, ignored if `--server <server>` is used.");
            var noCopyGitDirOpt = new Option<bool>(
                "--no-copy-git-dir",
                description: "Avoid copying the .git folder into the runner if it exists.");
            var keepRunnerDirectoryOpt = new Option<bool>(
                "--keep-runner-directory",
                description: "Skip deleting temporary runner directories.");
            var noSharedToolCacheOpt = new Option<bool>(
                "--no-shared-toolcache",
                description: "Do not share toolcache between runners, a shared toolcache may cause workflow failures.");
            var noReuseOpt = new Option<bool>(
                "--no-reuse",
                "Do not reuse a configured self-hosted runner, creates a new instance after a job completes.");
            var gitServerUrlOpt = new Option<string>(
                "--git-server-url",
                getDefaultValue: () => "https://github.com",
                description: "Url to github or gitea instance.");
            var gitApiServerUrlOpt = new Option<string>(
                "--git-api-server-url",
                description: "Url to github or gitea api. ( e.g https://api.github.com )");
            var gitGraphQlOpt = new Option<string>(
                "--git-graph-ql-server-url",
                description: "Url to github graphql api. ( e.g https://api.github.com/graphql )");
            var gitTarballUrlOpt = new Option<string>(
                "--git-tarball-url",
                description: "Url to github or gitea tarball api url, defaults to `<git-api-server-url>/repos/{0}/tarball/{1}`. `{0}` is replaced by `<owner>/<repo>`, `{1}` is replaced by branch, tag or sha.");
            var gitZipballUrlOpt = new Option<string>(
                "--git-zipball-url",
                description: "Url to github or gitea zipball api url, defaults to `<git-api-server-url>/repos/{0}/zipball/{1}`. `{0}` is replaced by `<owner>/<repo>`, `{1}` is replaced by branch, tag or sha.");
            var remoteCheckoutOpt = new Option<bool>(
                "--remote-checkout",
                description: "Do not inject localcheckout into your workflows, always use the original actions/checkout.");
            var artifactOutputDirOpt = new Option<string>(
                "--artifact-output-dir",
                description: "Output folder for all artifacts produced by this runs.");
            var logOutputDirOpt = new Option<string>(
                "--log-output-dir",
                description: "Output folder for all logs produced by this runs.");
            var repositoryOpt = new Option<string>(
                "--repository",
                description: "Custom github.repository.");
            var shaOpt = new Option<string>(
                "--sha",
                description: "Custom github.sha.");
            var refOpt = new Option<string>(
                "--ref",
                description: "Custom github.ref.");
            var workflowInputsOpt = new Option<string[]>(
                new[] {"-i", "--input"},
                description: "Inputs to add to the payload. E.g. `--input name=value`");
            var rootCommand = new RootCommand
            {
                workflowOption,
                serverOpt,
                payloadOpt,
                noDefaultPayloadOpt,
                eventOpt,
                envOpt,
                envFile,
                secretOpt,
                secretFileOpt,
                environmentSecretFileOpt,
                jobOpt,
                matrixOpt,
                listOpt,
                workflowsOpt,
                platformOption,
                actorOpt,
                watchOpt,
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
                noSharedToolCacheOpt,
                noReuseOpt,
                gitServerUrlOpt,
                gitApiServerUrlOpt,
                gitGraphQlOpt,
                gitTarballUrlOpt,
                gitZipballUrlOpt,
                remoteCheckoutOpt,
                artifactOutputDirOpt,
                logOutputDirOpt,
                repositoryOpt,
                shaOpt,
                refOpt,
            };

            rootCommand.Description = "Run your workflows locally.";

            // Note that the parameters of the handler method are matched according to the names of the options
            Func<Parameters , Task<int>> handler = async (parameters) =>
            {
                if(parameters.list) {
                    parameters.parallel = 0;
                }
                if(parameters.actor == null) {
                    parameters.actor = "runnerclient";
                }
                List<string> errors = new List<string>();
                if(parameters.matrix?.Length > 0) {
                    if(parameters.job == null) {
                        errors.Add("--matrix is only supported together with --job");
                    }
                    foreach(var p in parameters.matrix) {
                        if(!p.Contains(":")) {
                            errors.Add($"Invalid Argument for `--matrix`: `{p}`, missing `:`");
                        }
                    }
                }
                if(parameters.platform?.Length > 0) {
                    foreach(var p in parameters.platform) {
                        if(!p.Contains("=")) {
                            errors.Add($"Invalid Argument for `--platform`: `{p}`, missing `=`");
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
                        Console.WriteLine($"CTRL+C received Cancel Running Jobs");
                        cancelWorkflow.Invoke();
                        return;
                    }
                    e.Cancel = !canceled;
                    Console.WriteLine($"CTRL+C received {(e.Cancel ? "Shutting down... CTRL+C again to Terminate" : "Terminating")}");
                    canceled = true;
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
                            Console.WriteLine("Starting Server...");
                            if(parameters.server == null) {
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
                                        parameters.server = builder.Uri.ToString().Trim('/');
                                    }
                                } catch {
                                }
                                if(parameters.server == null) {
                                    try {
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
                                    } catch {
                                    }
                                }
                                if(parameters.server == null) {
                                    Console.WriteLine("Failed to autodetect non loopback ip, docker actions will fail to connect");
                                    parameters.server = "http://localhost:0";
                                }
                            }
                            GitHub.Runner.Sdk.ProcessInvoker invoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                            if(parameters.verbose) {
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
                            
                            serverconfig["Kestrel"] = JObject.FromObject(new { Endpoints = new { Http = new { Url = parameters.server } } });
                            serverconfig["Runner.Server"] = JObject.FromObject(new { 
                                GitServerUrl = parameters.GitServerUrl,
                                GitApiServerUrl = parameters.GitApiServerUrl,
                                GitGraphQlServerUrl = parameters.GitGraphQlServerUrl,
                                ActionDownloadUrls = new [] {
                                    new {
                                        TarballUrl = parameters.GitTarballUrl ?? parameters.GitApiServerUrl + "/repos/{0}/tarball/{1}",
                                        ZipballUrl = parameters.GitZipballUrl ?? parameters.GitApiServerUrl + "/repos/{0}/zipball/{1}",
                                    }
                                }
                            });
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
                                            var x = await invoker.ExecuteAsync(binpath, file, arguments, new Dictionary<string, string>() { {"RUNNER_SERVER_APP_JSON_SETTINGS_FILE", serverconfigfileName }, { "RUNNER_CLIENT_PIPE", pipeServer.GetClientHandleAsString() }, { "RUNNER_CLIENT_PIPE_IN", shutdownPipe.GetClientHandleAsString() }, { "GHARUN_CHANGE_PROCESS_GROUP", "1" }}, false, null, true, runToken.Token);
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
                                            parameters.server = line;
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

                        if(parameters.parallel > 0) {
                            Console.WriteLine($"Starting {parameters.parallel} Runner{(parameters.parallel != 1 ? "s" : "")}...");
                            var workerchannel = Channel.CreateBounded<bool>(1);
                            for(int i = 0; i < parameters.parallel; i++) {
                                listener.Add(CreateRunner(binpath, parameters, listener, workerchannel, source));
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
                            Console.WriteLine($"Press {(Debugger.IsAttached ? "Enter or CTRL+C" : "CTRL+C")} to stop the {(parameters.StartServer ? "Server" : (parameters.parallel != 1 ? "Runners" : "Runner"))}");

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
                                            addedFiles.Add(addedFile.Replace('\\', '/'));
                                        }
                                        addedFile = null;
                                    }
                                    while(changedFile != null || changed.TryDequeue(out changedFile)) {
                                        if(!await IsIgnored(parameters.directory ?? ".", changedFile)) {
                                            changedFiles.Add(changedFile.Replace('\\', '/'));
                                        }
                                        changedFile = null;
                                    }
                                    while(removedFile != null || removed.TryDequeue(out removedFile)) {
                                        if(!await IsIgnored(parameters.directory ?? ".", removedFile)) {
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
                            var workflows = parameters.workflow;
                            if(workflows == null || workflows.Length == 0) {
                                if(string.IsNullOrEmpty(parameters.workflows)) {
                                    parameters.workflows = Path.Join(parameters.directory ?? ".", ".github/workflows");
                                }
                                if(Directory.Exists(parameters.workflows)) {
                                    try {
                                        workflows = Directory.GetFiles(parameters.workflows, "*.yml", new EnumerationOptions { RecurseSubdirectories = false, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true }).Concat(Directory.GetFiles(parameters.workflows, "*.yaml", new EnumerationOptions { RecurseSubdirectories = false, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = true })).ToArray();
                                        if((workflows == null || workflows.Length == 0)) {
                                            Console.Error.WriteLine($"No workflow *.yml / *.yaml file found inside of {parameters.workflows}");
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
                                HttpClientHandler handler = new HttpClientHandler() {
                                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                                };  
                                var client = new HttpClient(handler);
                                client.DefaultRequestHeaders.Add("X-GitHub-Event", parameters.Event);
                                var b = new UriBuilder(parameters.server);
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
                                            var name = Path.GetRelativePath(parameters.directory ?? ".", w).Replace('\\', '/');
                                            mp.Add(new StreamContent(workflow), name, name);
                                        } catch {
                                            Console.WriteLine($"Failed to read file: {w}");
                                            return 1;
                                        }
                                    }
                                    
                                    List<string> wenv = new List<string>();
                                    List<string> wsecrets = new List<string>();
                                    try {
                                        wenv.AddRange(Util.ReadEnvFile(parameters.envFile));
                                    } catch {
                                        if(parameters.envFile != ".env") {
                                            Console.WriteLine($"Failed to read file: {parameters.envFile}");
                                        }
                                    }
                                    try {
                                        wsecrets.AddRange(Util.ReadEnvFile(parameters.secretFile));
                                    } catch {
                                        if(parameters.secretFile != ".secrets") {
                                            Console.WriteLine($"Failed to read file: {parameters.secretFile}");
                                        }
                                    }
                                    if(parameters.job != null) {
                                        query.Add("job", parameters.job);
                                    }
                                    if(parameters.matrix?.Length > 0) {
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
                                    if(parameters.RemoteCheckout) {
                                        query.Add("localcheckout", "false");
                                    }
                                    JObject payloadContent = new JObject();
                                    {
                                        var acommits = new JArray();
                                        payloadContent["commits"] = acommits;
                                        var sha = parameters.Sha;
                                        var bf = "0000000000000000000000000000000000000000";
                                        var user = JObject.FromObject(new { login = parameters.actor, name = parameters.actor, email = $"{parameters.actor}@runner.server.localhost", id = 976638, type = "user" });
                                        payloadContent["sender"] = user;
                                        payloadContent["pusher"] = user;
                                        var repoowner = user;
                                        payloadContent["before"] = bf;
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
                                                gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                                gitinvoker.OutputDataReceived += handleoutput;
                                                var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                                                await gitinvoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, "tag --points-at HEAD", new Dictionary<string, string>(), source.Token);
                                                if(line != null) {
                                                    Ref = "refs/tags/" + line;
                                                }
                                            }
                                            if(string.IsNullOrEmpty(Ref) || string.IsNullOrEmpty(repofullname)) {
                                                gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                                gitinvoker.OutputDataReceived += handleoutput;
                                                await gitinvoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, "symbolic-ref HEAD", new Dictionary<string, string>(), source.Token);
                                                if(line != null) {
                                                    var _ref = line;
                                                    if(string.IsNullOrEmpty(Ref)) {
                                                        Ref = _ref;
                                                    }
                                                    if(string.IsNullOrEmpty(repofullname)) {
                                                        line = null;
                                                        gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                                        gitinvoker.OutputDataReceived += handleoutput;
                                                        await gitinvoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, $"for-each-ref --format=%(upstream:short) {_ref}", new Dictionary<string, string>(), source.Token);
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
                                                gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                                gitinvoker.OutputDataReceived += handleoutput;
                                                await gitinvoker.ExecuteAsync(parameters.directory ?? Path.GetFullPath("."), git, "rev-parse HEAD", new Dictionary<string, string>(), source.Token);
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
                                        payloadContent["ref"] = Ref;
                                        payloadContent["after"] = sha;
                                        var commit = JObject.FromObject(new { message = "Untraced changes", id = sha, added = addedFiles, removed = removedFiles, modified = changedFiles });
                                        acommits.AddFirst(commit);
                                        payloadContent["head_commit"] = commit;
                                        var repository = JObject.FromObject(new { owner = repoowner, default_branch = parameters.defaultbranch ?? "main", master_branch = parameters.defaultbranch ?? "master", name = repofullname.Split('/', 2)[1], full_name = repofullname });
                                        payloadContent["repository"] = repository;
                                    }
                                    
                                    if(parameters.payload != null) {
                                        try {
                                            // 
                                            var filec = await File.ReadAllTextAsync(parameters.payload, Encoding.UTF8);
                                            var obj = JObject.Parse(filec);

                                            if(parameters.NoDefaultPayload) {  
                                                payloadContent = obj;
                                            } else {
                                                payloadContent.Merge(obj, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                                            }
                                        } catch {
                                            Console.WriteLine($"Failed to read file: {parameters.payload}");
                                            return 1;
                                        }
                                    } else if(parameters.NoDefaultPayload) {
                                        payloadContent = new JObject();
                                    }
                                    if(parameters.Inputs?.Length > 0) {
                                        var inputs = new JObject();
                                        payloadContent["inputs"] = inputs;
                                        foreach(var input in parameters.Inputs) {
                                            var kv = input.Split('=', 2);
                                            inputs[kv[0]] = kv[1];
                                        }
                                    }
                                    mp.Add(new StringContent(payloadContent.ToString()), "event", "event.json");
                                    if(parameters.EnvironmentSecretFiles?.Length > 0) {
                                        foreach(var opt in parameters.EnvironmentSecretFiles) {
                                            var subopt = opt.Split('=', 2);
                                            string name = subopt.Length == 2 ? subopt[0] : "";
                                            string filename = subopt.Length == 2 ? subopt[1] : subopt[0];
                                            if(filename.EndsWith(".yml") || filename.EndsWith(".yaml")) {
                                                var envfile = File.OpenRead(filename);
                                                workflowsToDispose.Add(envfile);
                                                mp.Add(new StreamContent(envfile), "actions-environment-secrets", $"{name}.secrets");
                                            } else {
                                                var ser = new YamlDotNet.Serialization.SerializerBuilder().Build();
                                                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                                Util.ReadEnvFile(filename, (key, val) => dict[key] = val);
                                                mp.Add(new StringContent(ser.Serialize(dict)), "actions-environment-secrets", $"{name}.secrets");
                                            }
                                        }
                                    }
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
                                cancelWorkflow = () => {
                                    cancelWorkflow = null;
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
                                                        client.PostAsync(GetCancelWorkflowUrl(b.ToString(), _job.runid), null, CancellationToken.None);
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
                                            if(!parameters.quiet && line == "event: log") {
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
                                                if(!parameters.quiet && timelineRecords[e.timelineId].RecordId != Guid.Empty && timelineRecords != null && e.timeline[0].State == TimelineRecordState.Completed) {
                                                    var record = e.timeline.Find(r => r.Id == timelineRecords[e.timelineId].RecordId);
                                                    if(record != null && record.Result.HasValue) {
                                                        var rec = timelineRecords[e.timelineId];
                                                        WriteLogLine((int)rec.Color, $"{(rec.WorkflowName != null ? $"{rec.WorkflowName} / " : "")}{rec.TimeLine[0].Name}", $"{record.Result.Value.ToString()}: {record.Name}");
                                                    }
                                                }
                                            }
                                            if(line == "event: repodownload") {
                                                var endpoint = JsonConvert.DeserializeObject<RepoDownload>(data);
                                                Task.Run(async () => {
                                                    var repodownload = new MultipartFormDataContent();
                                                    List<Stream> streamsToDispose = new List<Stream>();
                                                    try {
                                                        try {
                                                            await CollectRepoFiles(parameters.directory ?? Path.GetFullPath("."), endpoint, repodownload, streamsToDispose, 0, parameters, source);
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
                                                        await client.PostAsync(parameters.server + endpoint.Url, repodownload, token);
                                                    } finally {
                                                        foreach(var fstream in streamsToDispose) {
                                                            await fstream.DisposeAsync();
                                                        }
                                                        repodownload.Dispose();
                                                    }
                                                });
                                            }
                                            if(line == "event: workflow") {
                                                var _workflow = JsonConvert.DeserializeObject<WorkflowEventArgs>(data);
                                                Console.WriteLine($"Workflow {_workflow.runid} finished with status {(_workflow.Success ? "Success" : "Failure")}");
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
                                                var artifactUri = new UriBuilder(parameters.server);
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
                                                var timeLineRecords = JsonConvert.DeserializeObject<List<TimelineRecord>>(await client.GetStringAsync(parameters.server + $"/_apis/v1/Timeline/{job.TimeLineId.ToString()}"));
                                                foreach(var timeLineRecord in timeLineRecords) {
                                                    try {
                                                        if(timeLineRecord?.Log?.Id != null) {
                                                            var destpath = Path.Combine(logBasePath, timeLineRecord.Log.Id + "-" + special.Replace(timeLineRecord.Name, "-"));
                                                            Directory.CreateDirectory(Path.GetDirectoryName(destpath));
                                                            var logFileUri = new UriBuilder(parameters.server);
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
                        if(!parameters.watch) {
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
            var binder = new MyCustomBinder(bindingContext => {
                var parameters = new Parameters();
                parameters.workflow = bindingContext.ParseResult.GetValueForOption(workflowOption);
                parameters.server = bindingContext.ParseResult.GetValueForOption(serverOpt);
                parameters.payload = bindingContext.ParseResult.GetValueForOption(payloadOpt);
                parameters.NoDefaultPayload = bindingContext.ParseResult.GetValueForOption(noDefaultPayloadOpt);
                parameters.Event = bindingContext.ParseResult.GetValueForOption(eventOpt);
                parameters.env = bindingContext.ParseResult.GetValueForOption(envOpt);
                parameters.envFile = bindingContext.ParseResult.GetValueForOption(envFile);
                parameters.secret = bindingContext.ParseResult.GetValueForOption(secretOpt);
                parameters.secretFile = bindingContext.ParseResult.GetValueForOption(secretFileOpt);
                parameters.EnvironmentSecretFiles = bindingContext.ParseResult.GetValueForOption(environmentSecretFileOpt);
                parameters.job = bindingContext.ParseResult.GetValueForOption(jobOpt);
                parameters.matrix = bindingContext.ParseResult.GetValueForOption(matrixOpt);
                parameters.list = bindingContext.ParseResult.GetValueForOption(listOpt);
                parameters.workflows = bindingContext.ParseResult.GetValueForOption(workflowsOpt);
                parameters.platform = bindingContext.ParseResult.GetValueForOption(platformOption);
                parameters.actor = bindingContext.ParseResult.GetValueForOption(actorOpt);
                parameters.watch = bindingContext.ParseResult.GetValueForOption(watchOpt);
                parameters.quiet = bindingContext.ParseResult.GetValueForOption(quietOpt);
                parameters.privileged = bindingContext.ParseResult.GetValueForOption(privilegedOpt);
                parameters.userns = bindingContext.ParseResult.GetValueForOption(usernsOpt);
                parameters.containerPlatform = bindingContext.ParseResult.GetValueForOption(containerPlatformOpt);
                parameters.KeepContainer = bindingContext.ParseResult.GetValueForOption(keepContainerOpt);
                parameters.directory = bindingContext.ParseResult.GetValueForOption(DirectoryOpt);
                parameters.verbose = bindingContext.ParseResult.GetValueForOption(verboseOpt);
                parameters.parallel = bindingContext.ParseResult.GetValueForOption(parallelOpt);
                parameters.NoCopyGitDir = bindingContext.ParseResult.GetValueForOption(noCopyGitDirOpt);
                parameters.KeepRunnerDirectory = bindingContext.ParseResult.GetValueForOption(keepRunnerDirectoryOpt);
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
                parameters.RemoteCheckout = bindingContext.ParseResult.GetValueForOption(remoteCheckoutOpt);
                parameters.ArtifactOutputDir = bindingContext.ParseResult.GetValueForOption(artifactOutputDirOpt);
                parameters.LogOutputDir = bindingContext.ParseResult.GetValueForOption(logOutputDirOpt);
                parameters.Repository = bindingContext.ParseResult.GetValueForOption(repositoryOpt);
                parameters.Sha = bindingContext.ParseResult.GetValueForOption(shaOpt);
                parameters.Ref = bindingContext.ParseResult.GetValueForOption(refOpt);
                parameters.Inputs = bindingContext.ParseResult.GetValueForOption(workflowInputsOpt);
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
                if(ev == "workflow_dispatch") {
                    cmd.AddOption(workflowInputsOpt);
                }
            }
            var startserver = new Command("startserver", "Starts a server listening on the supplied address or selects a random free http address.");
            rootCommand.AddCommand(startserver);
            Func<Parameters, Task<int>> sthandler = p => {
                p.StartServer = true;
                p.parallel = 0;
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

            foreach(var opt in rootCommand.Options) {
                if(opt.Aliases.Contains("--server") || opt.Aliases.Contains("--verbose")) {
                    startserver.AddOption(opt);
                    startrunner.AddOption(opt);
                } else if(opt.Aliases.Contains("--parallel") || opt.Aliases.Contains("--privileged") || opt.Aliases.Contains("--userns") || opt.Aliases.Contains("--container-architecture")) {
                    startrunner.AddOption(opt);
                }
            }
            startrunner.AddOption(new Option<string>(
                "--token",
                description: "custom runner token to use"));

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

        private static async Task CollectRepoFiles(string wd, RepoDownload endpoint, MultipartFormDataContent repodownload, List<Stream> streamsToDispose, long level, Parameters parameters, CancellationTokenSource source) {
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
                                submoduleTasks.Add(() => CollectRepoFiles(Path.Combine(wd, filename), endpoint, repodownload, streamsToDispose, level + 1, parameters, source));
                            }
                            continue;
                        } else if(file.StartsWith("120")) {
                            //Symlink
                            submoduleTasks.Add(async () => {
                                GitHub.Runner.Sdk.ProcessInvoker gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
                                string dest = null;
                                gitinvoker.OutputDataReceived += (s, e) => {
                                    dest = e.Data;
                                };
                                var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                                var git = WhichUtil.Which("git", true);
                                var sha = file.Substring(modeend + 1, shaend - (modeend + 1));
                                await gitinvoker.ExecuteAsync(wd, git, $"cat-file -p {sha}", new Dictionary<string, string>(), source.Token);
                                repodownload.Add(new StringContent(dest), "lnk:" + Path.GetRelativePath(parameters.directory ?? ".", Path.Combine(wd, filename)).Replace('\\', '/'));
                            });
                            continue;
                            // readlink git cat-file -p sha
                        }
                    }
                    try {
                        var fs = File.OpenRead(Path.Combine(wd, filename));
                        streamsToDispose.Add(fs);
                        filename = Path.GetRelativePath(parameters.directory ?? ".", Path.Combine(wd, filename));
                        repodownload.Add(new StreamContent(fs), mode + ":" + filename.Replace('\\', '/'), filename.Replace('\\', '/'));
                    }
                    catch {

                    }
                }
            };
            GitHub.Runner.Sdk.ProcessInvoker gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
            gitinvoker.OutputDataReceived += handleoutput;
            var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var git = WhichUtil.Which("git", true);
            await gitinvoker.ExecuteAsync(wd, git, "ls-files -z -s", new Dictionary<string, string>(), source.Token);
            // collect all submodules
            foreach (var submodule in submoduleTasks) {
                await submodule();
            }
            gitinvoker = new GitHub.Runner.Sdk.ProcessInvoker(new TraceWriter(parameters.verbose));
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
                                    relpath = Path.GetRelativePath(parameters.directory ?? ".", Path.Combine(wd, filename));
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
                                    relpath = Path.GetRelativePath(parameters.directory ?? ".", Path.Combine(wd, filename));
                                    repodownload.Add(new StringContent(dest.Replace('\\', '/')), "lnk:" + relpath.Replace('\\', '/'));
                                    continue;
                                }
                            } catch {

                            }
                        }
                    try {
                        var fs = File.OpenRead(relpath);
                        streamsToDispose.Add(fs);
                        relpath = Path.GetRelativePath(parameters.directory ?? ".", Path.Combine(wd, filename));
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
                        var relpath = Path.GetRelativePath(parameters.directory ?? ".", w).Replace('\\', '/');
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