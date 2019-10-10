using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a default implementation of a counter store. 
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CounterStore : ICounterStore
    {
        public CounterStore(
            IDictionary<String, Int32> counters = null, 
            ICounterResolver resolver = null)
        {
            if (counters?.Count > 0)
            {
                m_counters.AddRange(counters);
            }

            this.Resolver = resolver;
        }

        public IReadOnlyDictionary<String, Int32> Counters
        {
            get
            {
                return m_counters;
            }
        }

        private ICounterResolver Resolver
        {
            get;
        }

        public Int32 Increment(
            IPipelineContext context,
            String prefix, 
            Int32 seed)
        {
            if (m_counters.TryGetValue(prefix, out Int32 existingValue))
            {
                return existingValue;
            }

            Int32 newValue = seed;
            if (this.Resolver != null)
            {
                newValue = this.Resolver.Increment(context, prefix, seed);
                m_counters[prefix] = newValue;
            }

            return newValue;
        }

        private readonly Dictionary<String, Int32> m_counters = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
    }
}
