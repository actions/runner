using System.Collections;
using System.Collections.Generic;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.ObjectTemplating;

namespace Runner.Server.Azure.Devops
{
    class ParametersContextData : IReadOnlyObject
    {
        private Dictionary<string, object> data;
        private TemplateValidationErrors errors;
        public ParametersContextData(Dictionary<string, object> data, TemplateValidationErrors errors)
        {
            this.data = data;
            this.errors = errors;
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
            if(!data.TryGetValue(key, out value)) {
                errors.Add($"Unexpected parameter reference 'parameters.{key}'");
                return false;
            }
            return true;
        }
    }

}