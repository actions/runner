using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions.CollectionAccessors
{
    internal sealed class DictionaryOfStringObjectAccessor : IReadOnlyObject
    {
        public DictionaryOfStringObjectAccessor(IDictionary<String, Object> dictionary)
        {
            m_dictionary = dictionary;
        }

        public Int32 Count => m_dictionary.Count;

        public IEnumerable<String> Keys => m_dictionary.Keys;

        public IEnumerable<Object> Values => m_dictionary.Values;

        public Object this[String key] => m_dictionary[key];

        public Boolean ContainsKey(String key)
        {
            return m_dictionary.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<String, Object>> GetEnumerator()
        {
            return m_dictionary.Select(x => new KeyValuePair<String, Object>(x.Key, x.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_dictionary.Select(x => new KeyValuePair<String, Object>(x.Key, x.Value)).GetEnumerator();
        }

        public Boolean TryGetValue(
            String key,
            out Object value)
        {
            return m_dictionary.TryGetValue(key, out value);
        }

        private readonly IDictionary<String, Object> m_dictionary;
    }
}
