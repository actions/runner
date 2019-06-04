using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Expressions.CollectionAccessors
{
    internal sealed class JArrayAccessor : IReadOnlyArray
    {
        public JArrayAccessor(JArray jarray)
        {
            m_jarray = jarray;
        }

        public Int32 Count => m_jarray.Count;

        public Object this[Int32 index] => m_jarray[index];

        public IEnumerator<Object> GetEnumerator()
        {
            return m_jarray.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_jarray.GetEnumerator();
        }

        private readonly JArray m_jarray;
    }
}
