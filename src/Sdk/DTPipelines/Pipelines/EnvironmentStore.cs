using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class EnvironmentStore : IEnvironmentStore
    {
        public EnvironmentStore(
            IList<EnvironmentInstance> environments,
            IEnvironmentResolver resolver = null)
        {
            m_resolver = resolver;
            m_environmentsByName = new Dictionary<String, EnvironmentInstance>(StringComparer.OrdinalIgnoreCase);
            m_environmentsById = new Dictionary<Int32, EnvironmentInstance>();
            Add(environments?.ToArray());
        }

        public void Add(params EnvironmentInstance[] environments)
        {
            if (environments is null)
            {
                return;
            }
            foreach (var e in environments)
            {
                if (e != null)
                {
                    m_environmentsById[e.Id] = e;

                    var name = e.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        m_environmentsByName[name] = e;
                    }
                }
            }
        }

        public EnvironmentInstance ResolveEnvironment(String name)
        {
            if (!m_environmentsByName.TryGetValue(name, out var environment)
             && m_resolver != null)
            {
                environment = m_resolver?.Resolve(name);
                Add(environment);
            }

            return environment;
        }

        public EnvironmentInstance ResolveEnvironment(Int32 id)
        {
            if (!m_environmentsById.TryGetValue(id, out var environment)
               && m_resolver != null)
            {
                environment = m_resolver?.Resolve(id);
                Add(environment);
            }

            return environment;
        }

        public EnvironmentInstance Get(EnvironmentReference reference)
        {
            if (reference is null)
            {
                return null;
            }

            if (reference.Name?.IsLiteral == true)
            {
                return ResolveEnvironment(reference.Name.Literal);
            }

            return ResolveEnvironment(reference.Id);
        }

        public IList<EnvironmentReference> GetReferences()
        {
            return m_environmentsById.Values
                .Select(x => new EnvironmentReference
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        private IEnvironmentResolver m_resolver;
        private IDictionary<String, EnvironmentInstance> m_environmentsByName;
        private IDictionary<Int32, EnvironmentInstance> m_environmentsById;
    }
}
