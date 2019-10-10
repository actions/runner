using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechanism for getting and setting resource properties.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [JsonConverter(typeof(ResourcePropertiesJsonConverter))]
    public class ResourceProperties
    {
        public ResourceProperties()
        {
        }

        internal ResourceProperties(IDictionary<String, JToken> items)
        {
            m_items = new Dictionary<String, JToken>(items, StringComparer.OrdinalIgnoreCase);
        }

        private ResourceProperties(ResourceProperties propertiesToClone)
        {
            if (propertiesToClone?.m_items?.Count > 0)
            {
                m_items = new Dictionary<String, JToken>(propertiesToClone.m_items, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets the count of properties defined.
        /// </summary>
        public Int32 Count
        {
            get
            {
                return m_items?.Count ?? 0;
            }
        }

        internal IDictionary<String, JToken> Items
        {
            get
            {
                if (m_items == null)
                {
                    m_items = new Dictionary<String, JToken>(StringComparer.OrdinalIgnoreCase);
                }
                return m_items;
            }
        }

        public IReadOnlyDictionary<String, JToken> GetItems()
        {
            return new ReadOnlyDictionary<String, JToken>(this.Items);
        }

        public ResourceProperties Clone()
        {
            return new ResourceProperties(this);
        }

        public Boolean Delete(String name)
        {
            return this.Items.Remove(name);
        }

        public Boolean DeleteAllExcept(ISet<String> names)
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(names, nameof(names));

            Boolean removed = false;
            if (m_items?.Count > 0)
            {
                foreach (var propertyName in m_items.Keys.Where(x => !names.Contains(x)).ToArray())
                {
                    removed |= Delete(propertyName);
                }
            }

            return removed;
        }

        public T Get<T>(
            String name,
            T defaultValue = default(T))
        {
            if (this.Items.TryGetValue(name, out var tokenValue) && tokenValue != null)
            {
                if (typeof(T) == typeof(JToken))
                {
                    return (T)(Object)tokenValue;
                }
                else
                {
                    return tokenValue.ToObject<T>(s_serializer);
                }
            }

            return defaultValue;
        }

        public Boolean TryGetValue<T>(String name, out T value)
        {
            if (this.Items.TryGetValue(name, out var tokenValue) && tokenValue != null)
            {
                if (typeof(T) == typeof(JToken))
                {
                    value = (T)(Object)tokenValue;
                }
                else
                {
                    value = tokenValue.ToObject<T>(s_serializer);
                }

                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        public void Set<T>(
            String name,
            T value)
        {
            if (value == null)
            {
                this.Items[name] = null;
            }
            else if (typeof(T) == typeof(JToken))
            {
                this.Items[name] = value as JToken;
            }
            else
            {
                this.Items[name] = JToken.FromObject(value, s_serializer);
            }
        }

        public void UnionWith(
            ResourceProperties properties,
            Boolean overwrite = false)
        {
            if (properties?.m_items == null)
            {
                return;
            }

            foreach (var property in properties.m_items)
            {
                if (overwrite || !this.Items.ContainsKey(property.Key))
                {
                    this.Items[property.Key] = property.Value;
                }
            }
        }

        internal IDictionary<String, Object> ToStringDictionary()
        {
            return this.Items.ToDictionary(x => x.Key, x => ToObject(x.Value), StringComparer.OrdinalIgnoreCase);
        }

        private static Object ToObject(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return Convert.ToString((Boolean)token);
                case JTokenType.Date:
                    return Convert.ToString((DateTime)token);
                case JTokenType.Float:
                    return Convert.ToString((Single)token);
                case JTokenType.Guid:
                    return Convert.ToString((Guid)token);
                case JTokenType.Integer:
                    return Convert.ToString((Int32)token);
                case JTokenType.TimeSpan:
                    return Convert.ToString((TimeSpan)token);
                case JTokenType.Uri:
                    return Convert.ToString((Uri)token);
                case JTokenType.String:
                    return (String)token;

                case JTokenType.Array:
                    var array = token as JArray;
                    return array.Select(x => ToObject(x)).ToList();

                case JTokenType.Object:
                    return ToDictionary(token as JObject);
            }

            return null;
        }

        private static IDictionary<String, Object> ToDictionary(JObject @object)
        {
            var result = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in @object.Properties())
            {
                result[item.Name] = ToObject(item.Value);
            }
            return result;
        }

        private IDictionary<String, JToken> m_items;
        private static readonly JsonSerializer s_serializer = JsonUtility.CreateJsonSerializer();
    }

    internal class ResourcePropertiesJsonConverter : VssSecureJsonConverter
    {
        public override Boolean CanWrite
        {
            get
            {
                return true;
            }
        }

        public override Boolean CanConvert(Type objectType)
        {
            return typeof(IDictionary<String, JToken>).GetTypeInfo().IsAssignableFrom(objectType);
        }

        public override Object ReadJson(
            JsonReader reader,
            Type objectType,
            Object existingValue,
            JsonSerializer serializer)
        {
            var items = serializer.Deserialize<IDictionary<String, JToken>>(reader);
            return new ResourceProperties(items);
        }

        public override void WriteJson(
            JsonWriter writer,
            Object value,
            JsonSerializer serializer)
        {
            base.WriteJson(writer, value, serializer);

            var properties = (ResourceProperties)value;
            serializer.Serialize(writer, properties?.Items);
        }
    }
}
