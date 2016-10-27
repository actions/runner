using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    public interface IHandler : IAgentService
    {
        IExecutionContext ExecutionContext { get; set; }
        string FilePathInputRootDirectory { get; set; }
        Dictionary<string, string> Inputs { get; set; }
        string TaskDirectory { get; set; }

        Task RunAsync();
    }

    public abstract class Handler : AgentService
    {
        protected IWorkerCommandManager CommandManager { get; private set; }
        protected Dictionary<string, string> Environment { get; private set; }

        public IExecutionContext ExecutionContext { get; set; }
        public string FilePathInputRootDirectory { get; set; }
        public Dictionary<string, string> Inputs { get; set; }
        public string TaskDirectory { get; set; }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            CommandManager = hostContext.GetService<IWorkerCommandManager>();
            Environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        protected void AddEndpointsToEnvironment()
        {
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(ExecutionContext.Endpoints, nameof(ExecutionContext.Endpoints));

            // Add the endpoints to the environment variable dictionary.
            foreach (ServiceEndpoint endpoint in ExecutionContext.Endpoints)
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
                AddEnvironmentVariable("VSTS_PUBLIC_VARIABLES", StringUtil.ConvertToJson(names));
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
                    AddEnvironmentVariable("VSTS_SECRET_VARIABLES", StringUtil.ConvertToJson(secretNames));
                }
            }
        }

        protected void AddEnvironmentVariable(string key, string value)
        {
            ArgUtil.NotNullOrEmpty(key, nameof(key));
            Trace.Verbose($"Setting env '{key}' to '{value}'.");
            Environment[key] = value ?? string.Empty;
        }
    }
}