using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Agent.Sdk;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.BlobStore.Common;
using Microsoft.VisualStudio.Services.BlobStore.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Content.Common.Tracing;
using Microsoft.VisualStudio.Services.PipelineCache.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;

namespace Agent.Plugins.PipelineCache
{    
    public class RestorePipelineCacheV0 : PipelineCacheTaskPluginBase
    {
        public override Guid Id => PipelineCachePluginConstants.RestoreCacheTaskId;

        protected override async Task ProcessCommandInternalAsync(
            AgentTaskPluginExecutionContext context, 
            string path, 
            string keyStr,
            string salt,
            CancellationToken token)
        {
            string[] key = keyStr.Split(
                new[] { '\n' },
                StringSplitOptions.RemoveEmptyEntries
            );
            string variableToSetOnHit = context.GetInput(PipelineCacheTaskPluginConstants.VariableToSetOnCacheHit, required: true);

            PipelineCacheServer server = new PipelineCacheServer();
            await server.DownloadAsync(
                context, 
                key, 
                path,
                salt,
                variableToSetOnHit,
                token);
        }
    }
}