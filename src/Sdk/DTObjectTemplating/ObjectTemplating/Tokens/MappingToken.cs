using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [JsonObject]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class MappingToken : TemplateToken, IEnumerable<KeyValuePair<ScalarToken, TemplateToken>>, IReadOnlyObject
    {
        public MappingToken(
            Int32? fileId,
            Int32? line,
            Int32? column)
            : base(TokenType.Mapping, fileId, line, column)
        {
        }

        internal Int32 Count => m_items?.Count ?? 0;

        // IReadOnlyObject (for expressions)
        Int32 IReadOnlyObject.Count
        {
            get
            {
                InitializeDictionary();
                return m_dictionary.Count;
            }
        }

        // IReadOnlyObject (for expressions)
        IEnumerable<String> IReadOnlyObject.Keys
        {
            get
            {
                InitializeDictionary();
                foreach (var key in m_dictionary.Keys)
                {
                    yield return key as String;
                }
            }
        }

        // IReadOnlyObject (for expressions)
        IEnumerable<Object> IReadOnlyObject.Values
        {
            get
            {
                InitializeDictionary();
                foreach (var value in m_dictionary.Values)
                {
                    yield return value;
                }
            }
        }

        public KeyValuePair<ScalarToken, TemplateToken> this[Int32 index]
        {
            get
            {
                return m_items[index];
            }

            set
            {
                m_items[index] = value;
                m_dictionary = null;
            }
        }

        // IReadOnlyObject (for expressions)
        Object IReadOnlyObject.this[String key]
        {
            get
            {
                InitializeDictionary();
                return m_dictionary[key];
            }
        }

        public void Add(IEnumerable<KeyValuePair<ScalarToken, TemplateToken>> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void Add(KeyValuePair<ScalarToken, TemplateToken> item)
        {
            if (m_items == null)
            {
                m_items = new List<KeyValuePair<ScalarToken, TemplateToken>>();
            }

            m_items.Add(item);
            m_dictionary = null;
        }

        public void Add(
            ScalarToken key,
            TemplateToken value)
        {
            Add(new KeyValuePair<ScalarToken, TemplateToken>(key, value));
        }

        public override TemplateToken Clone(Boolean omitSource)
        {
            var result = omitSource ? new MappingToken(null, null, null) : new MappingToken(FileId, Line, Column);
            if (m_items?.Count > 0)
            {
                foreach (var pair in m_items)
                {
                    result.Add(pair.Key?.Clone() as ScalarToken, pair.Value?.Clone());
                }
            }
            return result;
        }

        public IEnumerator<KeyValuePair<ScalarToken, TemplateToken>> GetEnumerator()
        {
            if (m_items?.Count > 0)
            {
                return m_items.GetEnumerator();
            }
            else
            {
                return (new List<KeyValuePair<ScalarToken, TemplateToken>>(0)).GetEnumerator();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            if (m_items?.Count > 0)
            {
                return m_items.GetEnumerator();
            }
            else
            {
                return (new KeyValuePair<ScalarToken, TemplateToken>[0]).GetEnumerator();
            }
        }

        public void Insert(
            Int32 index,
            KeyValuePair<ScalarToken, TemplateToken> item)
        {
            if (m_items == null)
            {
                m_items = new List<KeyValuePair<ScalarToken, TemplateToken>>();
            }

            m_items.Insert(index, item);
            m_dictionary = null;
        }

        public void Insert(
            Int32 index,
            ScalarToken key,
            TemplateToken value)
        {
            Insert(index, new KeyValuePair<ScalarToken, TemplateToken>(key, value));
        }

        public void RemoveAt(Int32 index)
        {
            m_items.RemoveAt(index);
            m_dictionary = null;
        }

        // IReadOnlyObject (for expressions)
        Boolean IReadOnlyObject.ContainsKey(String key)
        {
            InitializeDictionary();
            return m_dictionary.Contains(key);
        }

        // IReadOnlyObject (for expressions)
        IEnumerator IReadOnlyObject.GetEnumerator()
        {
            InitializeDictionary();
            return m_dictionary.GetEnumerator();
        }

        // IReadOnlyObject (for expressions)
        Boolean IReadOnlyObject.TryGetValue(
            String key,
            out Object value)
        {
            InitializeDictionary();
            if (!m_dictionary.Contains(key))
            {
                value = null;
                return false;
            }

            value = m_dictionary[key];
            return true;
        }

        /// <summary>
        /// Initializes the dictionary used for the expressions IReadOnlyObject interface
        /// </summary>
        private void InitializeDictionary()
        {
            if (m_dictionary == null)
            {
                m_dictionary = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
                if (m_items?.Count > 0)
                {
                    foreach (var pair in m_items)
                    {
                        if (pair.Key is StringToken stringToken &&
                            !m_dictionary.Contains(stringToken.Value))
                        {
                            m_dictionary.Add(stringToken.Value, pair.Value);
                        }
                    }
                }
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_items?.Count == 0)
            {
                m_items = null;
            }
        }

        [DataMember(Name = "map", EmitDefaultValue = false)]
        private List<KeyValuePair<ScalarToken, TemplateToken>> m_items;

        private IDictionary m_dictionary;
    }
}
