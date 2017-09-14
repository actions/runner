using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    public interface IHandler : IAgentService
    {
        List<ServiceEndpoint> Endpoints { get; set; }
        Dictionary<string, string> Environment { get; set; }
        IExecutionContext ExecutionContext { get; set; }
        string FilePathInputRootDirectory { get; set; }
        Dictionary<string, string> Inputs { get; set; }
        List<SecureFile> SecureFiles { get; set; }
        string TaskDirectory { get; set; }

        Task RunAsync();
    }

    public abstract class Handler : AgentService
    {
        protected IWorkerCommandManager CommandManager { get; private set; }

        public List<ServiceEndpoint> Endpoints { get; set; }
        public Dictionary<string, string> Environment { get; set; }
        public IExecutionContext ExecutionContext { get; set; }
        public string FilePathInputRootDirectory { get; set; }
        public Dictionary<string, string> Inputs { get; set; }
        public List<SecureFile> SecureFiles { get; set; }
        public string TaskDirectory { get; set; }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            CommandManager = hostContext.GetService<IWorkerCommandManager>();
        }

        protected void AddEndpointsToEnvironment()
        {
            Trace.Entering();
            ArgUtil.NotNull(Endpoints, nameof(Endpoints));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(ExecutionContext.Endpoints, nameof(ExecutionContext.Endpoints));

            List<ServiceEndpoint> endpoints;
            if ((ExecutionContext.Variables.GetBoolean(Constants.Variables.Agent.AllowAllEndpoints) ?? false) ||
                string.Equals(System.Environment.GetEnvironmentVariable("AGENT_ALLOWALLENDPOINTS") ?? string.Empty, bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                endpoints = ExecutionContext.Endpoints; // todo: remove after sprint 120 or so
            }
            else
            {
                endpoints = Endpoints;
            }

            // Add the endpoints to the environment variable dictionary.
            foreach (ServiceEndpoint endpoint in endpoints)
            {
                ArgUtil.NotNull(endpoint, nameof(endpoint));

                string partialKey = null;
                if (endpoint.Id != Guid.Empty)
                {
                    partialKey = endpoint.Id.ToString();
                }
                else if (string.Equals(endpoint.Name, ServiceEndpoints.SystemVssConnection, StringComparison.OrdinalIgnoreCase))
                {
                    partialKey = ServiceEndpoints.SystemVssConnection.ToUpperInvariant();
                }
                else if (endpoint.Data == null ||
                    !endpoint.Data.TryGetValue(WellKnownEndpointData.RepositoryId, out partialKey) ||
                    string.IsNullOrEmpty(partialKey))
                {
                    continue; // This should never happen.
                }

                AddEnvironmentVariable(
                    key: $"ENDPOINT_URL_{partialKey}",
                    value: endpoint.Url?.ToString());
                AddEnvironmentVariable(
                    key: $"ENDPOINT_AUTH_{partialKey}",
                    // Note, JsonUtility.ToString will not null ref if the auth object is null.
                    value: JsonUtility.ToString(endpoint.Authorization));
                if (endpoint.Authorization != null && endpoint.Authorization.Scheme != null)
                {
                    AddEnvironmentVariable(
                        key: $"ENDPOINT_AUTH_SCHEME_{partialKey}",
                        value: endpoint.Authorization.Scheme);

                    foreach (KeyValuePair<string, string> pair in endpoint.Authorization.Parameters)
                    {
                        AddEnvironmentVariable(
                            key: $"ENDPOINT_AUTH_PARAMETER_{partialKey}_{pair.Key?.Replace(' ', '_').ToUpperInvariant()}",
                            value: pair.Value);
                    }
                }
                if (endpoint.Id != Guid.Empty)
                {
                    AddEnvironmentVariable(
                        key: $"ENDPOINT_DATA_{partialKey}",
                        // Note, JsonUtility.ToString will not null ref if the data object is null.
                        value: JsonUtility.ToString(endpoint.Data));

                    if (endpoint.Data != null)
                    {
                        foreach (KeyValuePair<string, string> pair in endpoint.Data)
                        {
                            AddEnvironmentVariable(
                                key: $"ENDPOINT_DATA_{partialKey}_{pair.Key?.Replace(' ', '_').ToUpperInvariant()}",
                                value: pair.Value);
                        }
                    }
                }
            }
        }

        protected void AddSecureFilesToEnvironment()
        {
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(SecureFiles, nameof(SecureFiles));

            List<SecureFile> secureFiles;
            if ((ExecutionContext.Variables.GetBoolean(Constants.Variables.Agent.AllowAllSecureFiles) ?? false) ||
                string.Equals(System.Environment.GetEnvironmentVariable("AGENT_ALLOWALLSECUREFILES") ?? string.Empty, bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                secureFiles = ExecutionContext.SecureFiles ?? new List<SecureFile>(0); // todo: remove after sprint 121 or so
            }
            else
            {
                secureFiles = SecureFiles;
            }

            // Add the secure files to the environment variable dictionary.
            foreach (SecureFile secureFile in secureFiles)
            {
                if (secureFile != null && secureFile.Id != Guid.Empty)
                {
                    string partialKey = secureFile.Id.ToString();
                    AddEnvironmentVariable(
                        key: $"SECUREFILE_NAME_{partialKey}",
                        value: secureFile.Name);
                    AddEnvironmentVariable(
                        key: $"SECUREFILE_TICKET_{partialKey}",
                        value: secureFile.Ticket);
                }
            }
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

        protected void AddVariablesToEnvironment(bool excludeNames = false, bool excludeSecrets = false)
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Environment, nameof(Environment));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(ExecutionContext.Variables, nameof(ExecutionContext.Variables));

            // Add the public variables.
            var names = new List<string>();
            foreach (KeyValuePair<string, string> pair in ExecutionContext.Variables.Public)
            {
                // Add "agent.jobstatus" using the unformatted name and formatted name.
                if (string.Equals(pair.Key, Constants.Variables.Agent.JobStatus, StringComparison.OrdinalIgnoreCase))
                {
                    AddEnvironmentVariable(pair.Key, pair.Value);
                }

                // Add the variable using the formatted name.
                string formattedKey = (pair.Key ?? string.Empty).Replace('.', '_').Replace(' ', '_').ToUpperInvariant();
                AddEnvironmentVariable(formattedKey, pair.Value);

                // Store the name.
                names.Add(pair.Key ?? string.Empty);
            }

            // Add the public variable names.
            if (!excludeNames)
            {
                AddEnvironmentVariable("VSTS_PUBLIC_VARIABLES", JsonUtility.ToString(names));
            }

            if (!excludeSecrets)
            {
                // Add the secret variables.
                var secretNames = new List<string>();
                foreach (KeyValuePair<string, string> pair in ExecutionContext.Variables.Private)
                {
                    // Add the variable using the formatted name.
                    string formattedKey = (pair.Key ?? string.Empty).Replace('.', '_').Replace(' ', '_').ToUpperInvariant();
                    AddEnvironmentVariable($"SECRET_{formattedKey}", pair.Value);

                    // Store the name.
                    secretNames.Add(pair.Key ?? string.Empty);
                }

                // Add the secret variable names.
                if (!excludeNames)
                {
                    AddEnvironmentVariable("VSTS_SECRET_VARIABLES", JsonUtility.ToString(secretNames));
                }
            }
        }

        protected void AddEnvironmentVariable(string key, string value)
        {
            ArgUtil.NotNullOrEmpty(key, nameof(key));
            Trace.Verbose($"Setting env '{key}' to '{value}'.");
            Environment[key] = value ?? string.Empty;
        }

        protected void AddTaskVariablesToEnvironment()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext.TaskVariables, nameof(ExecutionContext.TaskVariables));

            foreach (KeyValuePair<string, string> pair in ExecutionContext.TaskVariables.Public)
            {
                // Add the variable using the formatted name.
                string formattedKey = (pair.Key ?? string.Empty).Replace('.', '_').Replace(' ', '_').ToUpperInvariant();
                AddEnvironmentVariable($"VSTS_TASKVARIABLE_{formattedKey}", pair.Value);
            }

            foreach (KeyValuePair<string, string> pair in ExecutionContext.TaskVariables.Private)
            {
                // Add the variable using the formatted name.
                string formattedKey = (pair.Key ?? string.Empty).Replace('.', '_').Replace(' ', '_').ToUpperInvariant();
                AddEnvironmentVariable($"VSTS_TASKVARIABLE_{formattedKey}", pair.Value);
            }
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
            string taskEnvPATH;
            Environment.TryGetValue(Constants.PathVariable, out taskEnvPATH);
            string originalPath = ExecutionContext.Variables.Get(Constants.PathVariable) ?? // Prefer a job variable.
                taskEnvPATH ?? // Then a task-environment variable.
                System.Environment.GetEnvironmentVariable(Constants.PathVariable) ?? // Then an environment variable.
                string.Empty;
            string newPath = VarUtil.PrependPath(prepend, originalPath);
            AddEnvironmentVariable(Constants.PathVariable, newPath);
        }
    }
}