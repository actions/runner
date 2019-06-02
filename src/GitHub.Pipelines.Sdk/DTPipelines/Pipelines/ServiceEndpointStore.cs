using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ServiceEndpointStore : IServiceEndpointStore
    {
        public ServiceEndpointStore(
            IList<ServiceEndpoint> endpoints, 
            IServiceEndpointResolver resolver = null)
        {
            this.Resolver = resolver;
            Add(endpoints?.ToArray());
        }

        /// <summary>
        /// Get the endpoint resolver configured for this store.
        /// </summary>
        public IServiceEndpointResolver Resolver
        {
            get;
        }

        public IList<ServiceEndpointReference> GetAuthorizedReferences()
        {
            return m_endpointsById.Values.Select(x => new ServiceEndpointReference { Id = x.Id, Name = x.Name }).ToList();
        }

        public void Authorize(ServiceEndpointReference reference)
        {
            this.Resolver?.Authorize(reference);
        }

        public ServiceEndpoint Get(ServiceEndpointReference reference)
        {
            if (reference == null)
            {
                return null;
            }

            var referenceId = reference.Id;
            var referenceName = reference.Name?.Literal;
            if (referenceId == Guid.Empty && String.IsNullOrEmpty(referenceName))
            {
                return null;
            }

            ServiceEndpoint authorizedEndpoint = null;
            if (referenceId != Guid.Empty)
            {
                if (m_endpointsById.TryGetValue(referenceId, out authorizedEndpoint))
                {
                    return authorizedEndpoint;
                }
            }
            else if (!String.IsNullOrEmpty(referenceName))
            {
                if (m_endpointsByName.TryGetValue(referenceName, out List<ServiceEndpoint> matchingEndpoints))
                {
                    if (matchingEndpoints.Count > 1)
                    {
                        throw new AmbiguousResourceSpecificationException(PipelineStrings.AmbiguousServiceEndpointSpecification(referenceId));
                    }

                    return matchingEndpoints[0];
                }
            }

            authorizedEndpoint = this.Resolver?.Resolve(reference);
            if (authorizedEndpoint != null)
            {
                Add(authorizedEndpoint);
            }

            return authorizedEndpoint;
        }

        private void Add(params ServiceEndpoint[] endpoints)
        {
            if (endpoints?.Length > 0)
            {
                foreach (var endpoint in endpoints)
                {
                    if (m_endpointsById.TryGetValue(endpoint.Id, out _))
                    {
                        continue;
                    }

                    m_endpointsById.Add(endpoint.Id, endpoint);

                    if (!m_endpointsByName.TryGetValue(endpoint.Name, out List<ServiceEndpoint> endpointsByName))
                    {
                        endpointsByName = new List<ServiceEndpoint>();
                        m_endpointsByName.Add(endpoint.Name, endpointsByName);
                    }

                    endpointsByName.Add(endpoint);
                }
            }
        }

        private readonly Dictionary<Guid, ServiceEndpoint> m_endpointsById = new Dictionary<Guid, ServiceEndpoint>();
        private readonly Dictionary<String, List<ServiceEndpoint>> m_endpointsByName = new Dictionary<String, List<ServiceEndpoint>>(StringComparer.OrdinalIgnoreCase);
    }
}
