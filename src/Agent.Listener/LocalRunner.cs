using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(LocalRunner))]
    public interface ILocalRunner : IAgentService
    {
        Task<int> RunAsync(CommandSettings command, CancellationToken token);
    }

    public sealed class LocalRunner : AgentService, ILocalRunner
    {
        private readonly Dictionary<string, TaskDefinition> _queryCache = new Dictionary<string, TaskDefinition>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, List<TaskDefinition>> _availableTasks;
        private TaskAgentHttpClient _httpClient;
        private ITerminal _term;

        public sealed override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = hostContext.GetService<ITerminal>();
        }

        public async Task<int> RunAsync(CommandSettings command, CancellationToken token)
        {
            Trace.Info(nameof(RunAsync));
            var configStore = HostContext.GetService<IConfigurationStore>();
            AgentSettings settings = configStore.GetSettings();

            // Store the HTTP client.
            // todo: fix in master to allow URL to be empty and then rebase on master.
            const string DefaultUrl = "http://127.0.0.1/local-runner-default-url";
            string url = command.GetUrl(DefaultUrl);
            if (!string.Equals(url, DefaultUrl, StringComparison.Ordinal))
            {
                var credentialManager = HostContext.GetService<ICredentialManager>();
                string authType = command.GetAuth(defaultValue: Constants.Configuration.Integrated);
                ICredentialProvider provider = credentialManager.GetCredentialProvider(authType);
                provider.EnsureCredential(HostContext, command, url);
                _httpClient = new TaskAgentHttpClient(new Uri(url), provider.GetVssCredentials(HostContext));
            }

            // Load the YAML file.
            string yamlFile = command.GetYaml();
            ArgUtil.File(yamlFile, nameof(yamlFile));
            var parseOptions = new ParseOptions
            {
                MaxFiles = 10,
                MustacheEvaluationMaxResultLength = 512 * 1024, // 512k string length
                MustacheEvaluationTimeout = TimeSpan.FromSeconds(10),
                MustacheMaxDepth = 5,
            };
            var pipelineParser = new PipelineParser(new PipelineTraceWriter(), new PipelineFileProvider(), parseOptions);
            Pipelines.Process process = pipelineParser.Load(
                defaultRoot: Directory.GetCurrentDirectory(),
                path: yamlFile,
                mustacheContext: null,
                cancellationToken: HostContext.AgentShutdownToken);
            ArgUtil.NotNull(process, nameof(process));
            if (command.WhatIf)
            {
                return Constants.Agent.ReturnCode.Success;
            }

            // Create job message.
            IJobDispatcher jobDispatcher = null;
            try
            {
                jobDispatcher = HostContext.CreateService<IJobDispatcher>();
                foreach (JobInfo job in await ConvertToJobMessagesAsync(process, token))
                {
                    job.RequestMessage.Environment.Variables[Constants.Variables.Agent.RunMode] = RunMode.Local.ToString();
                    jobDispatcher.Run(job.RequestMessage);
                    Task jobDispatch = jobDispatcher.WaitAsync(token);
                    if (!Task.WaitAll(new[] { jobDispatch }, job.Timeout))
                    {
                        jobDispatcher.Cancel(job.CancelMessage);

                        // Finish waiting on the same job dispatch task. The first call to WaitAsync dequeues
                        // the dispatch task and then proceeds to wait on it. So we need to continue awaiting
                        // the task instance (queue is now empty).
                        await jobDispatch;
                    }
                }
            }
            finally
            {
                if (jobDispatcher != null)
                {
                    await jobDispatcher.ShutdownAsync();
                }
            }

            return Constants.Agent.ReturnCode.Success;
        }

        private async Task<List<JobInfo>> ConvertToJobMessagesAsync(Pipelines.Process process, CancellationToken token)
        {
            var jobs = new List<JobInfo>();
            int requestId = 1;
            foreach (Phase phase in process.Phases ?? new List<IPhase>(0))
            {
                foreach (Job job in phase.Jobs ?? new List<IJob>(0))
                {
                    var builder = new StringBuilder();
                    builder.Append($@"{{
  ""tasks"": [");
                    var steps = new List<ISimpleStep>();
                    foreach (IStep step in job.Steps ?? new List<IStep>(0))
                    {
                        if (step is ISimpleStep)
                        {
                            steps.Add(step as ISimpleStep);
                        }
                        else
                        {
                            var stepsPhase = step as StepsPhase;
                            foreach (ISimpleStep nestedStep in stepsPhase.Steps ?? new List<ISimpleStep>(0))
                            {
                                steps.Add(nestedStep);
                            }
                        }
                    }

                    bool firstStep = true;
                    foreach (ISimpleStep step in steps)
                    {
                        if (!(step is TaskStep))
                        {
                            throw new Exception("Unable to run step type: " + step.GetType().FullName);
                        }

                        var task = step as TaskStep;
                        if (!task.Enabled)
                        {
                            continue;
                        }

                        TaskDefinition definition = await GetDefinitionAsync(task, token);
                        if (!firstStep)
                        {
                            builder.Append(",");
                        }

                        firstStep = false;
                        builder.Append($@"
    {{
      ""instanceId"": ""{Guid.NewGuid()}"",
      ""displayName"": {JsonConvert.ToString(!string.IsNullOrEmpty(task.Name) ? task.Name : definition.InstanceNameFormat)},
      ""enabled"": true,
      ""continueOnError"": {task.ContinueOnError.ToString().ToLowerInvariant()},
      ""condition"": {JsonConvert.ToString(task.Condition)},
      ""alwaysRun"": false,
      ""timeoutInMinutes"": {task.TimeoutInMinutes.ToString(CultureInfo.InvariantCulture)},
      ""id"": ""{definition.Id}"",
      ""name"": {JsonConvert.ToString(definition.Name)},
      ""version"": {JsonConvert.ToString(GetVersion(definition).ToString())},
      ""inputs"": {{");
                        bool firstInput = true;
                        foreach (KeyValuePair<string, string> input in task.Inputs ?? new Dictionary<string, string>(0))
                        {
                            if (!firstInput)
                            {
                                builder.Append(",");
                            }

                            firstInput = false;
                            builder.Append($@"
        {JsonConvert.ToString(input.Key)}: {JsonConvert.ToString(input.Value)}");
                        }

                        builder.Append($@"
      }},
      ""environment"": {{");
                        bool firstEnv = true;
                        foreach (KeyValuePair<string, string> env in task.Environment ?? new Dictionary<string, string>(0))
                        {
                            if (!firstEnv)
                            {
                                builder.Append(",");
                            }

                            firstEnv = false;
                            builder.Append($@"
        {JsonConvert.ToString(env.Key)}: {JsonConvert.ToString(env.Value)}");
                        }
                        builder.Append($@"
      }}
    }}");
                    }

                    builder.Append($@"
  ],
  ""requestId"": {requestId++},
  ""lockToken"": ""00000000-0000-0000-0000-000000000000"",
  ""lockedUntil"": ""0001-01-01T00:00:00"",
  ""messageType"": ""JobRequest"",
  ""plan"": {{
    ""scopeIdentifier"": ""00000000-0000-0000-0000-000000000000"",
    ""planType"": ""Build"",
    ""version"": 8,
    ""planId"": ""00000000-0000-0000-0000-000000000000"",
    ""artifactUri"": ""vstfs:///Build/Build/1234"",
    ""artifactLocation"": null
  }},
  ""timeline"": {{
    ""id"": ""00000000-0000-0000-0000-000000000000"",
    ""changeId"": 1,
    ""location"": null
  }},
  ""jobId"": ""{Guid.NewGuid()}"",
  ""jobName"": {JsonConvert.ToString(!string.IsNullOrEmpty(job.Name) ? job.Name : "Build")},
  ""environment"": {{
    ""endpoints"": [
      {{
        ""data"": {{
          ""repositoryId"": ""00000000-0000-0000-0000-000000000000"",
          ""rootFolder"": null,
          ""clean"": ""false"",
          ""checkoutSubmodules"": ""False"",
          ""onpremtfsgit"": ""False"",
          ""fetchDepth"": ""0"",
          ""gitLfsSupport"": ""false"",
          ""skipSyncSource"": ""true"",
          ""cleanOptions"": ""0""
        }},
        ""name"": ""gitTest"",
        ""type"": ""TfsGit"",
        ""url"": ""https://127.0.0.1/vsts-agent-local-runner/_git/gitTest"",
        ""authorization"": {{
          ""parameters"": {{
            ""AccessToken"": ""dummy-access-token""
          }},
          ""scheme"": ""OAuth""
        }},
        ""isReady"": false
      }}
    ],
    ""mask"": [
      {{
        ""type"": ""regex"",
        ""value"": ""dummy-access-token""
      }}
    ],
    ""variables"": {{");
                    builder.Append($@"
      ""system"": ""build"",
      ""system.collectionId"": ""00000000-0000-0000-0000-000000000000"",
      ""system.teamProject"": ""gitTest"",
      ""system.teamProjectId"": ""00000000-0000-0000-0000-000000000000"",
      ""system.definitionId"": ""55"",
      ""build.definitionName"": ""My Build Definition Name"",
      ""build.definitionVersion"": ""1"",
      ""build.queuedBy"": ""John Doe"",
      ""build.queuedById"": ""00000000-0000-0000-0000-000000000000"",
      ""build.requestedFor"": ""John Doe"",
      ""build.requestedForId"": ""00000000-0000-0000-0000-000000000000"",
      ""build.requestedForEmail"": ""john.doe@contoso.com"",
      ""build.sourceVersion"": ""55ba1763b74d42a758514b466b7ea931aedbc941"",
      ""build.sourceBranch"": ""refs/heads/master"",
      ""build.sourceBranchName"": ""master"",
      ""system.culture"": ""en-US"",
      ""build.clean"": """",
      ""build.buildId"": ""1863"",
      ""build.buildUri"": ""vstfs:///Build/Build/1863"",
      ""build.buildNumber"": ""1863"",
      ""system.isScheduled"": ""False"",
      ""system.hosttype"": ""build"",
      ""system.teamFoundationCollectionUri"": ""https://127.0.0.1/vsts-agent-local-runner"",
      ""system.taskDefinitionsUri"": ""https://127.0.0.1/vsts-agent-local-runner"",
      ""AZURE_HTTP_USER_AGENT"": ""VSTS_00000000-0000-0000-0000-000000000000_build_55_1863"",
      ""MSDEPLOY_HTTP_USER_AGENT"": ""VSTS_00000000-0000-0000-0000-000000000000_build_55_1863"",
      ""system.planId"": ""00000000-0000-0000-0000-000000000000"",
      ""system.jobId"": ""00000000-0000-0000-0000-000000000000"",
      ""system.timelineId"": ""00000000-0000-0000-0000-000000000000"",
      ""build.repository.uri"": ""https://127.0.0.1/vsts-agent-local-runner/_git/gitTest"",
      ""build.sourceVersionAuthor"": ""John Doe"",
      ""build.sourceVersionMessage"": ""Updated Program.cs""");
                    foreach (Variable variable in job.Variables ?? new List<IVariable>(0))
                    {
                        builder.Append($@",
      {JsonConvert.ToString(variable.Name ?? string.Empty)}: {JsonConvert.ToString(variable.Value ?? string.Empty)}");
                    }

                    builder.Append($@"
    }},
    ""systemConnection"": {{
      ""data"": {{
        ""ServerId"": ""00000000-0000-0000-0000-000000000000"",
        ""ServerName"": ""127.0.0.1""
      }},
      ""name"": ""SystemVssConnection"",
      ""url"": ""https://127.0.0.1/vsts-agent-local-runner"",
      ""authorization"": {{
        ""parameters"": {{
          ""AccessToken"": ""dummy-access-token""
        }},
        ""scheme"": ""OAuth""
      }},
      ""isReady"": false
    }}
  }}
}}");
                    string message = builder.ToString();
                    try
                    {
                        jobs.Add(new JobInfo(job, message));
                    }
                    catch
                    {
                        Dump("Job message JSON", message);
                        throw;
                    }
                }
            }

            return jobs;
        }

        private async Task<TaskDefinition> GetDefinitionAsync(TaskStep task, CancellationToken token)
        {
            var available = await GetAvailableTasksAsync(token);
            ArgUtil.NotNull(task.Reference, nameof(task.Reference));
            ArgUtil.NotNullOrEmpty(task.Reference.Name, nameof(task.Reference.Name));
            List<TaskDefinition> definitions;
            if (!available.TryGetValue(task.Reference.Name, out definitions))
            {
                throw new Exception($"Unable to resolve task {task.Reference.Name}");
            }

            // Attempt to find an exact match.
            TaskDefinition match = definitions.FirstOrDefault(definition => string.Equals(GetVersion(definition).ToString(), task.Reference.Version ?? string.Empty, StringComparison.Ordinal));

            // Attempt to find the best match from the "available" cache.
            if (match == null)
            {
                ArgUtil.NotNullOrEmpty(task.Reference.Version, nameof(task.Reference.Version));
                var versionPattern = "^" + Regex.Escape(task.Reference.Version) + @"(\.[0-9]+){0,2}$";
                var versionRegex = new Regex(versionPattern);
                match = definitions.OrderByDescending(definition => GetVersion(definition))
                    .FirstOrDefault(definition => versionRegex.IsMatch(GetVersion(definition).ToString()));
            }

            if (match == null)
            {
                throw new Exception($"Unable to resolve task {task.Reference.Name}@{task.Reference.Version}");
            }

            await DownloadTaskAsync(match, token);
            return match;
        }

        private async Task<Dictionary<string, List<TaskDefinition>>> GetAvailableTasksAsync(CancellationToken token)
        {
            if (_availableTasks != null)
            {
                return _availableTasks;
            }

            // Get available tasks from the local cache.
            var allDefinitions = new List<TaskDefinition>();
            string tasksDirectory = HostContext.GetDirectory(WellKnownDirectory.Tasks);
            if (Directory.Exists(tasksDirectory))
            {
                _term.WriteLine("Getting available task versions from cache.");
                foreach (string taskDirectory in Directory.GetDirectories(tasksDirectory))
                {
                    foreach (string taskSubDirectory in Directory.GetDirectories(taskDirectory))
                    {
                        string taskJsonPath = Path.Combine(taskSubDirectory, "task.json");
                        if (File.Exists(taskJsonPath) && File.Exists(taskSubDirectory + ".completed"))
                        {
                            Trace.Info($"Loading: '{taskJsonPath}'");
                            TaskDefinition definition = IOUtil.LoadObject<TaskDefinition>(taskJsonPath);
                            if (definition == null ||
                                string.IsNullOrEmpty(definition.Name) ||
                                definition.Version == null ||
                                !string.Equals(taskSubDirectory, GetDirectory(definition), StringComparison.OrdinalIgnoreCase))
                            {
                                Trace.Info("Task definition is invalid or does not match folder structure.");
                                continue;
                            }

                            allDefinitions.Add(definition);
                        }
                    }
                }
            }

            // Get available tasks from the server.
            if (_httpClient != null)
            {
                _term.WriteLine("Getting available task versions from server.");
                allDefinitions.AddRange(await _httpClient.GetTaskDefinitionsAsync(cancellationToken: token));
                _term.WriteLine("Successfully retrieved task versions from server.");
            }

            // Categorize the task definitions by name.
            _availableTasks = new Dictionary<string, List<TaskDefinition>>(StringComparer.OrdinalIgnoreCase);
            foreach (TaskDefinition definition in allDefinitions)
            {
                List<TaskDefinition> definitions;
                if (!_availableTasks.TryGetValue(definition.Name, out definitions))
                {
                    definitions = new List<TaskDefinition>();
                    _availableTasks.Add(definition.Name, definitions);
                }

                definitions.Add(definition);
            }

            return _availableTasks;
        }

        private async Task DownloadTaskAsync(TaskDefinition task, CancellationToken token)
        {
            Trace.Entering();
            ArgUtil.NotNull(task, nameof(task));
            ArgUtil.NotNullOrEmpty(task.Version, nameof(task.Version));

            // first check to see if we already have the task
            string destDirectory = GetDirectory(task);
            Trace.Info($"Ensuring task exists: ID '{task.Id}', version '{task.Version}', name '{task.Name}', directory '{destDirectory}'.");
            if (File.Exists(destDirectory + ".completed"))
            {
                Trace.Info("Task already downloaded.");
                return;
            }

            // delete existing task folder.
            Trace.Verbose("Deleting task destination folder: {0}", destDirectory);
            IOUtil.DeleteDirectory(destDirectory, CancellationToken.None);

            // Inform the user that a download is taking place. The download could take a while if
            // the task zip is large. It would be nice to print the localized name, but it is not
            // available from the reference included in the job message.
            _term.WriteLine(StringUtil.Loc("DownloadingTask0", task.Name));
            string zipFile;
            var version = new TaskVersion(task.Version);

            //download and extract task in a temp folder and rename it on success
            string tempDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Tasks), "_temp_" + Guid.NewGuid());
            try
            {
                Directory.CreateDirectory(tempDirectory);
                zipFile = Path.Combine(tempDirectory, string.Format("{0}.zip", Guid.NewGuid()));
                //open zip stream in async mode
                using (FileStream fs = new FileStream(zipFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    using (Stream result = await _httpClient.GetTaskContentZipAsync(task.Id, version, token))
                    {
                        //81920 is the default used by System.IO.Stream.CopyTo and is under the large object heap threshold (85k). 
                        await result.CopyToAsync(fs, 81920, token);
                        await fs.FlushAsync(token);
                    }
                }

                Directory.CreateDirectory(destDirectory);
                ZipFile.ExtractToDirectory(zipFile, destDirectory);

                Trace.Verbose("Create watermark file indicate task download succeed.");
                File.WriteAllText(destDirectory + ".completed", DateTime.UtcNow.ToString());

                Trace.Info("Finished getting task.");
            }
            finally
            {
                try
                {
                    //if the temp folder wasn't moved -> wipe it
                    if (Directory.Exists(tempDirectory))
                    {
                        Trace.Verbose("Deleting task temp folder: {0}", tempDirectory);
                        IOUtil.DeleteDirectory(tempDirectory, CancellationToken.None); // Don't cancel this cleanup and should be pretty fast.
                    }
                }
                catch (Exception ex)
                {
                    //it is not critical if we fail to delete the temp folder
                    Trace.Warning("Failed to delete temp folder '{0}'. Exception: {1}", tempDirectory, ex);
                    Trace.Warning(StringUtil.Loc("FailedDeletingTempDirectory0Message1", tempDirectory, ex.Message));
                }
            }
        }

        private string GetDirectory(TaskDefinition definition)
        {
            ArgUtil.NotEmpty(definition.Id, nameof(definition.Id));
            ArgUtil.NotNull(definition.Name, nameof(definition.Name));
            ArgUtil.NotNullOrEmpty(definition.Version, nameof(definition.Version));
            return Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Tasks), $"{definition.Name}_{definition.Id}", definition.Version);
        }

        private static Version GetVersion(TaskDefinition definition)
        {
            return new Version(definition.Version.Major, definition.Version.Minor, definition.Version.Patch);
        }

        private static void Dump(string header, string value)
        {
            Console.WriteLine();
            Console.WriteLine(String.Empty.PadRight(80, '*'));
            Console.WriteLine($"* {header}");
            Console.WriteLine(String.Empty.PadRight(80, '*'));
            Console.WriteLine();
            using (StringReader reader = new StringReader(value))
            {
                int lineNumber = 1;
                string line = reader.ReadLine();
                while (line != null)
                {
                    Console.WriteLine($"{lineNumber.ToString().PadLeft(4)}: {line}");
                    line = reader.ReadLine();
                    lineNumber++;
                }
            }
        }

        private sealed class JobInfo
        {
            public JobInfo(Job job, string requestMessage)
            {
                RequestMessage = JsonUtility.FromString<AgentJobRequestMessage>(requestMessage);
                Timeout = TimeSpan.FromMinutes(job.TimeoutInMinutes ?? 60);
            }

            public JobCancelMessage CancelMessage => new JobCancelMessage(RequestMessage.JobId, TimeSpan.FromSeconds(60));

            public AgentJobRequestMessage RequestMessage { get; }

            public TimeSpan Timeout { get; }
        }

        private sealed class PipelineTraceWriter : Pipelines.ITraceWriter
        {
            public void Info(String format, params Object[] args)
            {
                Console.WriteLine(format, args);
            }

            public void Verbose(String format, params Object[] args)
            {
                Console.WriteLine(format, args);
            }
        }

        private sealed class PipelineFileProvider : Pipelines.IFileProvider
        {
            public FileData GetFile(String path)
            {
                return new FileData
                {
                    Name = Path.GetFileName(path),
                    Directory = Path.GetDirectoryName(path),
                    Content = File.ReadAllText(path),
                };
            }

            public String ResolvePath(String defaultRoot, String path)
            {
                return Path.Combine(defaultRoot, path);
            }
        }
    }
}