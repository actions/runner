using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions.CollectionAccessors
{
    internal sealed class ListOfObjectAccessor : IReadOnlyArray
    {
        public ListOfObjectAccessor(IList<Object> list)
        {
            m_list = list;
        }

        public Int32 Count => m_list.Count;

        public Object this[Int32 index] => m_list[index];

        public IEnumerator<Object> GetEnumerator()
        {
            return m_list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_list.GetEnumerator();
        }

        private readonly IList<Object> m_list;
    }
}
