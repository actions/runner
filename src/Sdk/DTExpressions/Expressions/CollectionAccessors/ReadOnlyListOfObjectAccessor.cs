using System;
using System.Collections;
using System.Collections.Generic;

namespace GitHub.DistributedTask.Expressions.CollectionAccessors
{
    internal sealed class ReadOnlyListOfObjectAccessor : IReadOnlyArray
    {
        public ReadOnlyListOfObjectAccessor(IReadOnlyList<Object> list)
        {
            m_list = list;
        }

        public Int32 Count => m_list.Count;

        public Object this[Int32 index] => m_list[index];

        public IEnumerator<Object> GetEnumerator() => m_list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_list.GetEnumerator();

        private readonly IReadOnlyList<Object> m_list;
    }
}
