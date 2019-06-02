using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Common
{
    public class ReadOnlyDictionaryComparer<K, V> : IEqualityComparer<IReadOnlyDictionary<K, V>>
    {
        public static bool Equals(
            IReadOnlyDictionary<K, V> thisDictionary,
            IReadOnlyDictionary<K, V> thatDictionary,
            IEqualityComparer<V> valueComparer = null,
            Action<int, int> whenCountsNotEqual = null,
            Action<K, V> whenCorrespondingValueNotFound = null,
            Action<K, V, V> whenCorrespondingValueNotEqual = null)
        {
            if (ReferenceEquals(thisDictionary, thatDictionary))
            {
                return true;
            }

            if (thisDictionary == null)
            {
                if (thatDictionary == null)
                {
                    return true;
                }

                return false;
            }

            if (thatDictionary == null)
            {
                return false;
            }

            if (thisDictionary.Count != thatDictionary.Count)
            {
                whenCountsNotEqual?.Invoke(thisDictionary.Count, thatDictionary.Count);

                return false;
            }

            valueComparer = valueComparer ?? EqualityComparer<V>.Default;

            if (!IsSubset(thisDictionary, thatDictionary, valueComparer, whenCorrespondingValueNotFound, whenCorrespondingValueNotEqual))
            {
                return false;
            }

            if (!IsSubset(thatDictionary, thisDictionary, valueComparer, whenCorrespondingValueNotFound, whenCorrespondingValueNotEqual))
            {
                return false;
            }

            return true;
        }

        public static bool IsSubset(
            IReadOnlyDictionary<K, V> candidateSubsetDictionary,
            IReadOnlyDictionary<K, V> candidateSupersetDictionary,
            IEqualityComparer<V> valueComparer = null,
            Action<K, V> whenCorrespondingValueNotFound = null,
            Action<K, V, V> whenCorrespondingValueNotEqual = null)
        {
            foreach (var subsetKeyValuePair in candidateSubsetDictionary)
            {
                V supersetValue = default(V);

                if (!candidateSupersetDictionary.TryGetValue(subsetKeyValuePair.Key, out supersetValue))
                {
                    whenCorrespondingValueNotFound?.Invoke(subsetKeyValuePair.Key, subsetKeyValuePair.Value);

                    return false;
                }

                if (!valueComparer.Equals(subsetKeyValuePair.Value, supersetValue))
                {
                    whenCorrespondingValueNotEqual?.Invoke(subsetKeyValuePair.Key, subsetKeyValuePair.Value, supersetValue);

                    return false;
                }
            }

            return true;
        }

        bool IEqualityComparer<IReadOnlyDictionary<K, V>>.Equals(
            IReadOnlyDictionary<K, V> thisDictionary,
            IReadOnlyDictionary<K, V> thatDictionary)
            => Equals(thisDictionary, thatDictionary);

        public int GetHashCode(IReadOnlyDictionary<K, V> dictionary)
        {
            int hashCode = 7243;

            foreach (var keyValuePair in dictionary)
            {
                hashCode = 524287 * hashCode + keyValuePair.Key.GetHashCode();
                hashCode = 524287 * hashCode + keyValuePair.Value?.GetHashCode() ?? 0;
            }

            return hashCode;
        }
    }
}
