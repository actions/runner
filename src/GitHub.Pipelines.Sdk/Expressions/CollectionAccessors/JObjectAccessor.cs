using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions.CollectionAccessors
{
    internal sealed class JObjectAccessor : IReadOnlyObject
    {
        public JObjectAccessor(JObject jobject)
        {
            m_jobject = jobject;
        }

        public Int32 Count => m_jobject.Count;

        public IEnumerable<String> Keys => (m_jobject as IDictionary<String, JToken>).Keys;

        // This uses Select. Calling .Values directly throws an exception.
        public IEnumerable<Object> Values => (m_jobject as IDictionary<String, JToken>).Select(x => x.Value);

        public Object this[String key] => m_jobject[key];

        public Boolean ContainsKey(String key)
        {
            return (m_jobject as IDictionary<String, JToken>).ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<String, Object>> GetEnumerator()
        {
            return (m_jobject as IDictionary<String, JToken>).Select(x => new KeyValuePair<String, Object>(x.Key, x.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (m_jobject as IDictionary<String, JToken>).Select(x => new KeyValuePair<String, Object>(x.Key, x.Value)).GetEnumerator();
        }

        public Boolean TryGetValue(
            String key,
            out Object value)
        {
            if ((m_jobject as IDictionary<String, JToken>).TryGetValue(key, out JToken val))
            {
                value = val;
                return true;
            }

            value = null;
            return false;
        }

        private readonly JObject m_jobject;
    }
}
