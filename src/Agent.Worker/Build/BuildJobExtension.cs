using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class BuildJobExtension : AgentService, IJobExtension
    {
        public Type ExtensionType => typeof(IJobExtension);
        public string HostType => "build";
        public IStep PrepareStep { get; private set; }
        public IStep FinallyStep => null;

        public BuildJobExtension()
        {
            PrepareStep = new JobExtensionRunner(
                runAsync: PrepareAsync,
                alwaysRun: false,
                continueOnError: false,
                critical: true,
                displayName: StringUtil.Loc("GetSources"),
                enabled: true,
                @finally: false);
        }

        private async Task<TaskResult> PrepareAsync()
        {
            Trace.Entering();
            var directoryManager = HostContext.GetService<IBuildDirectoryManager>();
            IExecutionContext executionContext = PrepareStep.ExecutionContext;
            ServiceEndpoint endpoint = null;
            ISourceProvider sourceProvider = null;
            if (!TryGetPrimaryEndpointAndSourceProvider(executionContext, out endpoint, out sourceProvider))
            {
                executionContext.Error(StringUtil.Loc("SupportedRepositoryEndpointNotFound"));
                return TaskResult.Failed;
            }

            TrackingConfig trackingConfig = directoryManager.PrepareDirectory(
                executionContext,
                endpoint,
                sourceProvider);
            await Task.Yield();
            return TaskResult.Succeeded;
        }

        private bool TryGetPrimaryEndpointAndSourceProvider(
            IExecutionContext executionContext,
            out ServiceEndpoint endpoint,
            out ISourceProvider provider)
        {
            // Return the first service endpoint that contains a supported source provider.
            Trace.Entering();
            endpoint = null;
            provider = null;
            var extensionManager = HostContext.GetService<IExtensionManager>();
            List<ISourceProvider> sourceProviders = extensionManager.GetExtensions<ISourceProvider>();
            foreach (ServiceEndpoint ep in executionContext.Endpoints)
            {
                provider = sourceProviders
                    .FirstOrDefault(x => string.Equals(x.RepositoryType, ep.Type, StringComparison.OrdinalIgnoreCase));
                if (provider != null)
                {
                    endpoint = ep;
                    return true;
                }
            }

            return false;
        }
    }
}