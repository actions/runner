using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines.ContextData
{
    [DataContract]
    [JsonObject]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DictionaryContextData : PipelineContextData, IEnumerable<KeyValuePair<String, PipelineContextData>>, IReadOnlyObject
    {
        public DictionaryContextData()
            : base(PipelineContextDataType.Dictionary)
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
        public IEnumerable<PipelineContextData> Values
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

        private List<DictionaryContextDataPair> List
        {
            get
            {
                if (m_list == null)
                {
                    m_list = new List<DictionaryContextDataPair>();
                }

                return m_list;
            }
        }

        public PipelineContextData this[String key]
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
                    m_list[index] = new DictionaryContextDataPair(key, value);
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

        internal KeyValuePair<String, PipelineContextData> this[Int32 index]
        {
            get
            {
                var pair = m_list[index];
                return new KeyValuePair<String, PipelineContextData>(pair.Key, pair.Value);
            }
        }

        public void Add(IEnumerable<KeyValuePair<String, PipelineContextData>> pairs)
        {
            foreach (var pair in pairs)
            {
                Add(pair.Key, pair.Value);
            }
        }

        public void Add(
            String key,
            PipelineContextData value)
        {
            IndexLookup.Add(key, m_list?.Count ?? 0);
            List.Add(new DictionaryContextDataPair(key, value));
        }

        public override PipelineContextData Clone()
        {
            var result = new DictionaryContextData();

            if (m_list?.Count > 0)
            {
                result.m_list = new List<DictionaryContextDataPair>(m_list.Count);
                foreach (var item in m_list)
                {
                    result.m_list.Add(new DictionaryContextDataPair(item.Key, item.Value?.Clone()));
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

        public IEnumerator<KeyValuePair<String, PipelineContextData>> GetEnumerator()
        {
            if (m_list?.Count > 0)
            {
                foreach (var pair in m_list)
                {
                    yield return new KeyValuePair<String, PipelineContextData>(pair.Key, pair.Value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (m_list?.Count > 0)
            {
                foreach (var pair in m_list)
                {
                    yield return new KeyValuePair<String, PipelineContextData>(pair.Key, pair.Value);
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
            out PipelineContextData value)
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
            if (TryGetValue(key, out PipelineContextData data))
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
        [ClientIgnore]
        [EditorBrowsable(EditorBrowsableState.Never)]
        private sealed class DictionaryContextDataPair
        {
            public DictionaryContextDataPair(
                String key,
                PipelineContextData value)
            {
                Key = key;
                Value = value;
            }

            [DataMember(Name = "k")]
            public readonly String Key;

            [DataMember(Name = "v")]
            public readonly PipelineContextData Value;
        }

        private Dictionary<String, Int32> m_indexLookup;

        [DataMember(Name = "d", EmitDefaultValue = false)]
        private List<DictionaryContextDataPair> m_list;
    }
}
