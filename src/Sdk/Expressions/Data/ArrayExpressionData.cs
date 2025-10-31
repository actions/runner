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
    public sealed class ArrayExpressionData : ExpressionData, IEnumerable<ExpressionData>, IReadOnlyArray
    {
        public ArrayExpressionData()
            : base(ExpressionDataType.Array)
        {
        }

        [IgnoreDataMember]
        public Int32 Count => m_items?.Count ?? 0;

        public ExpressionData this[Int32 index] => m_items[index];

        Object IReadOnlyArray.this[Int32 index] => m_items[index];

        public void Add(ExpressionData item)
        {
            if (m_items == null)
            {
                m_items = new List<ExpressionData>();
            }

            m_items.Add(item);
        }

        public override ExpressionData Clone()
        {
            var result = new ArrayExpressionData();
            if (m_items?.Count > 0)
            {
                result.m_items = new List<ExpressionData>(m_items.Count);
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

        public IEnumerator<ExpressionData> GetEnumerator()
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
        private List<ExpressionData> m_items;
    }
}