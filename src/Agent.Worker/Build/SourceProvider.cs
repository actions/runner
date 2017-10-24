using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public interface ISourceProvider : IExtension, IAgentService
    {
        string RepositoryType { get; }

        string GetBuildDirectoryHashKey(IExecutionContext executionContext, ServiceEndpoint endpoint);

        Task GetSourceAsync(IExecutionContext executionContext, ServiceEndpoint endpoint, CancellationToken cancellationToken);

        Task PostJobCleanupAsync(IExecutionContext executionContext, ServiceEndpoint endpoint);

        string GetLocalPath(IExecutionContext executionContext, ServiceEndpoint endpoint, string path);

        void SetVariablesInEndpoint(IExecutionContext executionContext, ServiceEndpoint endpoint);

        Task RunMaintenanceOperations(IExecutionContext executionContext, string repositoryPath);

        bool TestOverrideBuildDirectory();
    }

    public abstract class SourceProvider : AgentService
    {
        public Type ExtensionType => typeof(ISourceProvider);

        public abstract string RepositoryType { get; }

        public string GetBuildDirectoryHashKey(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            // Validate parameters.
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(executionContext.Variables, nameof(executionContext.Variables));
            ArgUtil.NotNull(endpoint, nameof(endpoint));
            ArgUtil.NotNull(endpoint.Url, nameof(endpoint.Url));

            // Calculate the hash key.
            const string Format = "{{{{ \r\n    \"system\" : \"build\", \r\n    \"collectionId\" = \"{0}\", \r\n    \"definitionId\" = \"{1}\", \r\n    \"repositoryUrl\" = \"{2}\", \r\n    \"sourceFolder\" = \"{{0}}\",\r\n    \"hashKey\" = \"{{1}}\"\r\n}}}}";
            string hashInput = string.Format(
                CultureInfo.InvariantCulture,
                Format,
                executionContext.Variables.System_CollectionId,
                executionContext.Variables.System_DefinitionId,
                endpoint.Url.AbsoluteUri);
            using (SHA1 sha1Hash = SHA1.Create())
            {
                byte[] data = sha1Hash.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
                StringBuilder hexString = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    hexString.Append(data[i].ToString("x2"));
                }

                return hexString.ToString();
            }
        }

        public virtual string GetLocalPath(IExecutionContext executionContext, ServiceEndpoint endpoint, string path)
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

        public virtual bool TestOverrideBuildDirectory()
        {
            return false;
        }
    }
}