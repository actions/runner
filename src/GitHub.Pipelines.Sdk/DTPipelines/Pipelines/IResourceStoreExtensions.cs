using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IResourceStoreExtensions
    {
        /// <summary>
        /// Extracts the full resources from the <paramref name="store"/> which are referenced in the 
        /// <paramref name="resources"/> collection.
        /// </summary>
        /// <param name="store">The store which contains the resources</param>
        /// <param name="resources">The resources which should be included with the job</param>
        /// <returns>A new <c>JobResources</c> instance with the filtered set of resources from the store</returns>
        public static JobResources GetJobResources(
            this IResourceStore store,
            PipelineResources resources)
        {
            var jobResources = new JobResources();
            jobResources.Containers.AddRange(resources.Containers.Select(x => x.Clone()));

            foreach (var endpointRef in resources.Endpoints)
            {
                var endpoint = store.Endpoints.Get(endpointRef);
                if (endpoint != null)
                {
                    jobResources.Endpoints.Add(endpoint);
                }
            }

            foreach (var fileRef in resources.Files)
            {
                var file = store.Files.Get(fileRef);
                if (file != null)
                {
                    jobResources.SecureFiles.Add(file);
                }
            }

            foreach (var repository in resources.Repositories)
            {
                jobResources.Repositories.Add(store.Repositories.Get(repository.Alias));
            }

            return jobResources;
        }

        /// <summary>
        /// Retrieves a service endpoint from the store using the provided reference.
        /// </summary>
        /// <param name="store">The resource store which should be queried</param>
        /// <param name="reference">The service endpoint reference which should be resolved</param>
        /// <returns>A <c>ServiceEndpoint</c> instance matching the specified reference if found; otherwise, null</returns>
        public static ServiceEndpoint GetEndpoint(
            this IResourceStore store,
            ServiceEndpointReference reference)
        {
            return store.Endpoints.Get(reference);
        }

        /// <summary>
        /// Retrieves a secure file from the store using the provided reference.
        /// </summary>
        /// <param name="store">The resource store which should be queried</param>
        /// <param name="reference">The secure file reference which should be resolved</param>
        /// <returns>A <c>SecureFile</c> instance matching the specified reference if found; otherwise, null</returns>
        public static SecureFile GetFile(
            this IResourceStore store,
            SecureFileReference reference)
        {
            return store.Files.Get(reference);
        }

        /// <summary>
        /// Retrieves an agent queue from the store using the provided reference.
        /// </summary>
        /// <param name="store">The resource store which should be queried</param>
        /// <param name="reference">The agent queue reference which should be resolved</param>
        /// <returns>A <c>TaskAgentQueue</c> instance matching the specified reference if found; otherwise, null</returns>
        public static TaskAgentQueue GetQueue(
            this IResourceStore store,
            AgentQueueReference reference)
        {
            return store.Queues.Get(reference);
        }

        /// <summary>
        /// Retrieves an agent pool from the store using the provided reference.
        /// </summary>
        /// <param name="store">The resource store which should be queried</param>
        /// <param name="reference">The agent pool reference which should be resolved</param>
        /// <returns>A <c>TaskAgentPool</c> instance matching the specified reference if found; otherwise, null</returns>
        public static TaskAgentPool GetPool(
           this IResourceStore store,
           AgentPoolReference reference)
        {
            return store.Pools.Get(reference);
        }

        /// <summary>
        /// Retrieves a variable group from the store using the provided reference.
        /// </summary>
        /// <param name="store">The resource store which should be queried</param>
        /// <param name="reference">The variable group reference which should be resolved</param>
        /// <returns>A <c>VariableGroup</c> instance matching the specified reference if found; otherwise, null</returns>
        public static VariableGroup GetVariableGroup(
            this IResourceStore store,
            VariableGroupReference reference)
        {
            return store.VariableGroups.Get(reference);
        }

        /// <summary>
        /// Given a partially formed reference, returns the associated reference stored with the plan.
        /// </summary>
        public static ResourceReference GetSnappedReference(
            this IResourceStore store,
            ResourceReference r)
        {
            if (r is VariableGroupReference vgr)
            {
                var m = store.VariableGroups.Get(vgr);
                if (m != null)
                {
                    return new VariableGroupReference
                    {
                        Id = m.Id,
                        Name = m.Name
                    };
                }
            }
            else if (r is AgentQueueReference aqr)
            {
                var m = store.Queues.Get(aqr);
                if (m != null)
                {
                    return new AgentQueueReference
                    {
                        Id = m.Id,
                        Name = m.Name
                    };
                }
            }
            else if (r is AgentPoolReference apr)
            {
                var m = store.Pools.Get(apr);
                if (m != null)
                {
                    return new AgentPoolReference
                    {
                        Id = m.Id,
                        Name = m.Name
                    };
                }
            }
            else if (r is ServiceEndpointReference ser)
            {
                var m = store.Endpoints.Get(ser);
                if (m != null)
                {
                    return new ServiceEndpointReference
                    {
                        Id = m.Id,
                        Name = m.Name
                    };
                }
            }
            else if (r is SecureFileReference sfr)
            {
                var m = store.Files.Get(sfr);
                if (m != null)
                {
                    return new SecureFileReference
                    {
                        Id = m.Id,
                        Name = m.Name
                    };
                }
            }
            else if (r is EnvironmentReference er)
            {
                var m = store.Environments.Get(er);
                if (m != null)
                {
                    return new EnvironmentReference
                    {
                        Id = m.Id,
                        Name = m.Name
                    };
                }
            }

            return r;
        }
    }
}
