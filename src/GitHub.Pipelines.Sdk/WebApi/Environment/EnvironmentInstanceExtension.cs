using System;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    public static class EnvironmentInstanceExtension
    {
        public static PipelineResources GetLinkedResources(this EnvironmentInstance environment, int resourceId)
        {
            var pipelineResources = new PipelineResources();
            var resource = environment.Resources.FirstOrDefault(r => r.Id == resourceId);
            if (resource != null)
            {
                foreach (var linkedResource in resource.LinkedResources)
                {
                    switch (linkedResource.TypeName)
                    {
                        case "Microsoft.TeamFoundation.DistributedTask.Pipelines.ServiceEndpointReference":
                            pipelineResources.AddEndpointReference(new ServiceEndpointReference { Id = new Guid(linkedResource.Id) });
                            break;

                        default:
                            break;
                    }
                }
            }

            return pipelineResources;
        }
    }
}