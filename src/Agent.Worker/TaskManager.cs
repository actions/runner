using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(TaskManager))]
    public interface ITaskManager : IAgentService
    {
        Task DownloadAsync(IExecutionContext executionContext, IEnumerable<TaskInstance> tasks);

        Definition Load(TaskReference task);
    }

    public sealed class TaskManager : AgentService, ITaskManager
    {
        private async Task DownloadAsync(IExecutionContext executionContext, TaskReference task)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(task, nameof(task));
            ArgUtil.NotNullOrEmpty(task.Version, nameof(task.Version));
            var jobServer = HostContext.GetService<IJobServer>();

            // first check to see if we already have the task
            string destDirectory = GetDirectory(task);
            Trace.Info($"Ensuring task exists: ID '{task.Id}', version '{task.Version}', name '{task.Name}', directory '{destDirectory}'.");
            if (Directory.Exists(destDirectory))
            {
                Trace.Info("Task already downloaded.");
                return;
            }

            Trace.Info("Getting task.");
            string zipFile;
            var version = new TaskVersion(task.Version);

            //download and extract task in a temp folder and rename it on success
            string tempDirectory = Path.Combine(IOUtil.GetTasksPath(HostContext), "_temp_" + Guid.NewGuid());
            try
            {
                Directory.CreateDirectory(tempDirectory);
                zipFile = Path.Combine(tempDirectory, string.Format("{0}.zip", Guid.NewGuid()));
                //open zip stream in async mode
                using (FileStream fs = new FileStream(zipFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    using (Stream result = await jobServer.GetTaskContentZipAsync(task.Id, version, executionContext.CancellationToken))
                    {
                        //81920 is the default used by System.IO.Stream.CopyTo and is under the large object heap threshold (85k). 
                        await result.CopyToAsync(fs, 81920, executionContext.CancellationToken);
                        await fs.FlushAsync(executionContext.CancellationToken);
                    }
                }

                ZipFile.ExtractToDirectory(zipFile, tempDirectory);
                File.Delete(zipFile);
                Directory.CreateDirectory(Path.GetDirectoryName(destDirectory));
                Directory.Move(tempDirectory, destDirectory);
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

        private string GetDirectory(TaskReference task)
        {
            ArgUtil.NotEmpty(task.Id, nameof(task.Id));
            ArgUtil.NotNull(task.Name, nameof(task.Name));
            ArgUtil.NotNullOrEmpty(task.Version, nameof(task.Version));
            return Path.Combine(
                IOUtil.GetTasksPath(HostContext),
                $"{task.Name}_{task.Id}",
                task.Version);
        }

        public async Task DownloadAsync(IExecutionContext executionContext, IEnumerable<TaskInstance> tasks)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(tasks, nameof(tasks));

            //remove duplicate and disabled tasks
            IEnumerable<TaskInstance> uniqueTasks =
                from task in tasks
                where task.Enabled
                group task by new
                    {
                        task.Id,
                        task.Version
                    }
                into taskGrouping
                select taskGrouping.First();
            foreach (TaskInstance task in uniqueTasks)
            {
                await DownloadAsync(executionContext, task);
            }
        }

        public Definition Load(TaskReference task)
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(task, nameof(task));

            // Initialize the definition wrapper object.
            var definition = new Definition() { Directory = GetDirectory(task) };

            // Deserialize the JSON.
            string file = Path.Combine(definition.Directory, Constants.Path.TaskJsonFile);
            Trace.Info($"Loading task definition '{file}'.");
            string json = File.ReadAllText(file);
            definition.Data = JsonConvert.DeserializeObject<DefinitionData>(json);

            // Replace the macros within the handler data sections.
            // TODO: Do other macros need to be replaced within the handler section? Currently only the $(currentdirectory) macro is handled. Or does the handler need to deal with that.
            foreach (HandlerData handlerData in (definition.Data?.Execution?.All as IEnumerable<HandlerData> ?? new HandlerData[0]))
            {
                handlerData?.ReplaceMacros(definition);
            }

            return definition;
        }
    }

    public sealed class Definition
    {
        public DefinitionData Data { get; set; }
        public string Directory { get; set; }
    }

    public sealed class DefinitionData
    {
        public TaskInputDefinition[] Inputs { get; set; }
        public ExecutionData Execution { get; set; }
    }

    public sealed class ExecutionData
    {
        private readonly List<HandlerData> _all = new List<HandlerData>();
        private NodeHandlerData _node;
        private ProcessHandlerData _process;

        [JsonIgnore]
        public List<HandlerData> All => _all;

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
        public static readonly string CurrentDirectoryMacro = "$(currentdirectory)";

        public string[] Platforms { get; set; }
        [JsonIgnore]
        public abstract int Priority { get; }
        public string Target { get; set; }

        public bool PreferredOnCurrentPlatform()
        {
#if OS_WINDOWS
            const string CurrentPlatform = "windows";
            return Platforms?.Any(x => string.Equals(x, CurrentPlatform, StringComparison.OrdinalIgnoreCase)) ?? false;
#else
            return false;
#endif
        }

        public virtual void ReplaceMacros(Definition definition)
        {
            Target = Replace(input: Target, macro: CurrentDirectoryMacro, replacement: definition.Directory);
        }

        protected static string Replace(string input, string macro, string replacement)
        {
            // Validate or coalesce args.
            input = input ?? string.Empty;
            ArgUtil.NotNullOrEmpty(macro, nameof(macro));
            replacement = replacement ?? string.Empty;

            // Bump the start index with each replacement to prevent recursive replacement.
            int startIndex = 0;
            int matchIndex;
            while (startIndex < input.Length &&
                (matchIndex = input.IndexOf(macro, startIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                input = string.Concat(
                    input.Substring(0, matchIndex),
                    replacement,
                    input.Substring(matchIndex + macro.Length));
                startIndex = matchIndex + replacement.Length;
            }

            return input;
        }
    }

    public sealed class NodeHandlerData : HandlerData
    {
        public override int Priority => 1;
        public string WorkingDirectory { get; set; }

        public override void ReplaceMacros(Definition definition)
        {
            base.ReplaceMacros(definition);
            WorkingDirectory = Replace(input: WorkingDirectory, macro: CurrentDirectoryMacro, replacement: definition.Directory);
        }
    }

    // TODO: PowerShell3
    // TODO: PowerShell
    // TODO: AzurePowerShell
    // TODO: PowerShellExe

    public sealed class ProcessHandlerData : HandlerData
    {
        public string ArgumentFormat { get; set; }
        public override int Priority => 6;
        public string WorkingDirectory { get; set; }

        public override void ReplaceMacros(Definition definition)
        {
            base.ReplaceMacros(definition);
            ArgumentFormat = Replace(input: ArgumentFormat, macro: CurrentDirectoryMacro, replacement: definition.Directory);
            WorkingDirectory = Replace(input: WorkingDirectory, macro: CurrentDirectoryMacro, replacement: definition.Directory);
        }
    }
}
