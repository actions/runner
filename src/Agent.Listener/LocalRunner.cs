using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Yaml = Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml;
using YamlContracts = Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    [ServiceLocator(Default = typeof(LocalRunner))]
    public interface ILocalRunner : IAgentService
    {
        Task<int> LocalRunAsync(CommandSettings command, CancellationToken token);
    }

    public sealed class LocalRunner : AgentService, ILocalRunner
    {
        private string _gitPath;
        private ITaskStore _taskStore;
        private ITerminal _term;

        public sealed override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _taskStore = HostContext.GetService<ITaskStore>();
            _term = hostContext.GetService<ITerminal>();
        }

        public async Task<int> LocalRunAsync(CommandSettings command, CancellationToken token)
        {
            Trace.Info(nameof(LocalRunAsync));

            // Warn preview.
            _term.WriteLine("This command is currently in preview. The interface and behavior will change in a future version.");
            if (!command.Unattended)
            {
                _term.WriteLine("Press Enter to continue.");
                _term.ReadLine();
            }

            HostContext.RunMode = RunMode.Local;

            // Resolve the YAML file path.
            string ymlFile = command.GetYml();
            if (string.IsNullOrEmpty(ymlFile))
            {
                string[] ymlFiles =
                    Directory.GetFiles(Directory.GetCurrentDirectory())
                    .Where((string filePath) =>
                    {
                        return filePath.EndsWith(".yml", IOUtil.FilePathStringComparison);
                    })
                    .ToArray();
                if (ymlFiles.Length > 1)
                {
                    throw new Exception($"More than one .yml file exists in the current directory. Specify which file to use via the --'{Constants.Agent.CommandLine.Args.Yml}' command line argument.");
                }

                ymlFile = ymlFiles.FirstOrDefault();
            }

            if (string.IsNullOrEmpty(ymlFile))
            {
                throw new Exception($"Unable to find a .yml file in the current directory. Specify which file to use via the --'{Constants.Agent.CommandLine.Args.Yml}' command line argument.");
            }

            // Load the YAML file.
            var parseOptions = new ParseOptions
            {
                MaxFiles = 10,
                MustacheEvaluationMaxResultLength = 512 * 1024, // 512k string length
                MustacheEvaluationTimeout = TimeSpan.FromSeconds(10),
                MustacheMaxDepth = 5,
            };
            var pipelineParser = new PipelineParser(new PipelineTraceWriter(), new PipelineFileProvider(), parseOptions);
            if (command.WhatIf)
            {
                pipelineParser.DeserializeAndSerialize(
                    defaultRoot: Directory.GetCurrentDirectory(),
                    path: ymlFile,
                    mustacheContext: null,
                    cancellationToken: HostContext.AgentShutdownToken);
                return Constants.Agent.ReturnCode.Success;
            }

            YamlContracts.Process process = pipelineParser.LoadInternal(
                defaultRoot: Directory.GetCurrentDirectory(),
                path: ymlFile,
                mustacheContext: null,
                cancellationToken: HostContext.AgentShutdownToken);
            ArgUtil.NotNull(process, nameof(process));

            // Verify the current directory is the root of a git repo.
            string repoDirectory = Directory.GetCurrentDirectory();
            if (!Directory.Exists(Path.Combine(repoDirectory, ".git")))
            {
                throw new Exception("Unable to run the build locally. The command must be executed from the root directory of a local git repository.");
            }

            // Verify at least one phase was found.
            if (process.Phases == null || process.Phases.Count == 0)
            {
                throw new Exception($"No phases or steps were discovered from the file: '{ymlFile}'");
            }

            // Filter the phases.
            string phaseName = command.GetPhase();
            if (!string.IsNullOrEmpty(phaseName))
            {
                process.Phases = process.Phases
                    .Cast<YamlContracts.Phase>()
                    .Where(x => string.Equals(x.Name, phaseName, StringComparison.OrdinalIgnoreCase))
                    .Cast<YamlContracts.IPhase>()
                    .ToList();
                if (process.Phases.Count == 0)
                {
                    throw new Exception($"Phase '{phaseName}' not found.");
                }
            }

            // Verify a phase was specified if more than one phase was found.
            if (process.Phases.Count > 1)
            {
                throw new Exception($"More than one phase was discovered. Use the --{Constants.Agent.CommandLine.Args.Phase} argument to specify a phase.");
            }

            // Get the matrix.
            var phase = process.Phases[0] as YamlContracts.Phase;
            var queueTarget = phase.Target as QueueTarget;

            // Filter to a specific matrix.
            string matrixName = command.GetMatrix();
            if (!string.IsNullOrEmpty(matrixName))
            {
                if (queueTarget?.Matrix != null)
                {
                    queueTarget.Matrix = queueTarget.Matrix.Keys
                        .Where(x => string.Equals(x, matrixName, StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(keySelector: x => x, elementSelector: x => queueTarget.Matrix[x]);
                }

                if (queueTarget?.Matrix == null || queueTarget.Matrix.Count == 0)
                {
                    throw new Exception($"Job configuration matrix '{matrixName}' not found.");
                }
            }

            // Verify a matrix was specified if more than one matrix was found.
            if (queueTarget?.Matrix != null && queueTarget.Matrix.Count > 1)
            {
                throw new Exception($"More than one job configuration matrix was discovered. Use the --{Constants.Agent.CommandLine.Args.Matrix} argument to specify a matrix.");
            }

            // Get the URL - required if missing tasks.
            string url = command.GetUrl(suppressPromptIfEmpty: true);
            if (string.IsNullOrEmpty(url))
            {
                if (!TestAllTasksCached(process, token))
                {
                    url = command.GetUrl(suppressPromptIfEmpty: false);
                }
            }

            if (!string.IsNullOrEmpty(url))
            {
                // Initialize and store the HTTP client.
                var credentialManager = HostContext.GetService<ICredentialManager>();

                // Get the auth type. On premise defaults to negotiate (Kerberos with fallback to NTLM).
                // Hosted defaults to PAT authentication.
                string defaultAuthType = UrlUtil.IsHosted(url) ? Constants.Configuration.PAT :
                    (Constants.Agent.Platform == Constants.OSPlatform.Windows ? Constants.Configuration.Integrated : Constants.Configuration.Negotiate);
                string authType = command.GetAuth(defaultValue: defaultAuthType);
                ICredentialProvider provider = credentialManager.GetCredentialProvider(authType);
                provider.EnsureCredential(HostContext, command, url);
                _taskStore.HttpClient = new TaskAgentHttpClient(new Uri(url), provider.GetVssCredentials(HostContext));
            }

            var configStore = HostContext.GetService<IConfigurationStore>();
            AgentSettings settings = configStore.GetSettings();

            // Create job message.
            JobInfo job = (await ConvertToJobMessagesAsync(process, repoDirectory, token)).Single();
            IJobDispatcher jobDispatcher = null;
            try
            {
                jobDispatcher = HostContext.CreateService<IJobDispatcher>();
                job.RequestMessage.Environment.Variables[Constants.Variables.Agent.RunMode] = RunMode.Local.ToString();
                jobDispatcher.Run(job.RequestMessage);
                Task jobDispatch = jobDispatcher.WaitAsync(token);
                if (!Task.WaitAll(new[] { jobDispatch }, job.Timeout))
                {
                    jobDispatcher.Cancel(job.CancelMessage);

                    // Finish waiting on the job dispatch task. The call to jobDispatcher.WaitAsync dequeues
                    // the job dispatch task. In the cancel flow, we need to continue awaiting the task instance
                    // (queue is now empty).
                    await jobDispatch;
                }

                // Translate the job result to an agent return code.
                TaskResult jobResult = jobDispatcher.GetLocalRunJobResult(job.RequestMessage);
                switch (jobResult)
                {
                    case TaskResult.Succeeded:
                    case TaskResult.SucceededWithIssues:
                        return Constants.Agent.ReturnCode.Success;
                    default:
                        return Constants.Agent.ReturnCode.TerminatedError;
                }
            }
            finally
            {
                if (jobDispatcher != null)
                {
                    await jobDispatcher.ShutdownAsync();
                }
            }
        }

        private async Task<List<JobInfo>> ConvertToJobMessagesAsync(YamlContracts.Process process, string repoDirectory, CancellationToken token)
        {
            // Collect info about the repo.
            string repoName = Path.GetFileName(repoDirectory);
            string userName = await GitAsync("config --get user.name", token);
            string userEmail = await GitAsync("config --get user.email", token);
            string branch = await GitAsync("symbolic-ref HEAD", token);
            string commit = await GitAsync("rev-parse HEAD", token);
            string commitAuthorName = await GitAsync("show --format=%an --no-patch HEAD", token);
            string commitSubject = await GitAsync("show --format=%s --no-patch HEAD", token);

            var jobs = new List<JobInfo>();
            int requestId = 1;
            foreach (Phase phase in process.Phases ?? new List<IPhase>(0))
            {
                IDictionary<string, IDictionary<string, string>> matrix;
                var queueTarget = phase.Target as QueueTarget;
                if (queueTarget?.Matrix != null && queueTarget.Matrix.Count > 0)
                {
                    // Get the matrix.
                    matrix = queueTarget.Matrix;
                }
                else
                {
                    // Create the default matrix.
                    matrix = new Dictionary<string, IDictionary<string, string>>(1);
                    matrix[string.Empty] = new Dictionary<string, string>(0);
                }

                foreach (string jobName in matrix.Keys)
                {
                    var builder = new StringBuilder();
                    builder.Append($@"{{
  ""tasks"": [");
                    var steps = new List<ISimpleStep>();
                    foreach (IStep step in phase.Steps ?? new List<IStep>(0))
                    {
                        if (step is ISimpleStep)
                        {
                            steps.Add(step as ISimpleStep);
                        }
                        else
                        {
                            var stepGroup = step as StepGroup;
                            foreach (ISimpleStep nestedStep in stepGroup.Steps ?? new List<ISimpleStep>(0))
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

                        // Sanity check - the pipeline parser should have already validated version is an int.
                        int taskVersion;
                        if (!int.TryParse(task.Reference.Version, NumberStyles.None, CultureInfo.InvariantCulture, out taskVersion))
                        {
                            throw new Exception($"Unexpected task version format. Expected an unsigned integer with no formatting. Actual: '{taskVersion}'");
                        }

                        TaskDefinition definition = await _taskStore.GetTaskAsync(
                            name: task.Reference.Name,
                            version: taskVersion,
                            token: token);
                        await _taskStore.EnsureCachedAsync(definition, token);
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
      ""version"": {JsonConvert.ToString(definition.Version.ToString())},
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
  ""jobName"": {JsonConvert.ToString(!string.IsNullOrEmpty(phase.Name) ? phase.Name : "Build")},
  ""environment"": {{
    ""endpoints"": [
      {{
        ""data"": {{
          ""repositoryId"": ""00000000-0000-0000-0000-000000000000"",
          ""localDirectory"": {JsonConvert.ToString(repoDirectory)},
          ""clean"": ""false"",
          ""checkoutSubmodules"": ""False"",
          ""onpremtfsgit"": ""False"",
          ""fetchDepth"": ""0"",
          ""gitLfsSupport"": ""false"",
          ""skipSyncSource"": ""false"",
          ""cleanOptions"": ""0""
        }},
        ""name"": {JsonConvert.ToString(repoName)},
        ""type"": ""LocalRun"",
        ""url"": ""https://127.0.0.1/vsts-agent-local-runner?directory={Uri.EscapeDataString(repoDirectory)}"",
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
      ""system.culture"": ""en-US"",
      ""system.definitionId"": ""55"",
      ""system.isScheduled"": ""False"",
      ""system.hosttype"": ""build"",
      ""system.jobId"": ""00000000-0000-0000-0000-000000000000"",
      ""system.planId"": ""00000000-0000-0000-0000-000000000000"",
      ""system.timelineId"": ""00000000-0000-0000-0000-000000000000"",
      ""system.taskDefinitionsUri"": ""https://127.0.0.1/vsts-agent-local-runner"",
      ""system.teamFoundationCollectionUri"": ""https://127.0.0.1/vsts-agent-local-runner"",
      ""system.teamProject"": {JsonConvert.ToString(repoName)},
      ""system.teamProjectId"": ""00000000-0000-0000-0000-000000000000"",
      ""build.buildId"": ""1863"",
      ""build.buildNumber"": ""1863"",
      ""build.buildUri"": ""vstfs:///Build/Build/1863"",
      ""build.clean"": """",
      ""build.definitionName"": ""My Build Definition Name"",
      ""build.definitionVersion"": ""1"",
      ""build.queuedBy"": {JsonConvert.ToString(userName)},
      ""build.queuedById"": ""00000000-0000-0000-0000-000000000000"",
      ""build.requestedFor"": {JsonConvert.ToString(userName)},
      ""build.requestedForEmail"": {JsonConvert.ToString(userEmail)},
      ""build.requestedForId"": ""00000000-0000-0000-0000-000000000000"",
      ""build.repository.uri"": ""https://127.0.0.1/vsts-agent-local-runner/_git/{Uri.EscapeDataString(repoName)}"",
      ""build.sourceBranch"": {JsonConvert.ToString(branch)},
      ""build.sourceBranchName"": {JsonConvert.ToString(branch.Split('/').Last())},
      ""build.sourceVersion"": {JsonConvert.ToString(commit)},
      ""build.sourceVersionAuthor"": {JsonConvert.ToString(commitAuthorName)},
      ""build.sourceVersionMessage"": {JsonConvert.ToString(commitSubject)},
      ""AZURE_HTTP_USER_AGENT"": ""VSTS_00000000-0000-0000-0000-000000000000_build_55_1863"",
      ""MSDEPLOY_HTTP_USER_AGENT"": ""VSTS_00000000-0000-0000-0000-000000000000_build_55_1863""");
                    foreach (Variable variable in phase.Variables ?? new List<IVariable>(0))
                    {
                        builder.Append($@",
      {JsonConvert.ToString(variable.Name ?? string.Empty)}: {JsonConvert.ToString(variable.Value ?? string.Empty)}");
                    }

                    foreach (KeyValuePair<string, string> variable in matrix[jobName])
                    {
                        builder.Append($@",
      {JsonConvert.ToString(variable.Key ?? string.Empty)}: {JsonConvert.ToString(variable.Value ?? string.Empty)}");
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
                        jobs.Add(new JobInfo(phase, jobName, message));
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

        private async Task<string> GitAsync(string arguments, CancellationToken token)
        {
            // Resolve the location of git.
            if (_gitPath == null)
            {
#if OS_WINDOWS
                _gitPath = Path.Combine(IOUtil.GetExternalsPath(), "git", "cmd", $"git{IOUtil.ExeExtension}");
                ArgUtil.File(_gitPath, nameof(_gitPath));
#else
                var whichUtil = HostContext.GetService<IWhichUtil>();
                _gitPath = whichUtil.Which("git", require: true);
#endif
            }

            // Prepare the environment variables to overlay.
            var overlayEnvironment = new Dictionary<string, string>(StringComparer.Ordinal);
            overlayEnvironment["GIT_TERMINAL_PROMPT"] = "0";
            // Skip any GIT_TRACE variable since GIT_TRACE will affect ouput from every git command.
            // This will fail the parse logic for detect git version, remote url, etc.
            // Ex. 
            //      SET GIT_TRACE=true
            //      git version 
            //      11:39:58.295959 git.c:371               trace: built-in: git 'version'
            //      git version 2.11.1.windows.1
            IDictionary currentEnvironment = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry entry in currentEnvironment)
            {
                string key = entry.Key as string ?? string.Empty;
                if (string.Equals(key, "GIT_TRACE", StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith("GIT_TRACE_", StringComparison.OrdinalIgnoreCase))
                {
                    overlayEnvironment[key] = string.Empty;
                }
            }

            // Run git and return the output from the streams.
            var output = new StringBuilder();
            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            Console.WriteLine();
            Console.WriteLine($"git {arguments}");
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                output.AppendLine(message.Data);
                Console.WriteLine(message.Data);
            };
            processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                output.AppendLine(message.Data);
                Console.WriteLine(message.Data);
            };
#if OS_WINDOWS
            Encoding encoding = Encoding.UTF8;
#else
            Encoding encoding = null;
#endif
            await processInvoker.ExecuteAsync(
                workingDirectory: Directory.GetCurrentDirectory(),
                fileName: _gitPath,
                arguments: arguments,
                environment: overlayEnvironment,
                requireExitCodeZero: true,
                outputEncoding: encoding,
                cancellationToken: token);

            string result = output.ToString().Trim();
            ArgUtil.NotNullOrEmpty(result, nameof(result));
            return result;
        }

        private bool TestAllTasksCached(Process process, CancellationToken token)
        {
            foreach (Phase phase in process.Phases ?? new List<IPhase>(0))
            {
                var steps = new List<ISimpleStep>();
                foreach (IStep step in phase.Steps ?? new List<IStep>(0))
                {
                    if (step is ISimpleStep)
                    {
                        if (!(step is CheckoutStep))
                        {
                            steps.Add(step as ISimpleStep);
                        }
                    }
                    else
                    {
                        var stepGroup = step as StepGroup;
                        foreach (ISimpleStep nestedStep in stepGroup.Steps ?? new List<ISimpleStep>(0))
                        {
                            steps.Add(nestedStep);
                        }
                    }
                }

                foreach (ISimpleStep step in steps)
                {
                    if (!(step is TaskStep))
                    {
                        throw new Exception("Unexpected step type: " + step.GetType().FullName);
                    }

                    var task = step as TaskStep;
                    if (!task.Enabled)
                    {
                        continue;
                    }

                    // Sanity check - the pipeline parser should have already validated version is an int.
                    int taskVersion;
                    if (!int.TryParse(task.Reference.Version, NumberStyles.None, CultureInfo.InvariantCulture, out taskVersion))
                    {
                        throw new Exception($"Unexpected task version format. Expected an unsigned integer with no formmatting. Actual: '{task.Reference.Version}'");
                    }

                    if (!_taskStore.TestCached(name: task.Reference.Name, version: taskVersion, token: token))
                    {
                        return false;
                    }
                }
            }

            return true;
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

        [ServiceLocator(Default = typeof(TaskStore))]
        private interface ITaskStore : IAgentService
        {
            TaskAgentHttpClient HttpClient { get; set; }

            Task EnsureCachedAsync(TaskDefinition task, CancellationToken token);
            Task<TaskDefinition> GetTaskAsync(string name, int version, CancellationToken token);
            bool TestCached(string name, int version, CancellationToken token);
        }

        private sealed class TaskStore : AgentService, ITaskStore
        {
            private List<TaskDefinition> _localTasks;
            private List<TaskDefinition> _serverTasks;
            private ITerminal _term;

            public TaskAgentHttpClient HttpClient { get; set; }

            public sealed override void Initialize(IHostContext hostContext)
            {
                base.Initialize(hostContext);
                _term = hostContext.GetService<ITerminal>();
            }

            public async Task EnsureCachedAsync(TaskDefinition task, CancellationToken token)
            {
                Trace.Entering();
                ArgUtil.NotNull(task, nameof(task));
                ArgUtil.NotNullOrEmpty(task.Version, nameof(task.Version));

                // first check to see if we already have the task
                string destDirectory = GetTaskDirectory(task);
                Trace.Info($"Ensuring task exists: ID '{task.Id}', version '{task.Version}', name '{task.Name}', directory '{destDirectory}'.");
                if (File.Exists(destDirectory + ".completed"))
                {
                    Trace.Info("Task already downloaded.");
                    return;
                }

                // Invalidate the local cache.
                _localTasks = null;

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
                        using (Stream result = await HttpClient.GetTaskContentZipAsync(task.Id, version, token))
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

            public async Task<TaskDefinition> GetTaskAsync(string name, int version, CancellationToken token)
            {
                if (HttpClient != null)
                {
                    return (await GetServerTaskAsync(name, version, token));
                }

                return GetLocalTask(name, version, require: true, token: token);
            }

            public bool TestCached(string name, int version, CancellationToken token)
            {
                TaskDefinition localTask = GetLocalTask(name, version, require: false, token: token);
                return localTask != null;
            }

            private TaskDefinition GetLocalTask(string name, int version, bool require, CancellationToken token)
            {
                if (_localTasks == null)
                {
                    // Get tasks from the local cache.
                    var tasks = new List<TaskDefinition>();
                    string tasksDirectory = HostContext.GetDirectory(WellKnownDirectory.Tasks);
                    if (Directory.Exists(tasksDirectory))
                    {
                        _term.WriteLine("Getting available tasks from the cache.");
                        foreach (string taskDirectory in Directory.GetDirectories(tasksDirectory))
                        {
                            foreach (string taskSubDirectory in Directory.GetDirectories(taskDirectory))
                            {
                                string taskJsonPath = Path.Combine(taskSubDirectory, "task.json");
                                if (File.Exists(taskJsonPath) && File.Exists(taskSubDirectory + ".completed"))
                                {
                                    token.ThrowIfCancellationRequested();
                                    Trace.Info($"Loading: '{taskJsonPath}'");
                                    TaskDefinition definition = IOUtil.LoadObject<TaskDefinition>(taskJsonPath);
                                    if (definition == null ||
                                        string.IsNullOrEmpty(definition.Name) ||
                                        definition.Version == null)
                                    {
                                        _term.WriteLine($"Task definition is invalid. The name property must not be empty and the version property must not be null. Task definition: {taskJsonPath}");
                                        continue;
                                    }
                                    else if (!string.Equals(taskSubDirectory, GetTaskDirectory(definition), IOUtil.FilePathStringComparison))
                                    {
                                        _term.WriteLine($"Task definition does not match the expected folder structure. Expected: '{GetTaskDirectory(definition)}'; actual: '{taskJsonPath}'");
                                        continue;
                                    }

                                    tasks.Add(definition);
                                }
                            }
                        }
                    }

                    _localTasks = FilterWithinMajorVersion(tasks);
                }

                return FilterByReference(_localTasks, name, version, require);
            }

            private async Task<TaskDefinition> GetServerTaskAsync(string name, int version, CancellationToken token)
            {
                ArgUtil.NotNull(HttpClient, nameof(HttpClient));
                if (_serverTasks == null)
                {
                    _term.WriteLine("Getting available task versions from server.");
                    var tasks = await HttpClient.GetTaskDefinitionsAsync(cancellationToken: token);
                    _term.WriteLine("Successfully retrieved task versions from server.");
                    _serverTasks = FilterWithinMajorVersion(tasks);
                }

                return FilterByReference(_serverTasks, name, version, require: true);
            }

            private TaskDefinition FilterByReference(List<TaskDefinition> tasks, string name, int version, bool require)
            {
                // Filter by name.
                Guid id = default(Guid);
                if (Guid.TryParseExact(name, format: "D", result: out id)) // D = 32 digits separated by hyphens
                {
                    // Filter by GUID.
                    tasks = tasks.Where(x => x.Id == id).ToList();
                }
                else
                {
                    // Filter by name.
                    tasks = tasks.Where(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Validate name is not ambiguous.
                if (tasks.GroupBy(x => x.Id).Count() > 1)
                {
                    throw new Exception($"Unable to resolve a task for the name '{name}'. The name is ambiguous.");
                }

                // Filter by version.
                tasks = tasks.Where(x => x.Version.Major == version).ToList();

                // Validate a task was found.
                if (tasks.Count == 0)
                {
                    if (require)
                    {
                        throw new Exception($"No tasks found matching: '{name}@{version}'");
                    }

                    return null;
                }

                ArgUtil.Equal(1, tasks.Count, nameof(tasks.Count));
                return tasks[0];
            }

            private List<TaskDefinition> FilterWithinMajorVersion(List<TaskDefinition> tasks)
            {
                return tasks
                    .GroupBy(x => new { Id = x.Id, MajorVersion = x.Version }) // Group by ID and major-version
                    .Select(x => x.OrderByDescending(y => y.Version).First()) // Select the max version
                    .ToList();
            }

            private string GetTaskDirectory(TaskDefinition definition)
            {
                ArgUtil.NotEmpty(definition.Id, nameof(definition.Id));
                ArgUtil.NotNull(definition.Name, nameof(definition.Name));
                ArgUtil.NotNullOrEmpty(definition.Version, nameof(definition.Version));
                return Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Tasks), $"{definition.Name}_{definition.Id}", definition.Version);
            }
        }

        private sealed class JobInfo
        {
            public JobInfo(Phase phase, string jobName, string requestMessage)
            {
                JobName = jobName ?? string.Empty;
                PhaseName = phase.Name ?? string.Empty;
                RequestMessage = JsonUtility.FromString<AgentJobRequestMessage>(requestMessage);
                string timeoutInMinutesString = (phase.Target as QueueTarget)?.TimeoutInMinutes ??
                    (phase.Target as DeploymentTarget)?.TimeoutInMinutes ??
                    "60";
                Timeout = TimeSpan.FromMinutes(int.Parse(timeoutInMinutesString, NumberStyles.None));
            }

            public JobCancelMessage CancelMessage => new JobCancelMessage(RequestMessage.JobId, TimeSpan.FromSeconds(60));

            public string JobName { get; }

            public string PhaseName { get; }

            public AgentJobRequestMessage RequestMessage { get; }

            public TimeSpan Timeout { get; }
        }

        private sealed class PipelineTraceWriter : Yaml.ITraceWriter
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

        private sealed class PipelineFileProvider : Yaml.IFileProvider
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