using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions.CollectionAccessors
{
    internal sealed class ReadOnlyDictionaryOfStringObjectAccessor : IReadOnlyObject
    {
        public ReadOnlyDictionaryOfStringObjectAccessor(IReadOnlyDictionary<String, Object> dictionary)
        {
            m_dictionary = dictionary;
        }

        public Int32 Count => m_dictionary.Count;

        public IEnumerable<String> Keys => m_dictionary.Keys;

        public IEnumerable<Object> Values => m_dictionary.Values;

        public Object this[String key] => m_dictionary[key];

        public Boolean ContainsKey(String key) => m_dictionary.ContainsKey(key);

        public IEnumerator<KeyValuePair<String, Object>> GetEnumerator() => m_dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_dictionary.GetEnumerator();

        public Boolean TryGetValue(
            String key,
            out Object value)
        {
            return m_dictionary.TryGetValue(key, out value);
        }

        private readonly IReadOnlyDictionary<String, Object> m_dictionary;
    }
}
