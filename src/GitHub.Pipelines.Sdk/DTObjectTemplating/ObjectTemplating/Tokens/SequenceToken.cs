using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [JsonObject]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class SequenceToken : TemplateToken, IEnumerable<TemplateToken>
    {
        public SequenceToken(
            Int32? fileId,
            Int32? line,
            Int32? column)
            : base(TokenType.Sequence, fileId, line, column)
        {
        }

        public TemplateToken this[Int32 index]
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

        internal Int32 Count => m_items?.Count ?? 0;

        public void Add(TemplateToken value)
        {
            if (m_items == null)
            {
                m_items = new List<TemplateToken>();
            }

            m_items.Add(value);
        }

        public override TemplateToken Clone()
        {
            return Clone(false);
        }

        public override TemplateToken Clone(Boolean omitSource)
        {
            var result = omitSource ? new SequenceToken(null, null, null) : new SequenceToken(FileId, Line, Column);
            if (m_items?.Count > 0)
            {
                foreach (var item in m_items)
                {
                    result.Add(item?.Clone());
                }
            }
            return result;
        }

        public IEnumerator<TemplateToken> GetEnumerator()
        {
            if (m_items?.Count > 0)
            {
                return m_items.GetEnumerator();
            }
            else
            {
                return (new TemplateToken[0] as IEnumerable<TemplateToken>).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (m_items?.Count > 0)
            {
                return m_items.GetEnumerator();
            }
            else
            {
                return (new TemplateToken[0] as IEnumerable<TemplateToken>).GetEnumerator();
            }
        }

        public void Insert(
            Int32 index,
            TemplateToken item)
        {
            if (m_items == null)
            {
                m_items = new List<TemplateToken>();
            }

            m_items.Insert(index, item);
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

        [DataMember(Name = "seq", EmitDefaultValue = false)]
        private List<TemplateToken> m_items;
    }
}
