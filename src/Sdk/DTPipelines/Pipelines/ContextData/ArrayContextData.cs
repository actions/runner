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
    public sealed class ArrayContextData : PipelineContextData, IEnumerable<PipelineContextData>, IReadOnlyArray
    {
        public ArrayContextData()
            : base(PipelineContextDataType.Array)
        {
        }

        [IgnoreDataMember]
        public Int32 Count => m_items?.Count ?? 0;

        public PipelineContextData this[Int32 index] => m_items[index];

        Object IReadOnlyArray.this[Int32 index] => m_items[index];

        public void Add(PipelineContextData item)
        {
            if (m_items == null)
            {
                m_items = new List<PipelineContextData>();
            }

            m_items.Add(item);
        }

        public override PipelineContextData Clone()
        {
            var result = new ArrayContextData();
            if (m_items?.Count > 0)
            {
                result.m_items = new List<PipelineContextData>(m_items.Count);
                foreach (var item in m_items)
                {
                    result.m_items.Add(item);
                }
            }
            return result;
        }

        public override JToken ToJToken()
        {
            var result = new JArray();
            if (m_items?.Count > 0)
            {
                foreach (var item in m_items)
                {
                    result.Add(item?.ToJToken() ?? JValue.CreateNull());
                }
            }
            return result;
        }

        public IEnumerator<PipelineContextData> GetEnumerator()
        {
            if (m_items?.Count > 0)
            {
                foreach (var item in m_items)
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (m_items?.Count > 0)
            {
                foreach (var item in m_items)
                {
                    yield return item;
                }
            }
        }

        IEnumerator IReadOnlyArray.GetEnumerator()
        {
            if (m_items?.Count > 0)
            {
                foreach (var item in m_items)
                {
                    yield return item;
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

        [DataMember(Name = "a", EmitDefaultValue = false)]
        private List<PipelineContextData> m_items;
    }
}
