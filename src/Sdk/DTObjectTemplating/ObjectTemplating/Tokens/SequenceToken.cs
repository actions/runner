using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class SequenceToken : TemplateToken, IEnumerable<TemplateToken>, IReadOnlyArray
    {
        public SequenceToken(
            Int32? fileId,
            Int32? line,
            Int32? column)
            : base(TokenType.Sequence, fileId, line, column)
        {
        }

        public Int32 Count => m_items?.Count ?? 0;

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

        // IReadOnlyArray (for expressions)
        Object IReadOnlyArray.this[Int32 index]
        {
            get
            {
                return m_items[index];
            }
        }

        public void Add(TemplateToken value)
        {
            if (m_items == null)
            {
                m_items = new List<TemplateToken>();
            }

            m_items.Add(value);
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

        // IReadOnlyArray (for expressions)
        IEnumerator IReadOnlyArray.GetEnumerator()
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

        public void InsertRange(
            Int32 index,
            IEnumerable<TemplateToken> items)
        {
            if (m_items == null)
            {
                m_items = new List<TemplateToken>();
            }

            m_items.InsertRange(index, items);
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
