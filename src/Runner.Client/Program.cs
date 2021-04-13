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

        static int Main(string[] args)
        {
            if(System.OperatingSystem.IsWindowsVersionAtLeast(10)) {
                WindowsUtils.EnableVT();
            }

            var secretOpt = new Option<string>(
                new[] { "-s", "--secret" },
                description: "secret for you workflow, overrides keys from the secrets file");
            secretOpt.Argument.Arity = new ArgumentArity(0, ArgumentArity.MaximumArity);
            var envOpt = new Option<string>(
                new[] { "-e", "--env" },
                description: "env for you workflow, overrides keys from the env file");
            envOpt.Argument.Arity = new ArgumentArity(0, ArgumentArity.MaximumArity);
            var matrixOpt = new Option<string>(
                new[] { "-m", "--matrix" },
                description: "matrix filter e.g. '-m Key:value', use together with '--job'. Use multiple times to filter more jobs");
            matrixOpt.Argument.Arity = new ArgumentArity(0, ArgumentArity.MaximumArity);
            
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    "--workflow",
                    description: "Workflow to run"),
                new Option<string>(
                    "--server",
                    getDefaultValue: () => "http://localhost:5000",
                    description: "Runner.Server address"),
                new Option<string>(
                    "--payload",
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
                    "--secrets-file",
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
            };

            rootCommand.Description = "Send events to your runner";

            // Note that the parameters of the handler method are matched according to the names of the options
            Func<string, string, string, string, string[], string, string[], string, string, string[], bool, Task<int>> handler = async (workflow, server, payload, Event, env, envFile, secret, secretsFile, job, matrix, list) =>
            {
                if(workflow == null || payload == null) {
                    Console.WriteLine("Missing `--workflow` or `--payload` option, type `--help` for help");
                    return -1;
                }
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-GitHub-Event", Event);
                var b = new UriBuilder(server);
                var query = new QueryBuilder();
                b.Path = "runner/host/_apis/v1/Message";
                try {
                    query.Add("workflow", await File.ReadAllTextAsync(workflow, Encoding.UTF8));
                } catch {
                    Console.WriteLine($"Failed to read file: {workflow}");
                    return 1;
                }
                List<string> wenv = new List<string>();
                List<string> wsecrets = new List<string>();
                try {
                    wenv.AddRange(await File.ReadAllLinesAsync(envFile, Encoding.UTF8));
                } catch {
                    if(envFile != ".env") {
                        Console.WriteLine($"Failed to read file: {envFile}");
                    }
                }
                try {
                    wsecrets.AddRange(await File.ReadAllLinesAsync(secretsFile, Encoding.UTF8));
                } catch {
                    if(secretsFile != ".secrets") {
                        Console.WriteLine($"Failed to read file: {secretsFile}");
                    }
                }
                if(job != null) {
                    query.Add("job", job);
                }
                if(matrix?.Length > 0) {
                    if(job == null) {
                        Console.WriteLine("--matrix is only supported together with --job");
                        return 1;
                    }
                    query.Add("matrix", matrix);
                }
                if(list) {
                    query.Add("list", "1");
                }
                if(env?.Length > 0) {
                    foreach (var e in env) {
                        if(e.IndexOf('=') > 0) {
                            wenv.Add(e);
                        } else {
                            wenv.Add($"{e}:{Environment.GetEnvironmentVariable(e)}");
                        }
                    }
                }
                if(secret?.Length > 0) {
                    foreach (var e in secret) {
                        if(e.IndexOf('=') > 0) {
                            wsecrets.Add(e);
                        } else {
                            wsecrets.Add($"{e}:{Environment.GetEnvironmentVariable(e)}");
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
                string payloadContent;
                try {
                    payloadContent = await File.ReadAllTextAsync(payload, Encoding.UTF8);
                } catch {
                    Console.WriteLine($"Failed to read file: {payload}");
                    return 1;
                }
                try {
                    var resp = await client.PostAsync(b.Uri.ToString(), new StringContent(payloadContent));
                    var s = await resp.Content.ReadAsStringAsync();
                    var hr = JsonConvert.DeserializeObject<HookResponse>(s);
                    if(list) {
                        if(hr.jobList != null) {
                            Console.WriteLine($"Found {hr.jobList.Count} matched Job(s)");
                            foreach(var j in hr.jobList) {
                                Console.WriteLine(j.Name);
                            }
                            return 0;
                        }
                        Console.WriteLine("Failed to enumerate jobs");
                        return 1;
                    }

                    var b2 = new UriBuilder(b.ToString());
                    query = new QueryBuilder();
                    query.Add("repo", hr.repo);
                    query.Add("runid", hr.run_id.ToString());
                    b2.Query = query.ToString().TrimStart('?');
                    b2.Path = "runner/host/_apis/v1/Message";
                    var sr = await client.GetStringAsync(b2.ToString());
                    List<Job> jobs = JsonConvert.DeserializeObject<List<Job>>(sr);
                    Dictionary<Guid, List<TimelineRecord>> timelineRecords = new Dictionary<Guid, List<TimelineRecord>>();
                    long rj = 0;
                    bool hasErrors = false;
                    foreach(Job j in jobs) {
                        Console.WriteLine($"Running Job: {j.name}");
                        if(j.errors?.Count > 0) {
                            hasErrors = true;
                            foreach (var error in j.errors) {
                                Console.Error.WriteLine($"Error: {error}");
                            }
                        } else {
                            rj++;
                        }
                    }
                    if(rj == 0) {
                        return -1;
                    }
                    var eventstream = await client.GetStreamAsync(server + $"/runner/server/_apis/v1/TimeLineWebConsoleLog?runid={hr.run_id}");
                    Dictionary<Guid, Guid> recordId = new Dictionary<Guid, Guid>();
                    Dictionary<Guid, ConsoleColor> color = new Dictionary<Guid, ConsoleColor>();
                    List<WebConsoleEvent> pending = new List<WebConsoleEvent>();
                    int col = 0;
                    using(TextReader reader = new StreamReader(eventstream)) {
                        while(true) {
                            var line = await reader.ReadLineAsync();
                            if(line == null) {
                                break;
                            }
                            var data = await reader.ReadLineAsync();
                            data = data.Substring("data: ".Length);
                            await reader.ReadLineAsync();
                            if(line == "event: log") {
                                var e = JsonConvert.DeserializeObject<WebConsoleEvent>(data);
                                if(pending.Count > 0) {
                                    pending.Add(e);
                                    continue;
                                }
                                Guid rec;
                                if(!recordId.TryGetValue(e.timelineId, out rec)) {
                                    pending.Add(e);
                                    continue;
                                } else if(rec != e.record.StepId) {
                                    List<TimelineRecord> tr = null;
                                    if(rec != Guid.Empty && timelineRecords.TryGetValue(e.timelineId, out tr)) {
                                        var record = tr.Find(r => r.Id == rec);
                                        if(record == null || !record.Result.HasValue) {
                                            pending.Add(e);
                                            continue;
                                        }
                                        Console.WriteLine($"\x1b[{(int)color[e.timelineId] + 30}m[{tr[0].Name}] \x1b[0m{record.Result.Value.ToString()}: {record.Name}");
                                    }
                                    recordId[e.timelineId] = e.record.StepId;
                                    if(tr != null || timelineRecords.TryGetValue(e.timelineId, out tr)) {
                                        var record = tr.Find(r => r.Id == e.record.StepId);
                                        Console.WriteLine($"\x1b[{(int)color[e.timelineId] + 30}m[{tr[0].Name}] \x1b[0mRunning: {record.Name}");
                                    }
                                }
                                var old = Console.ForegroundColor;
                                Console.ForegroundColor = color[e.timelineId];
                                foreach (var webconsoleline in e.record.Value) {
                                    if(webconsoleline.StartsWith("##[section]")) {
                                        Console.WriteLine("******************************************************************************");
                                        Console.WriteLine(webconsoleline.Substring("##[section]".Length));
                                        Console.WriteLine("******************************************************************************");
                                    } else {
                                        Console.WriteLine($"\x1b[{(int)color[e.timelineId] + 30}m|\x1b[0m {webconsoleline}");
                                    }
                                }
                                Console.ForegroundColor = old;
                            }
                            if(line == "event: timeline") {
                                var e = JsonConvert.DeserializeObject<TimeLineEvent>(data);
                                timelineRecords[e.timelineId] = e.timeline;
                                if(!recordId.ContainsKey(e.timelineId)) {
                                    recordId[e.timelineId] = Guid.Empty;
                                    color[e.timelineId] = (ConsoleColor) col + 1;
                                    col = (col + 1) % 14;
                                }
                                while(pending.Count > 0) {
                                    var e2 = pending[0];
                                    if(recordId[e.timelineId] != e2.record.StepId) {
                                        List<TimelineRecord> tr = null;
                                        if(recordId[e.timelineId] != Guid.Empty && timelineRecords.TryGetValue(e.timelineId, out tr)) {
                                            var record = tr.Find(r => r.Id == recordId[e.timelineId]);
                                            if(record == null || !record.Result.HasValue) {
                                                break;
                                            }
                                            Console.WriteLine($"\x1b[{(int)color[e.timelineId] + 30}m[{tr[0].Name}] \x1b[0m{record.Result.Value.ToString()}: {record.Name}");
                                        }
                                        recordId[e.timelineId] = e2.record.StepId;
                                        if(tr != null || timelineRecords.TryGetValue(e.timelineId, out tr)) {
                                            var record = tr.Find(r => r.Id == recordId[e.timelineId]);
                                            Console.WriteLine($"\x1b[{(int)color[e.timelineId] + 30}m[{tr[0].Name}] \x1b[0mRunning: {record.Name}");
                                        }
                                    }
                                    var old = Console.ForegroundColor;
                                    Console.ForegroundColor = color[e.timelineId];
                                    foreach (var webconsoleline in e2.record.Value) {
                                        if(webconsoleline.StartsWith("##[section]")) {
                                            Console.WriteLine("******************************************************************************");
                                            Console.WriteLine(webconsoleline.Substring("##[section]".Length));
                                            Console.WriteLine("******************************************************************************");
                                        } else {
                                            Console.WriteLine($"\x1b[{(int)color[e.timelineId] + 30}m|\x1b[0m {webconsoleline}");
                                        }
                                    }
                                    Console.ForegroundColor = old;
                                    pending.RemoveAt(0);
                                }
                                if(recordId[e.timelineId] != Guid.Empty && timelineRecords != null) {
                                    var record = e.timeline.Find(r => r.Id == recordId[e.timelineId]);
                                    if(record != null && record.Result.HasValue) {
                                        Console.WriteLine($"\x1b[{(int)color[e.timelineId] + 30}m[{e.timeline[0].Name}] \x1b[0m{record.Result.Value.ToString()}: {record.Name}");
                                    }
                                }
                                if(timelineRecords.Count >= rj && timelineRecords.Values.All(r => r[0].State == TimelineRecordState.Completed)) {
                                    var b3 = new UriBuilder(b.ToString());
                                    query = new QueryBuilder();
                                    query.Add("repo", hr.repo);
                                    query.Add("runid", hr.run_id.ToString());
                                    query.Add("depending", "1");
                                    b3.Query = query.ToString().TrimStart('?');
                                    b3.Path = "runner/host/_apis/v1/Message";
                                    var sr2 = await client.GetStringAsync(b3.ToString());
                                    List<Job> jobs2 = JsonConvert.DeserializeObject<List<Job>>(sr2);
                                    if(jobs2?.Count > 0) {
                                        continue;
                                    }
                                    sr2 = await client.GetStringAsync(b2.ToString());
                                    jobs2 = JsonConvert.DeserializeObject<List<Job>>(sr2);
                                    if(jobs2.Count > jobs.Count) {
                                        foreach(var j in jobs2) {
                                            if(j.errors == null || j.errors.Count == 0) {
                                                continue;
                                            }
                                            bool cont = false;
                                            foreach(var j2 in jobs) {
                                                if(j.JobId == j2.JobId) {
                                                    cont = true;
                                                    break;
                                                }
                                            }
                                            if(cont) {
                                                continue;
                                            }
                                            hasErrors = true;
                                            foreach(var error in j.errors) {
                                                Console.Error.WriteLine($"Error: {error}");
                                            }
                                        }
                                        jobs = jobs2;
                                    }
                                    var nc = jobs2.Count(j => !(j.errors?.Count > 0));
                                    if(nc > rj) {
                                        rj = nc;
                                        if(timelineRecords.Count < rj) {
                                            continue;
                                        }
                                    }
                                    return !hasErrors && timelineRecords.Values.All(r => r[0].Result == TaskResult.Succeeded || r[0].Result == TaskResult.SucceededWithIssues || r[0].Result == TaskResult.Skipped) ? 0 : 1;
                                }
                            }
                        }
                    }
                } catch (Exception except) {
                    Console.WriteLine($"Failed to connect to Server {server}, make shure the server is running on that address or port: {except.Message}, {except.StackTrace}");
                    return 1;
                }
                Console.WriteLine($"Job request sent to {server}");
                return 0;
            };

            rootCommand.Handler = CommandHandler.Create(handler);

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }
    }
}

// Console.WriteLine("Try starting Runner.Server, Please wait...");
//                     var proc = Process.StartProcess("Runner.Server", $"--server {server}");
//                     proc.EnableRaisingEvents = true;