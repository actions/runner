using System;
using System.Collections;
using System.Collections.Generic;
using GitHub.DistributedTask.Expressions2.Sdk;
using Runner.Server.Azure.Devops;

namespace Runner.Server.Controllers
{
    internal class PipelineContext : IReadOnlyObject
    {
        private Dictionary<string, object> data;
        public PipelineContext(DateTimeOffset starttime)
        {
            data = new(StringComparer.OrdinalIgnoreCase);
            data["startTime"] = new DateTimeWrapper { DateTime = starttime };
        }

        public object this[string key] => data[key];

        public int Count => data.Count;

        public IEnumerable<string> Keys => data.Keys;

        public IEnumerable<object> Values => data.Values;

        public bool ContainsKey(string key)
        {
            return data.ContainsKey(key);
        }

        public IEnumerator GetEnumerator()
        {
            return data.GetEnumerator();
        }

        public bool TryGetValue(string key, out object value)
        {
            return data.TryGetValue(key, out value);
        }
    }
}