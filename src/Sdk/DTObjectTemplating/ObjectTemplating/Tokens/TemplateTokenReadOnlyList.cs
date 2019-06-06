using System;
using System.Collections.Generic;
using GitHub.DistributedTask.Expressions;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    /// <summary>
    /// Collection interface for expressions to work with SequenceToken objects
    /// </summary>
    internal sealed class TemplateTokenReadOnlyList : IReadOnlyArray
    {
        internal TemplateTokenReadOnlyList(SequenceToken sequence)
        {
            m_sequence = sequence;
        }

        public Int32 Count
        {
            get
            {
                if (m_list == null)
                {
                    Initialize();
                }

                return m_list.Count;
            }
        }

        public Object this[Int32 index]
        {
            get
            {
                if (m_list == null)
                {
                    Initialize();
                }

                return m_list[index];
            }
        }

        public IEnumerator<Object> GetEnumerator()
        {
            if (m_list == null)
            {
                Initialize();
            }

            return m_list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            if (m_list == null)
            {
                Initialize();
            }

            return m_list.GetEnumerator();
        }

        private void Initialize()
        {
            m_list = new List<Object>(m_sequence);
        }

        private readonly SequenceToken m_sequence;
        private List<Object> m_list;
    }
}
