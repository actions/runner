using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agent.Sdk;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.BlobStore.Common;
using Microsoft.VisualStudio.Services.BlobStore.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Content.Common.Tracing;
using Microsoft.VisualStudio.Services.WebApi;

namespace Agent.Plugins.PipelineCache
{
    public abstract class PipelineCacheTaskPluginBase : IAgentTaskPlugin
    {
        public abstract Guid Id { get; }
        public string Version => "0.1.0"; // Publish and Download tasks will be always on the same version.
        public string Stage => "main";

        public async Task RunAsync(AgentTaskPluginExecutionContext context, CancellationToken token)
        {
            ArgUtil.NotNull(context, nameof(context));

            string key = context.GetInput(PipelineCacheTaskPluginConstants.Key, required: true);

            // TODO: Translate path from container to host (Ting)
            string path = context.GetInput(PipelineCacheTaskPluginConstants.Path, required: true);

            // TODO: variable is meant to be temporary until the salt lives in the service side (Pipeline service)
            VariableValue saltVariable = context.Variables.GetValueOrDefault("AZDEVOPS_PIPELINECACHE_SALT");
            string salt = saltVariable == null ? "randomSalt" : saltVariable.Value;

            await ProcessCommandInternalAsync(
                context, 
                path, 
                key, 
                salt,
                token);
        }

        // Process the command with preprocessed arguments.
        protected abstract Task ProcessCommandInternalAsync(
            AgentTaskPluginExecutionContext context, 
            string path, 
            string key,
            string salt,
            CancellationToken token);

            
        // Properties set by tasks
        protected static class PipelineCacheTaskPluginConstants
        {
            public static readonly string Key = "key"; // this needs to match the input in the task.
            public static readonly string Path = "path";
            public static readonly string PipelineId = "pipelineId";
            public static readonly string VariableToSetOnCacheHit = "cacheHitVar";
            public static readonly string Salt = "salt";
            
        }
    }
}