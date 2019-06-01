using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions.CollectionAccessors
{
    internal sealed class JsonObjectContractAccessor : IReadOnlyObject
    {
        public JsonObjectContractAccessor(
            JsonObjectContract contract,
            Object obj)
        {
            m_contract = contract;
            m_obj = obj;
        }

        public Int32 Count => GetProperties().Count();

        public IEnumerable<String> Keys => GetProperties().Select(x => x.PropertyName);

        public IEnumerable<Object> Values => GetProperties().Select(x => x.ValueProvider.GetValue(m_obj));

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
            return TryGetProperty(key, out _);
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
            if (TryGetProperty(key, out JsonProperty property))
            {
                value = property.ValueProvider.GetValue(m_obj);
                return true;
            }

            value = null;
            return false;
        }

        private IEnumerable<JsonProperty> GetProperties()
        {
            return m_contract.Properties.Where(x => !x.Ignored);
        }

        private Boolean TryGetProperty(
            String key,
            out JsonProperty property)
        {
            property = m_contract.Properties.GetClosestMatchProperty(key);
            if (property != null && !property.Ignored)
            {
                return true;
            }

            property = null;
            return false;
        }

        private readonly JsonObjectContract m_contract;
        private readonly Object m_obj;
    }
}
