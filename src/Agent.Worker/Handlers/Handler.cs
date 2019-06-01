using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    public interface IHandler : IAgentService
    {
        List<ServiceEndpoint> Endpoints { get; set; }
        Dictionary<string, string> Environment { get; set; }
        IExecutionContext ExecutionContext { get; set; }
        Variables RuntimeVariables { get; set; }
        IStepHost StepHost { get; set; }
        Dictionary<string, string> Inputs { get; set; }
        List<SecureFile> SecureFiles { get; set; }
        string TaskDirectory { get; set; }
        Pipelines.TaskStepDefinitionReference Task { get; set; }
        Task RunAsync();
    }

    public abstract class Handler : AgentService
    {
#if OS_WINDOWS
        // In windows OS the maximum supported size of a environment variable value is 32k.
        // You can set environment variable greater then 32K, but that variable will not be able to read in node.exe.
        private const int _environmentVariableMaximumSize = 32766;
#endif

        protected IActionCommandManager ActionCommandManager { get; private set; }

        public List<ServiceEndpoint> Endpoints { get; set; }
        public Dictionary<string, string> Environment { get; set; }
        public Variables RuntimeVariables { get; set; }
        public IExecutionContext ExecutionContext { get; set; }
        public IStepHost StepHost { get; set; }
        public Dictionary<string, string> Inputs { get; set; }
        public List<SecureFile> SecureFiles { get; set; }
        public string TaskDirectory { get; set; }
        public Pipelines.TaskStepDefinitionReference Task { get; set; }

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
                ExecutionContext.Warning(StringUtil.Loc("EnvironmentVariableExceedsMaximumLength", key, value.Length, _environmentVariableMaximumSize));
            }
#endif
        }

        protected void AddPrependPathToEnvironment()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext.PrependPath, nameof(ExecutionContext.PrependPath));
            if (ExecutionContext.PrependPath.Count == 0)
            {
                return;
            }

            // Prepend path.
            string prepend = string.Join(Path.PathSeparator.ToString(), ExecutionContext.PrependPath.Reverse<string>());
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
