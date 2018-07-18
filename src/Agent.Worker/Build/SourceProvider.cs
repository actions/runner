using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public interface ISourceProvider : IExtension, IAgentService
    {
        string RepositoryType { get; }

        Task GetSourceAsync(IExecutionContext executionContext, ServiceEndpoint endpoint, CancellationToken cancellationToken);

        Task PostJobCleanupAsync(IExecutionContext executionContext, ServiceEndpoint endpoint);

        string GetLocalPath(IExecutionContext executionContext, RepositoryResource repository, string path);

        void SetVariablesInEndpoint(IExecutionContext executionContext, ServiceEndpoint endpoint);

        Task RunMaintenanceOperations(IExecutionContext executionContext, string repositoryPath);
    }

    public abstract class SourceProvider : AgentService
    {
        public Type ExtensionType => typeof(ISourceProvider);

        public abstract string RepositoryType { get; }

        public virtual string GetLocalPath(IExecutionContext executionContext, RepositoryResource repository, string path)
        {
            return path;
        }

        public virtual void SetVariablesInEndpoint(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            endpoint.Data.Add(Constants.EndpointData.SourcesDirectory, executionContext.Variables.Get(Constants.Variables.Build.SourcesDirectory));
            endpoint.Data.Add(Constants.EndpointData.SourceVersion, executionContext.Variables.Get(Constants.Variables.Build.SourceVersion));
        }

        public string GetEndpointData(ServiceEndpoint endpoint, string name)
        {
            var trace = HostContext.GetTrace(nameof(SourceProvider));
            string value;
            if (endpoint.Data.TryGetValue(name, out value))
            {
                trace.Info($"Get '{name}': '{value}'");
                return value;
            }

            trace.Info($"Get '{name}' (not found)");
            return null;
        }

        public virtual Task RunMaintenanceOperations(IExecutionContext executionContext, string repositoryPath)
        {
            return Task.CompletedTask;
        }
    }
}