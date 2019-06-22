using System;
using System.Collections.Generic;

namespace GitHub.Build.WebApi
{
    internal static class SerializationHelper
    {
        public static void Copy<T>(
            ref List<T> source,
            ref List<T> target, 
            Boolean clearSource = false)
        {
            if (source != null && source.Count > 0)
            {
                target = new List<T>(source);
                if (clearSource)
                {
                    source = null;
                }
            }
        }

        public static void Copy<TKey, TValue>(
            ref IDictionary<TKey, TValue> source,
            ref IDictionary<TKey, TValue> target, 
            IEqualityComparer<TKey> comparer,
            Boolean clearSource = false)
        {
            if (source != null && source.Count > 0)
            {
                target = new Dictionary<TKey, TValue>(source, comparer);
                if (clearSource)
                {
                    source = null;
                }
            }
        }
    }
}
