using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal sealed class ProcessResource
    {
        internal String Name { get; set; }

        internal String Type { get; set; }

        internal IDictionary<String, Object> Data
        {
            get
            {
                if (_data == null)
                {
                    _data = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
                }

                return _data;
            }
        }

        private IDictionary<String, Object> _data;
    }
}
