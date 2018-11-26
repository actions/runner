using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(TaskManager))]
    public interface ITaskManager : IAgentService
    {
        Task DownloadAsync(IExecutionContext executionContext, IEnumerable<Pipelines.JobStep> steps);

        Definition Load(Pipelines.TaskStep task);
    }

    public sealed class TaskManager : AgentService, ITaskManager
    {
        private const int _defaultFileStreamBufferSize = 4096;

        //81920 is the default used by System.IO.Stream.CopyTo and is under the large object heap threshold (85k). 
        private const int _defaultCopyBufferSize = 81920;

        public async Task DownloadAsync(IExecutionContext executionContext, IEnumerable<Pipelines.JobStep> steps)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(steps, nameof(steps));

            executionContext.Output(StringUtil.Loc("EnsureTasksExist"));

            IEnumerable<Pipelines.TaskStep> tasks = steps.OfType<Pipelines.TaskStep>();

            //remove duplicate, disabled and built-in tasks
            IEnumerable<Pipelines.TaskStep> uniqueTasks =
                from task in tasks
                group task by new
                {
                    task.Reference.Id,
                    task.Reference.Version
                }
                into taskGrouping
                select taskGrouping.First();

            if (uniqueTasks.Count() == 0)
            {
                executionContext.Debug("There is no required tasks need to download.");
                return;
            }

            foreach (var task in uniqueTasks.Select(x => x.Reference))
            {
                if (task.Id == Pipelines.PipelineConstants.CheckoutTask.Id && task.Version == Pipelines.PipelineConstants.CheckoutTask.Version)
                {
                    Trace.Info("Skip download checkout task.");
                    continue;
                }
                await DownloadAsync(executionContext, task);
            }
        }

        public Definition Load(Pipelines.TaskStep task)
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(task, nameof(task));

            if (task.Reference.Id == Pipelines.PipelineConstants.CheckoutTask.Id && task.Reference.Version == Pipelines.PipelineConstants.CheckoutTask.Version)
            {
                var checkoutTask = new Definition()
                {
                    Directory = HostContext.GetDirectory(WellKnownDirectory.Tasks),
                    Data = new DefinitionData()
                    {
                        Author = Pipelines.PipelineConstants.CheckoutTask.Author,
                        Description = Pipelines.PipelineConstants.CheckoutTask.Description,
                        FriendlyName = Pipelines.PipelineConstants.CheckoutTask.FriendlyName,
                        HelpMarkDown = Pipelines.PipelineConstants.CheckoutTask.HelpMarkDown,
                        Inputs = Pipelines.PipelineConstants.CheckoutTask.Inputs.ToArray(),
                        Execution = StringUtil.ConvertFromJson<ExecutionData>(StringUtil.ConvertToJson(Pipelines.PipelineConstants.CheckoutTask.Execution)),
                        PostJobExecution = StringUtil.ConvertFromJson<ExecutionData>(StringUtil.ConvertToJson(Pipelines.PipelineConstants.CheckoutTask.PostJobExecution))
                    }
                };

                return checkoutTask;
            }

            // Initialize the definition wrapper object.
            var definition = new Definition() { Directory = GetDirectory(task.Reference) };

            // Deserialize the JSON.
            string file = Path.Combine(definition.Directory, Constants.Path.TaskJsonFile);
            Trace.Info($"Loading task definition '{file}'.");
            string json = File.ReadAllText(file);
            definition.Data = JsonConvert.DeserializeObject<DefinitionData>(json);

            // Replace the macros within the handler data sections.
            foreach (HandlerData handlerData in (definition.Data?.Execution?.All as IEnumerable<HandlerData> ?? new HandlerData[0]))
            {
                handlerData?.ReplaceMacros(HostContext, definition);
            }

            return definition;
        }

        private async Task DownloadAsync(IExecutionContext executionContext, Pipelines.TaskStepDefinitionReference task)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(task, nameof(task));
            ArgUtil.NotNullOrEmpty(task.Version, nameof(task.Version));
            var taskServer = HostContext.GetService<ITaskServer>();

            // first check to see if we already have the task
            string destDirectory = GetDirectory(task);
            Trace.Info($"Ensuring task exists: ID '{task.Id}', version '{task.Version}', name '{task.Name}', directory '{destDirectory}'.");
            if (File.Exists(destDirectory + ".completed"))
            {
                executionContext.Debug($"Task '{task.Name}' already downloaded at '{destDirectory}'.");
                return;
            }

            // delete existing task folder.
            Trace.Verbose("Deleting task destination folder: {0}", destDirectory);
            IOUtil.DeleteDirectory(destDirectory, CancellationToken.None);

            // Inform the user that a download is taking place. The download could take a while if
            // the task zip is large. It would be nice to print the localized name, but it is not
            // available from the reference included in the job message.
            executionContext.Output(StringUtil.Loc("DownloadingTask0", task.Name));
            string zipFile = string.Empty;
            var version = new TaskVersion(task.Version);

            //download and extract task in a temp folder and rename it on success
            string tempDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Tasks), "_temp_" + Guid.NewGuid());
            try
            {
                Directory.CreateDirectory(tempDirectory);
                int retryCount = 0;

                // Allow up to 20 * 60s for any task to be downloaded from service. 
                // Base on Kusto, the longest we have on the service today is over 850 seconds.
                // Timeout limit can be overwrite by environment variable
                if (!int.TryParse(Environment.GetEnvironmentVariable("VSTS_TASK_DOWNLOAD_TIMEOUT") ?? string.Empty, out int timeoutSeconds))
                {
                    timeoutSeconds = 20 * 60;
                }

                while (retryCount < 3)
                {
                    using (var taskDownloadTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                    using (var taskDownloadCancellation = CancellationTokenSource.CreateLinkedTokenSource(taskDownloadTimeout.Token, executionContext.CancellationToken))
                    {
                        try
                        {
                            zipFile = Path.Combine(tempDirectory, string.Format("{0}.zip", Guid.NewGuid()));

                            //open zip stream in async mode
                            using (FileStream fs = new FileStream(zipFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: _defaultFileStreamBufferSize, useAsync: true))
                            using (Stream result = await taskServer.GetTaskContentZipAsync(task.Id, version, taskDownloadCancellation.Token))
                            {
                                await result.CopyToAsync(fs, _defaultCopyBufferSize, taskDownloadCancellation.Token);
                                await fs.FlushAsync(taskDownloadCancellation.Token);

                                // download succeed, break out the retry loop.
                                break;
                            }
                        }
                        catch (OperationCanceledException) when (executionContext.CancellationToken.IsCancellationRequested)
                        {
                            Trace.Info($"Task download has been cancelled.");
                            throw;
                        }
                        catch (Exception ex) when (retryCount < 2)
                        {
                            retryCount++;
                            Trace.Error($"Fail to download task '{task.Id} ({task.Name}/{task.Version})' -- Attempt: {retryCount}");
                            Trace.Error(ex);
                            if (taskDownloadTimeout.Token.IsCancellationRequested)
                            {
                                // task download didn't finish within timeout
                                executionContext.Warning(StringUtil.Loc("TaskDownloadTimeout", task.Name, timeoutSeconds));
                            }
                            else
                            {
                                executionContext.Warning(StringUtil.Loc("TaskDownloadFailed", task.Name, ex.Message));
                            }
                        }
                    }

                    if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSTS_TASK_DOWNLOAD_NO_BACKOFF")))
                    {
                        var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
                        executionContext.Warning($"Back off {backOff.TotalSeconds} seconds before retry.");
                        await Task.Delay(backOff);
                    }
                }

                Directory.CreateDirectory(destDirectory);
                ZipFile.ExtractToDirectory(zipFile, destDirectory);

                Trace.Verbose("Create watermark file indicate task download succeed.");
                File.WriteAllText(destDirectory + ".completed", DateTime.UtcNow.ToString());

                executionContext.Debug($"Task '{task.Name}' has been downloaded into '{destDirectory}'.");
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
                    executionContext.Warning(StringUtil.Loc("FailedDeletingTempDirectory0Message1", tempDirectory, ex.Message));
                }
            }
        }

        private string GetDirectory(Pipelines.TaskStepDefinitionReference task)
        {
            ArgUtil.NotEmpty(task.Id, nameof(task.Id));
            ArgUtil.NotNull(task.Name, nameof(task.Name));
            ArgUtil.NotNullOrEmpty(task.Version, nameof(task.Version));
            return Path.Combine(
                HostContext.GetDirectory(WellKnownDirectory.Tasks),
                $"{task.Name}_{task.Id}",
                task.Version);
        }
    }

    public sealed class Definition
    {
        public DefinitionData Data { get; set; }
        public string Directory { get; set; }
    }

    public sealed class DefinitionData
    {
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public string HelpMarkDown { get; set; }
        public string Author { get; set; }
        public OutputVariable[] OutputVariables { get; set; }
        public TaskInputDefinition[] Inputs { get; set; }
        public ExecutionData PreJobExecution { get; set; }
        public ExecutionData Execution { get; set; }
        public ExecutionData PostJobExecution { get; set; }
    }

    public sealed class OutputVariable
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public sealed class ExecutionData
    {
        private readonly List<HandlerData> _all = new List<HandlerData>();
        private AzurePowerShellHandlerData _azurePowerShell;
        private NodeHandlerData _node;
        private PowerShellHandlerData _powerShell;
        private PowerShell3HandlerData _powerShell3;
        private PowerShellExeHandlerData _powerShellExe;
        private ProcessHandlerData _process;
        private AgentPluginHandlerData _agentPlugin;

        [JsonIgnore]
        public List<HandlerData> All => _all;

#if !OS_WINDOWS || X86
        [JsonIgnore]
#endif
        public AzurePowerShellHandlerData AzurePowerShell
        {
            get
            {
                return _azurePowerShell;
            }

            set
            {
                _azurePowerShell = value;
                Add(value);
            }
        }

        public NodeHandlerData Node
        {
            get
            {
                return _node;
            }

            set
            {
                _node = value;
                Add(value);
            }
        }

#if !OS_WINDOWS || X86
        [JsonIgnore]
#endif
        public PowerShellHandlerData PowerShell
        {
            get
            {
                return _powerShell;
            }

            set
            {
                _powerShell = value;
                Add(value);
            }
        }

#if !OS_WINDOWS
        [JsonIgnore]
#endif
        public PowerShell3HandlerData PowerShell3
        {
            get
            {
                return _powerShell3;
            }

            set
            {
                _powerShell3 = value;
                Add(value);
            }
        }

#if !OS_WINDOWS
        [JsonIgnore]
#endif
        public PowerShellExeHandlerData PowerShellExe
        {
            get
            {
                return _powerShellExe;
            }

            set
            {
                _powerShellExe = value;
                Add(value);
            }
        }

#if !OS_WINDOWS
        [JsonIgnore]
#endif
        public ProcessHandlerData Process
        {
            get
            {
                return _process;
            }

            set
            {
                _process = value;
                Add(value);
            }
        }

        public AgentPluginHandlerData AgentPlugin
        {
            get
            {
                return _agentPlugin;
            }

            set
            {
                _agentPlugin = value;
                Add(value);
            }
        }

        private void Add(HandlerData data)
        {
            if (data != null)
            {
                _all.Add(data);
            }
        }
    }

    public abstract class HandlerData
    {
        public Dictionary<string, string> Inputs { get; }

        public string[] Platforms { get; set; }

        [JsonIgnore]
        public abstract int Priority { get; }

        public string Target
        {
            get
            {
                return GetInput(nameof(Target));
            }

            set
            {
                SetInput(nameof(Target), value);
            }
        }

        public HandlerData()
        {
            Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool PreferredOnCurrentPlatform()
        {
#if OS_WINDOWS
            const string CurrentPlatform = "windows";
            return Platforms?.Any(x => string.Equals(x, CurrentPlatform, StringComparison.OrdinalIgnoreCase)) ?? false;
#else
            return false;
#endif
        }

        public void ReplaceMacros(IHostContext context, Definition definition)
        {
            var handlerVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            handlerVariables["currentdirectory"] = definition.Directory;
            VarUtil.ExpandValues(context, source: handlerVariables, target: Inputs);
        }

        protected string GetInput(string name)
        {
            string value;
            if (Inputs.TryGetValue(name, out value))
            {
                return value ?? string.Empty;
            }

            return string.Empty;
        }

        protected void SetInput(string name, string value)
        {
            Inputs[name] = value;
        }
    }

    public sealed class NodeHandlerData : HandlerData
    {
        public override int Priority => 1;

        public string WorkingDirectory
        {
            get
            {
                return GetInput(nameof(WorkingDirectory));
            }

            set
            {
                SetInput(nameof(WorkingDirectory), value);
            }
        }
    }

    public sealed class PowerShell3HandlerData : HandlerData
    {
        public override int Priority => 2;
    }

    public sealed class PowerShellHandlerData : HandlerData
    {
        public string ArgumentFormat
        {
            get
            {
                return GetInput(nameof(ArgumentFormat));
            }

            set
            {
                SetInput(nameof(ArgumentFormat), value);
            }
        }

        public override int Priority => 3;

        public string WorkingDirectory
        {
            get
            {
                return GetInput(nameof(WorkingDirectory));
            }

            set
            {
                SetInput(nameof(WorkingDirectory), value);
            }
        }
    }

    public sealed class AzurePowerShellHandlerData : HandlerData
    {
        public string ArgumentFormat
        {
            get
            {
                return GetInput(nameof(ArgumentFormat));
            }

            set
            {
                SetInput(nameof(ArgumentFormat), value);
            }
        }

        public override int Priority => 4;

        public string WorkingDirectory
        {
            get
            {
                return GetInput(nameof(WorkingDirectory));
            }

            set
            {
                SetInput(nameof(WorkingDirectory), value);
            }
        }
    }

    public sealed class PowerShellExeHandlerData : HandlerData
    {
        public string ArgumentFormat
        {
            get
            {
                return GetInput(nameof(ArgumentFormat));
            }

            set
            {
                SetInput(nameof(ArgumentFormat), value);
            }
        }

        public string FailOnStandardError
        {
            get
            {
                return GetInput(nameof(FailOnStandardError));
            }

            set
            {
                SetInput(nameof(FailOnStandardError), value);
            }
        }

        public string InlineScript
        {
            get
            {
                return GetInput(nameof(InlineScript));
            }

            set
            {
                SetInput(nameof(InlineScript), value);
            }
        }

        public override int Priority => 5;

        public string ScriptType
        {
            get
            {
                return GetInput(nameof(ScriptType));
            }

            set
            {
                SetInput(nameof(ScriptType), value);
            }
        }

        public string WorkingDirectory
        {
            get
            {
                return GetInput(nameof(WorkingDirectory));
            }

            set
            {
                SetInput(nameof(WorkingDirectory), value);
            }
        }
    }

    public sealed class ProcessHandlerData : HandlerData
    {
        public string ArgumentFormat
        {
            get
            {
                return GetInput(nameof(ArgumentFormat));
            }

            set
            {
                SetInput(nameof(ArgumentFormat), value);
            }
        }

        public string ModifyEnvironment
        {
            get
            {
                return GetInput(nameof(ModifyEnvironment));
            }

            set
            {
                SetInput(nameof(ModifyEnvironment), value);
            }
        }

        public override int Priority => 6;

        public string WorkingDirectory
        {
            get
            {
                return GetInput(nameof(WorkingDirectory));
            }

            set
            {
                SetInput(nameof(WorkingDirectory), value);
            }
        }
    }

    public sealed class AgentPluginHandlerData : HandlerData
    {
        public override int Priority => 0;
    }
}
