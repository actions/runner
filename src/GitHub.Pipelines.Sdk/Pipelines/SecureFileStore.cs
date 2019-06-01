using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SecureFileStore : ISecureFileStore
    {
        public SecureFileStore(
            IList<SecureFile> files,
            ISecureFileResolver resolver = null)
        {
            this.Resolver = resolver;
            Add(files?.ToArray());
        }

        /// <summary>
        /// Get the endpoint resolver configured for this store.
        /// </summary>
        public ISecureFileResolver Resolver
        {
            get;
        }

        public IList<SecureFileReference> GetAuthorizedReferences()
        {
            return m_resourcesById.Values.Select(x => new SecureFileReference { Id = x.Id }).ToList();
        }

        public SecureFile Get(SecureFileReference reference)
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

            SecureFile authorizedResource = null;
            if (referenceId != Guid.Empty)
            {
                if (m_resourcesById.TryGetValue(referenceId, out authorizedResource))
                {
                    return authorizedResource;
                }
            }
            else if (!String.IsNullOrEmpty(referenceName))
            {
                if (m_resourcesByName.TryGetValue(referenceName, out List<SecureFile> matchingResources))
                {
                    if (matchingResources.Count > 1)
                    {
                        throw new AmbiguousResourceSpecificationException(PipelineStrings.AmbiguousServiceEndpointSpecification(referenceId));
                    }

                    return matchingResources[0];
                }
            }

            // If we have an authorizer then attempt to authorize the reference for use
            authorizedResource = this.Resolver?.Resolve(reference);
            if (authorizedResource != null)
            {
                Add(authorizedResource);
            }

            return authorizedResource;
        }

        private void Add(params SecureFile[] resources)
        {
            if (resources?.Length > 0)
            {
                foreach (var resource in resources)
                {
                    if (m_resourcesById.TryGetValue(resource.Id, out _))
                    {
                        continue;
                    }

                    m_resourcesById.Add(resource.Id, resource);

                    if (!m_resourcesByName.TryGetValue(resource.Name, out List<SecureFile> resourcesByName))
                    {
                        resourcesByName = new List<SecureFile>();
                        m_resourcesByName.Add(resource.Name, resourcesByName);
                    }

                    resourcesByName.Add(resource);
                }
            }
        }

        private readonly Dictionary<Guid, SecureFile> m_resourcesById = new Dictionary<Guid, SecureFile>();
        private readonly Dictionary<String, List<SecureFile>> m_resourcesByName = new Dictionary<String, List<SecureFile>>(StringComparer.OrdinalIgnoreCase);
    }
}
