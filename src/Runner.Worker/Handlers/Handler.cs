using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Handlers
{
    public interface IHandler : IRunnerService
    {
        Pipelines.ActionStepDefinitionReference Action { get; set; }
        Dictionary<string, string> Environment { get; set; }
        IExecutionContext ExecutionContext { get; set; }
        Variables RuntimeVariables { get; set; }
        IStepHost StepHost { get; set; }
        Dictionary<string, string> Inputs { get; set; }
        string ActionDirectory { get; set; }
        Task RunAsync(ActionRunStage stage);
        void PrintActionDetails(ActionRunStage stage);
    }

    public abstract class Handler : RunnerService
    {
#if OS_WINDOWS
        // In windows OS the maximum supported size of a environment variable value is 32k.
        // You can set environment variable greater then 32K, but that variable will not be able to read in node.exe.
        private const int _environmentVariableMaximumSize = 32766;
#endif

        protected IActionCommandManager ActionCommandManager { get; private set; }

        public Pipelines.ActionStepDefinitionReference Action { get; set; }
        public Dictionary<string, string> Environment { get; set; }
        public Variables RuntimeVariables { get; set; }
        public IExecutionContext ExecutionContext { get; set; }
        public IStepHost StepHost { get; set; }
        public Dictionary<string, string> Inputs { get; set; }
        public string ActionDirectory { get; set; }

        public virtual void PrintActionDetails(ActionRunStage stage)
        {
            if (stage == ActionRunStage.Post)
            {
                ExecutionContext.Output($"Post job cleanup.");
                return;
            }

            string groupName = "";
            if (Action.Type == Pipelines.ActionSourceType.ContainerRegistry)
            {
                var registryAction = Action as Pipelines.ContainerRegistryReference;
                groupName = $"Run docker://{registryAction.Image}";
            }
            else if (Action.Type == Pipelines.ActionSourceType.Repository)
            {
                var repoAction = Action as Pipelines.RepositoryPathReference;
                if (string.Equals(repoAction.RepositoryType, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
                {
                    groupName = $"Run {repoAction.Path}";
                }
                else
                {
                    if (string.IsNullOrEmpty(repoAction.Path))
                    {
                        groupName = $"Run {repoAction.Name}@{repoAction.Ref}";
                    }
                    else
                    {
                        groupName = $"Run {repoAction.Name}/{repoAction.Path}@{repoAction.Ref}";
                    }
                }
            }
            else
            {
                // this should never happen
                Trace.Error($"Can't generate default folding group name for action {Action.Type.ToString()}");
                groupName = "Action details";
            }

            ExecutionContext.Output($"##[group]{groupName}");

            if (this.Inputs?.Count > 0)
            {
                ExecutionContext.Output("with:");
                foreach (var input in this.Inputs)
                {
                    if (!string.IsNullOrEmpty(input.Value))
                    {
                        ExecutionContext.Output($"  {input.Key}: {input.Value}");
                    }
                }
            }

            if (this.Environment?.Count > 0)
            {
                ExecutionContext.Output("env:");
                foreach (var env in this.Environment)
                {
                    ExecutionContext.Output($"  {env.Key}: {env.Value}");
                }
            }

            ExecutionContext.Output("##[endgroup]");
        }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            ActionCommandManager = hostContext.CreateService<IActionCommandManager>();
        }

        protected void AddInputsToEnvironment()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Inputs, nameof(Inputs));

            // Add the inputs to the environment variable dictionary.
            foreach (KeyValuePair<string, string> pair in Inputs)
            {
                AddEnvironmentVariable(
                    key: $"INPUT_{pair.Key?.Replace(' ', '_').ToUpperInvariant()}",
                    value: pair.Value);
            }
        }

        protected void AddEnvironmentVariable(string key, string value)
        {
            ArgUtil.NotNullOrEmpty(key, nameof(key));
            Trace.Verbose($"Setting env '{key}' to '{value}'.");

            Environment[key] = value ?? string.Empty;

#if OS_WINDOWS
            if (Environment[key].Length > _environmentVariableMaximumSize)
            {
                ExecutionContext.Warning($"Environment variable '{key}' exceeds the maximum supported length. Environment variable length: {value.Length} , Maximum supported length: {_environmentVariableMaximumSize}");
            }
#endif
        }

        protected void AddPrependPathToEnvironment()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext.Global.PrependPath, nameof(ExecutionContext.Global.PrependPath));
            if (ExecutionContext.Global.PrependPath.Count == 0)
            {
                return;
            }

            // Prepend path.
            string prepend = string.Join(Path.PathSeparator.ToString(), ExecutionContext.Global.PrependPath.Reverse<string>());
            var containerStepHost = StepHost as ContainerStepHost;
            if (containerStepHost != null)
            {
                containerStepHost.PrependPath = prepend;
            }
            else
            {
                string taskEnvPATH;
                Environment.TryGetValue(Constants.PathVariable, out taskEnvPATH);
                string originalPath = RuntimeVariables.Get(Constants.PathVariable) ?? // Prefer a job variable.
                    taskEnvPATH ?? // Then a task-environment variable.
                    System.Environment.GetEnvironmentVariable(Constants.PathVariable) ?? // Then an environment variable.
                    string.Empty;
                string newPath = PathUtil.PrependPath(prepend, originalPath);
                AddEnvironmentVariable(Constants.PathVariable, newPath);
            }
        }
    }
}
