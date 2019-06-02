using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [JsonObject]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class MappingToken : TemplateToken, IEnumerable<KeyValuePair<ScalarToken, TemplateToken>>
    {
        public MappingToken(
            Int32? fileId,
            Int32? line,
            Int32? column)
            : base(TokenType.Mapping, fileId, line, column)
        {
        }

        internal Int32 Count => m_items?.Count ?? 0;

        public KeyValuePair<ScalarToken, TemplateToken> this[Int32 index]
        {
            get
            {
                return m_items[index];
            }

            set
            {
                m_items[index] = value;
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
        }

        public void Add(
            ScalarToken key,
            TemplateToken value)
        {
            Add(new KeyValuePair<ScalarToken, TemplateToken>(key, value));
        }

        public override TemplateToken Clone()
        {
            return Clone(false);
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
    }
}