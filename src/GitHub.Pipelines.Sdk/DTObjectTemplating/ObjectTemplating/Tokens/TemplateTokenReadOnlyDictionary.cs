using System;
using System.Collections.Generic;
using GitHub.DistributedTask.Expressions;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    /// <summary>
    /// Collection interface for expressions to work with MappingToken objects
    /// </summary>
    internal sealed class TemplateTokenReadOnlyDictionary : IReadOnlyObject
    {
        internal TemplateTokenReadOnlyDictionary(MappingToken mapping)
        {
            m_mapping = mapping;
        }

        public Int32 Count
        {
            get
            {
                if (m_dictionary == null)
                {
                    Initialize();
                }

                return m_dictionary.Count;
            }
        }

        public IEnumerable<String> Keys
        {
            get
            {
                if (m_dictionary == null)
                {
                    Initialize();
                }

                return m_dictionary.Keys;
            }
        }

        public IEnumerable<Object> Values
        {
            get
            {
                if (m_dictionary == null)
                {
                    Initialize();
                }

                return m_dictionary.Values;
            }
        }

        public Object this[String key]
        {
            get
            {
                if (m_dictionary == null)
                {
                    Initialize();
                }

                return m_dictionary[key];
            }
        }

        public Boolean ContainsKey(String key)
        {
            if (m_dictionary == null)
            {
                Initialize();
            }

            return m_dictionary.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<String, Object>> GetEnumerator()
        {
            if (m_dictionary == null)
            {
                Initialize();
            }

            return m_dictionary.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            if (m_dictionary == null)
            {
                Initialize();
            }

            return m_dictionary.GetEnumerator();
        }

        public Boolean TryGetValue(
            String key,
            out Object value)
        {
            if (m_dictionary == null)
            {
                Initialize();
            }

            return m_dictionary.TryGetValue(key, out value);
        }

        private void Initialize()
        {
            m_dictionary = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in m_mapping)
            {
                if (pair.Key is LiteralToken literal &&
                    !m_dictionary.ContainsKey(literal.Value))
                {
                    m_dictionary.Add(literal.Value, pair.Value);
                }
            }
        }

        private readonly MappingToken m_mapping;
        private Dictionary<String, Object> m_dictionary;
    }
}
