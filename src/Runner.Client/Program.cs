using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Net.Http;
using System.Text;
using GitHub.DistributedTask.WebApi;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;

namespace Runner.Client
{
    class Program
    {

        private class HookResponse {
            public string repo {get;set;}
            public long run_id {get;set;}
            public bool skipped {get;set;}
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
            public string timelineId {get;set;}
            public List<TimelineRecord> timeline {get;set;}
        }

        private class WebConsoleEvent {
            public string timelineId {get;set;}
            public Guid recordId {get;set;}
            public TimelineRecordFeedLinesWrapper record {get;set;}
        }

        static int Main(string[] args)
        {
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
                    description: "Which event to send to a worker")
            };

            rootCommand.Description = "Send events to your runner";

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<string, string, string, string>(async (workflow, server, payload, Event) =>
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
                    var b2 = new UriBuilder(b.ToString());
                    query = new QueryBuilder();
                    query.Add("repo", hr.repo);
                    query.Add("runid", hr.run_id.ToString());
                    b2.Query = query.ToString();
                    b2.Path = "runner/host/_apis/v1/Message";
                    var sr = await client.GetStringAsync(b2.ToString());
                    List<Job> jobs = JsonConvert.DeserializeObject<List<Job>>(sr);
                    foreach(Job j in jobs) {
                        Console.WriteLine($"Running Job: {j.name}");
                        if(j.errors?.Count > 0) {
                            foreach (var error in j.errors) {
                                Console.Error.WriteLine($"Error: {error}");
                            }
                            continue;
                        }
                        var eventstream = await client.GetStreamAsync(server + $"/runner/server/_apis/v1/TimeLineWebConsoleLog?timelineId={j.TimeLineId.ToString()}");
                        List<TimelineRecord> timelineRecords = null;
                        try {
                            var content = await client.GetStringAsync(server + $"/runner/server/_apis/v1/Timeline/{j.TimeLineId.ToString()}");
                            timelineRecords = JsonConvert.DeserializeObject<List<TimelineRecord>>(content);
                        }
                        catch (HttpRequestException) {
                            Console.WriteLine("No Timeline found, wait for a timeline event");
                        }
                        Guid recordId = Guid.Empty;
                        List<WebConsoleEvent> pending = new List<WebConsoleEvent>();
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
                                    if(recordId != e.record.StepId) {
                                        if(recordId != Guid.Empty && timelineRecords != null) {
                                            var record = timelineRecords.Find(r => r.Id == recordId);
                                            if(record == null || !record.Result.HasValue) {
                                                pending.Add(e);
                                                continue;
                                            }
                                            // if(!record.Result.HasValue) {
                                            //     var content = await client.GetStringAsync(server + $"/runner/server/_apis/v1/Timeline/{j.TimeLineId.ToString()}");
                                            //     timelineRecords = JsonConvert.DeserializeObject<List<TimelineRecord>>(content);
                                            //     record = timelineRecords.Find(r => r.Id == recordId);
                                            // }
                                            Console.WriteLine($"{record.Result.Value.ToString()}: {record.Name}");
                                            // switch(record.Result.Value) {
                                            //     case TaskResult.Succeeded:
                                            //     break;
                                            // }
                                        }
                                        recordId = e.record.StepId;
                                        if(recordId != Guid.Empty && timelineRecords != null) {
                                            var record = timelineRecords.Find(r => r.Id == recordId);
                                            Console.WriteLine($"Running: {record.Name}");
                                        }
                                    }
                                    foreach (var webconsoleline in e.record.Value) {
                                        if(webconsoleline.StartsWith("##[section]")) {
                                            Console.WriteLine("******************************************************************************");
                                            Console.WriteLine(webconsoleline.Substring("##[section]".Length));
                                            Console.WriteLine("******************************************************************************");
                                        } else {
                                            Console.WriteLine(webconsoleline);
                                        }
                                    }
                                }
                                if(line == "event: timeline") {
                                    var e = JsonConvert.DeserializeObject<TimeLineEvent>(data);
                                    timelineRecords = e.timeline;
                                    while(pending.Count > 0) {
                                        var e2 = pending[0];
                                        if(recordId != e2.record.StepId) {
                                            if(recordId != Guid.Empty && timelineRecords != null) {
                                                var record = timelineRecords.Find(r => r.Id == recordId);
                                                if(record == null || !record.Result.HasValue) {
                                                    break;
                                                }
                                                Console.WriteLine($"{record.Result.Value.ToString()}: {record.Name}");
                                            }
                                            recordId = e2.record.StepId;
                                            if(recordId != Guid.Empty && timelineRecords != null) {
                                                var record = timelineRecords.Find(r => r.Id == recordId);
                                                Console.WriteLine($"Running: {record.Name}");
                                            }
                                        }
                                        foreach (var webconsoleline in e2.record.Value) {
                                            if(webconsoleline.StartsWith("##[section]")) {
                                                Console.WriteLine("******************************************************************************");
                                                Console.WriteLine(webconsoleline.Substring("##[section]".Length));
                                                Console.WriteLine("******************************************************************************");
                                            } else {
                                                Console.WriteLine(webconsoleline);
                                            }
                                        }
                                        pending.RemoveAt(0);
                                    }
                                    if(e.timeline[0].State == TimelineRecordState.Completed) {
                                        if(recordId != Guid.Empty && timelineRecords != null) {
                                        var record = timelineRecords.Find(r => r.Id == recordId);
                                            if(record == null || !record.Result.HasValue) {
                                                break;
                                            }
                                            Console.WriteLine($"{record.Result.Value.ToString()}: {record.Name}");
                                        }
                                        return 0;
                                    }
                                }
                            }
                        }
                        // Console.WriteLine(j.TimeLineId);
                    }
                } catch (Exception except) {
                    Console.WriteLine($"Failed to connect to Server {server}, make shure the server is running on that address or port: {except.Message}, {except.StackTrace}");
                    return 1;
                }
                Console.WriteLine($"Job request sent to {server}");
                return 0;
            });

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }
    }
}

// Console.WriteLine("Try starting Runner.Server, Please wait...");
//                     var proc = Process.StartProcess("Runner.Server", $"--server {server}");
//                     proc.EnableRaisingEvents = true;