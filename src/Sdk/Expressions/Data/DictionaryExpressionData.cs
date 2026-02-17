#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Actions.Expressions.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Actions.Expressions.Data
{
    [DataContract]
    [JsonObject]
    public class DictionaryExpressionData : ExpressionData, IEnumerable<KeyValuePair<String, ExpressionData>>, IReadOnlyObject
    {
        public DictionaryExpressionData()
            : base(ExpressionDataType.Dictionary)
        {
        }

        [IgnoreDataMember]
        public Int32 Count => m_list?.Count ?? 0;

        [IgnoreDataMember]
        public IEnumerable<String> Keys
        {
            get
            {
                if (m_list?.Count > 0)
                {
                    foreach (var pair in m_list)
                    {
                        yield return pair.Key;
                    }
                }
            }
        }

        [IgnoreDataMember]
        public IEnumerable<ExpressionData> Values
        {
            get
            {
                if (m_list?.Count > 0)
                {
                    foreach (var pair in m_list)
                    {
                        yield return pair.Value;
                    }
                }
            }
        }

        IEnumerable<Object> IReadOnlyObject.Values
        {
            get
            {
                if (m_list?.Count > 0)
                {
                    foreach (var pair in m_list)
                    {
                        yield return pair.Value;
                    }
                }
            }
        }

        private Dictionary<String, Int32> IndexLookup
        {
            get
            {
                if (m_indexLookup == null)
                {
                    m_indexLookup = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
                    if (m_list?.Count > 0)
                    {
                        for (var i = 0; i < m_list.Count; i++)
                        {
                            var pair = m_list[i];
                            m_indexLookup.Add(pair.Key, i);
                        }
                    }
                }

                return m_indexLookup;
            }
        }

        private List<DictionaryExpressionDataPair> List
        {
            get
            {
                if (m_list == null)
                {
                    m_list = new List<DictionaryExpressionDataPair>();
                }

                return m_list;
            }
        }

        public ExpressionData this[String key]
        {
            get
            {
                var index = IndexLookup[key];
                return m_list[index].Value;
            }

            set
            {
                // Existing
                if (IndexLookup.TryGetValue(key, out var index))
                {
                    key = m_list[index].Key; // preserve casing
                    m_list[index] = new DictionaryExpressionDataPair(key, value);
                }
                // New
                else
                {
                    Add(key, value);
                }
            }
        }

        Object IReadOnlyObject.this[String key]
        {
            get
            {
                var index = IndexLookup[key];
                return m_list[index].Value;
            }
        }

        internal KeyValuePair<String, ExpressionData> this[Int32 index]
        {
            get
            {
                var pair = m_list[index];
                return new KeyValuePair<String, ExpressionData>(pair.Key, pair.Value);
            }
        }

        public void Add(IEnumerable<KeyValuePair<String, ExpressionData>> pairs)
        {
            foreach (var pair in pairs)
            {
                Add(pair.Key, pair.Value);
            }
        }

        public void Add(
            String key,
            ExpressionData value)
        {
            IndexLookup.Add(key, m_list?.Count ?? 0);
            List.Add(new DictionaryExpressionDataPair(key, value));
        }

        public override ExpressionData Clone()
        {
            var result = new DictionaryExpressionData();

            if (m_list?.Count > 0)
            {
                result.m_list = new List<DictionaryExpressionDataPair>(m_list.Count);
                foreach (var item in m_list)
                {
                    result.m_list.Add(new DictionaryExpressionDataPair(item.Key, item.Value?.Clone()));
                }
            }

            return result;
        }

        public override JToken ToJToken()
        {
            var json = new JObject();
            if (m_list?.Count > 0)
            {
                foreach (var item in m_list)
                {
                    json.Add(item.Key, item.Value?.ToJToken() ?? JValue.CreateNull());
                }
            }
            return json;
        }

        public Boolean ContainsKey(String key)
        {
            return TryGetValue(key, out _);
        }

        public IEnumerator<KeyValuePair<String, ExpressionData>> GetEnumerator()
        {
            if (m_list?.Count > 0)
            {
                foreach (var pair in m_list)
                {
                    yield return new KeyValuePair<String, ExpressionData>(pair.Key, pair.Value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (m_list?.Count > 0)
            {
                foreach (var pair in m_list)
                {
                    yield return new KeyValuePair<String, ExpressionData>(pair.Key, pair.Value);
                }
            }
        }

        IEnumerator IReadOnlyObject.GetEnumerator()
        {
            if (m_list?.Count > 0)
            {
                foreach (var pair in m_list)
                {
                    yield return new KeyValuePair<String, Object>(pair.Key, pair.Value);
                }
            }
        }

        public Boolean TryGetValue(
            String key,
            out ExpressionData value)
        {
            if (m_list?.Count > 0 &&
                IndexLookup.TryGetValue(key, out var index))
            {
                value = m_list[index].Value;
                return true;
            }

            value = null;
            return false;
        }

        Boolean IReadOnlyObject.TryGetValue(
            String key,
            out Object value)
        {
            if (TryGetValue(key, out ExpressionData data))
            {
                value = data;
                return true;
            }

            value = null;
            return false;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_list?.Count == 0)
            {
                m_list = null;
            }
        }

        [DataContract]
        private sealed class DictionaryExpressionDataPair
        {
            public DictionaryExpressionDataPair(
                String key,
                ExpressionData value)
            {
                Key = key;
                Value = value;
            }

            [DataMember(Name = "k")]
            public readonly String Key;

            [DataMember(Name = "v")]
            public readonly ExpressionData Value;
        }

        private Dictionary<String, Int32> m_indexLookup;

        [DataMember(Name = "d", EmitDefaultValue = false)]
        private List<DictionaryExpressionDataPair> m_list;
    }
}