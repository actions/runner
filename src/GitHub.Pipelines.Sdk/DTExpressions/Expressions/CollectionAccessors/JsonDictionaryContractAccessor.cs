using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace GitHub.DistributedTask.Expressions.CollectionAccessors
{
    internal sealed class JsonDictionaryContractAccessor : IReadOnlyObject
    {
        public JsonDictionaryContractAccessor(
            JsonDictionaryContract contract,
            Object obj)
        {
            m_contract = contract;
            m_obj = obj;
        }

        public Int32 Count
        {
            get
            {
                var genericMethod = s_getCountTemplate.Value.MakeGenericMethod(m_contract.DictionaryValueType);
                return (Int32)genericMethod.Invoke(null, new[] { m_obj });
            }
        }

        public IEnumerable<String> Keys
        {
            get
            {
                var genericMethod = s_getKeysTemplate.Value.MakeGenericMethod(m_contract.DictionaryValueType);
                return genericMethod.Invoke(null, new[] { m_obj }) as IEnumerable<String>;
            }
        }

        public IEnumerable<Object> Values => Keys.Select(x => this[x]);

        public Object this[String key]
        {
            get
            {
                if (TryGetValue(key, out Object value))
                {
                    return value;
                }

                throw new KeyNotFoundException(ExpressionResources.KeyNotFound(key));
            }
        }

        public Boolean ContainsKey(String key)
        {
            return TryGetValue(key, out _);
        }

        public IEnumerator<KeyValuePair<String, Object>> GetEnumerator()
        {
            return Keys.Select(x => new KeyValuePair<String, Object>(x, this[x])).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Keys.Select(x => new KeyValuePair<String, Object>(x, this[x])).GetEnumerator();
        }

        public Boolean TryGetValue(
            String key,
            out Object value)
        {
            var genericMethod = s_tryGetValueTemplate.Value.MakeGenericMethod(m_contract.DictionaryValueType);
            var tuple = genericMethod.Invoke(null, new[] { m_obj, key }) as Tuple<Boolean, Object>;
            value = tuple.Item2;
            return tuple.Item1;
        }

        private static Int32 GetCount<TValue>(IDictionary<String, TValue> dictionary)
        {
            return dictionary.Count;
        }

        private static IEnumerable<String> GetKeys<TValue>(IDictionary<String, TValue> dictionary)
        {
            return dictionary.Keys;
        }

        private static Tuple<Boolean, Object> TryGetValue<TValue>(
            IDictionary<String, TValue> dictionary,
            String key)
        {
            if (dictionary.TryGetValue(key, out TValue value))
            {
                return new Tuple<Boolean, Object>(true, value);
            }

            return new Tuple<Boolean, Object>(false, null);
        }

        private static Lazy<MethodInfo> s_getCountTemplate = new Lazy<MethodInfo>(() => typeof(JsonDictionaryContractAccessor).GetTypeInfo().GetMethod(nameof(GetCount), BindingFlags.NonPublic | BindingFlags.Static));
        private static Lazy<MethodInfo> s_getKeysTemplate = new Lazy<MethodInfo>(() => typeof(JsonDictionaryContractAccessor).GetTypeInfo().GetMethod(nameof(GetKeys), BindingFlags.NonPublic | BindingFlags.Static));
        private static Lazy<MethodInfo> s_tryGetValueTemplate = new Lazy<MethodInfo>(() => typeof(JsonDictionaryContractAccessor).GetTypeInfo().GetMethod(nameof(TryGetValue), BindingFlags.NonPublic | BindingFlags.Static));
        private readonly JsonDictionaryContract m_contract;
        private readonly Object m_obj;
    }
}
