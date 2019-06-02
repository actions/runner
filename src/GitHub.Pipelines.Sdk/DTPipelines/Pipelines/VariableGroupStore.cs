using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VariableGroupStore : IVariableGroupStore
    {
        public VariableGroupStore(
            IList<VariableGroup> resources,
            IVariableGroupResolver resolver = null,
            params IVariableValueProvider[] valueProviders)
        {
            this.Resolver = resolver;
            Add(resources?.ToArray());

            if (valueProviders?.Length > 0)
            {
                m_valueProviders = new Dictionary<String, IVariableValueProvider>(StringComparer.OrdinalIgnoreCase);
                foreach (var valueProvider in valueProviders)
                {
                    if (!m_valueProviders.TryAdd(valueProvider.GroupType, valueProvider))
                    {
                        throw new ArgumentException($"Group type {valueProvider.GroupType} cannot have more than one provider", nameof(valueProviders));
                    }
                }
            }
        }

        /// <summary>
        /// Get the variable group resolver configured for this store.
        /// </summary>
        public IVariableGroupResolver Resolver
        {
            get;
        }

        public IList<VariableGroupReference> GetAuthorizedReferences()
        {
            return m_resourcesById.Values.Select(x => new VariableGroupReference { Id = x.Id }).ToList();
        }

        public VariableGroup Get(VariableGroupReference reference)
        {
            if (reference == null)
            {
                return null;
            }

            var referenceId = reference.Id;
            var referenceName = reference.Name?.Literal;
            if (referenceId == 0 && String.IsNullOrEmpty(referenceName))
            {
                return null;
            }

            VariableGroup authorizedResource = null;
            if (referenceId != 0)
            {
                if (m_resourcesById.TryGetValue(referenceId, out authorizedResource))
                {
                    return authorizedResource;
                }
            }
            else if (!String.IsNullOrEmpty(referenceName))
            {
                if (m_resourcesByName.TryGetValue(referenceName, out List<VariableGroup> matchingResources))
                {
                    if (matchingResources.Count > 1)
                    {
                        throw new AmbiguousResourceSpecificationException(PipelineStrings.AmbiguousVariableGroupSpecification(referenceName));
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

        public IList<TaskStep> GetPreSteps(
            IPipelineContext context,
            IReadOnlyList<JobStep> steps)
        {
            if (context.ReferencedResources.VariableGroups.Count == 0)
            {
                return null;
            }

            // If the environment version is 1 and it's a build context we should inject
            if (context.EnvironmentVersion < 2 && context is PipelineExecutionContext)
            {
                return null;
            }

            var newSteps = new List<TaskStep>();
            foreach (var group in context.ReferencedResources.VariableGroups.Where(x => x.SecretStore != null && x.SecretStore.Keys.Count > 0))
            {
                // Only inject a task if the provider supports task injection for the current context
                var valueProvider = GetValueProvider(group);
                if (valueProvider != null && !valueProvider.ShouldGetValues(context))
                {
                    newSteps.AddRangeIfRangeNotNull(valueProvider.GetSteps(context, group, group.SecretStore.Keys));
                }
            }

            return newSteps;
        }

        public IList<TaskStep> GetPostSteps(
            IPipelineContext context,
            IReadOnlyList<JobStep> steps)
        {
            return new List<TaskStep>();
        }

        public Boolean ResolveStep(
            IPipelineContext context, 
            JobStep step, 
            out IList<TaskStep> resolvedSteps)
        {
            resolvedSteps = new List<TaskStep>();
            return false;
        }

        /// <summary>
        /// Gets the value provider which may be used to retrieve values for the variable group from the data store.
        /// </summary>
        /// <param name="group">The target variable group</param>
        /// <returns>A provider suitable for retrieving values from the variable group</returns>
        public IVariableValueProvider GetValueProvider(VariableGroupReference group)
        {
            if (m_valueProviders != null && m_valueProviders.TryGetValue(group.GroupType, out var valueProvider))
            {
                return valueProvider;
            }
            return null;
        }

        private void Add(params VariableGroup[] resources)
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

                    if (!m_resourcesByName.TryGetValue(resource.Name, out List<VariableGroup> resourcesByName))
                    {
                        resourcesByName = new List<VariableGroup>();
                        m_resourcesByName.Add(resource.Name, resourcesByName);
                    }

                    resourcesByName.Add(resource);
                }
            }
        }

        private readonly Dictionary<String, IVariableValueProvider> m_valueProviders;
        private readonly Dictionary<Int32, VariableGroup> m_resourcesById = new Dictionary<Int32, VariableGroup>();
        private readonly Dictionary<String, List<VariableGroup>> m_resourcesByName = new Dictionary<String, List<VariableGroup>>(StringComparer.OrdinalIgnoreCase);
    }
}
