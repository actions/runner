using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.Common.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Services.WebApi
{
    //extended properties are serialized with many core types,
    //so a single contract to deal with them
    //the server side TeamFoundationPropertiesService stores five types in their native format
    //: Byte[], Int32, Double, DateType and String.
    //JSON.NET deals correctly with Double, DateType and String, but can't discern the proper
    //type of Byte[] and Int32 on deserization if deserializing into Object. Byte[] gets serialized as a 
    //Base64 encoded string, and stays that way. All integers get serialized as Int64, and stay that way
    //on deserialization. Adding ItemTypeNameHandling=TypeNameHandling.All fixed Byte[] but not Int32, it turns
    //out that they only primitive type that gets the name is byte[]...
    //So we implemented the PropertiesCollectionItemConverter to preserve the type.
    //PropertyValidation accepts the 5 types named above, plus any other Primitive type (any type with a TypeCode != TypeCode.Object)
    //Except for DBNull. We also accept Guid. Types *not* in the set of five (including Guid) are stored as String in the DB
    //and come back as that from the service. There is a special TryGetValue that can be used to try to convert the type
    //from string back to the type it is supposed to be.

 
    /// <summary>
    /// The class represents a property bag as a collection of key-value pairs. Values of all primitive types (any type with a `TypeCode != TypeCode.Object`) 
    /// except for `DBNull` are accepted. Values of type Byte[], Int32, Double, DateType and String preserve their type, 
    /// other primitives are retuned as a String. Byte[] expected as base64 encoded string.
    /// </summary>
    [CollectionDataContract(Name = "Properties", ItemName = "Property", KeyName = "Key", ValueName = "Value")]
    [JsonDictionary(ItemConverterType = typeof(PropertiesCollectionItemConverter))]
    public sealed class PropertiesCollection : IDictionary<String, Object>, ICollection
    {
        public PropertiesCollection()
        {
            m_innerDictionary = new Dictionary<String, Object>(VssStringComparer.PropertyName);
            this.ValidateNewValues = true;
        }

        public PropertiesCollection(IDictionary<String, Object> source) : this(source, validateExisting: true)
        {
        }

        internal PropertiesCollection(IDictionary<String, Object> source, bool validateExisting)
        {
            if (validateExisting)
            {
                PropertyValidation.ValidateDictionary(source);
            }
            m_innerDictionary = new Dictionary<String, Object>(source, VssStringComparer.PropertyName);
            this.ValidateNewValues = true;
        }

        private Dictionary<String, Object> m_innerDictionary;

        //allow containers to turn off property validation
        internal Boolean ValidateNewValues
        {
            get;
            set;
        }

        #region Public Properties
        /// <summary>
        /// The count of properties in the collection.
        /// </summary>
        public Int32 Count
        {
            get
            {
                return m_innerDictionary.Count;
            }
        }

        /// <summary>
        /// Implements IDictionary&lt;String, Object&gt;.Item
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Object this[String key]
        {
            get
            {
                return m_innerDictionary[key];
            }
            set
            {
                if (this.ValidateNewValues)
                {
                    PropertyValidation.ValidatePropertyName(key);
                    PropertyValidation.ValidatePropertyValue(key, value);
                }

                m_innerDictionary[key] = value;
            }
        }

        /// <summary>
        /// The set of keys in the collection.
        /// </summary>
        public Dictionary<String, Object>.KeyCollection Keys
        {
            get
            {
                return m_innerDictionary.Keys;
            }
        }

        /// <summary>
        /// The set of values in the collection.
        /// </summary>
        public Dictionary<String, Object>.ValueCollection Values
        {
            get
            {
                return m_innerDictionary.Values;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Implements IDictionary&lt;String, Object&gt;.Add
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(String key, Object value)
        {
            if (this.ValidateNewValues)
            {
                PropertyValidation.ValidatePropertyName(key);
                PropertyValidation.ValidatePropertyValue(key, value);
            }

            m_innerDictionary.Add(key, value);
        }

        /// <summary>
        /// Implements ICollection&lt;KeyValuePair&lt;String, Object&gt;&gt;.Clear()
        /// </summary>
        public void Clear()
        {
            m_innerDictionary.Clear();
        }

        /// <summary>
        /// Implements IDictionary&lt;String, Object&gt;.ContainsKey()
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean ContainsKey(String key)
        {
            return m_innerDictionary.ContainsKey(key);
        }

        /// <summary>
        /// Implements IDictionary&lt;String, Object&gt;.ContainsValue()
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean ContainsValue(Object value)
        {
            return m_innerDictionary.ContainsValue(value);
        }

        /// <summary>
        /// Implements IDictionary&lt;String, Object&gt;.Remove()
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Boolean Remove(String key)
        {
            return m_innerDictionary.Remove(key);
        }

        public T GetValue<T>(String key, T defaultValue)
        {
            T value;
            if (!TryGetValue<T>(key, out value))
            {
                value = defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Implements IDictionary&lt;String, Object&gt;.TryGetValue()
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryGetValue(String key, out Object value)
        {
            return m_innerDictionary.TryGetValue(key, out value);
        }

        public Boolean TryGetValue<T>(String key, out T value)
        {
            return this.TryGetValidatedValue(key, out value);
        }

        public override Boolean Equals(Object otherObj)
        {
            if (Object.ReferenceEquals(this, otherObj))
            {
                return true;
            }

            PropertiesCollection otherCollection = otherObj as PropertiesCollection;
            if (otherCollection == null || Count != otherCollection.Count)
            {
                return false;
            }
            else
            {
                Object obj;
                foreach (var key in Keys)
                {
                    if (!otherCollection.TryGetValue(key, out obj) || !obj.Equals(this[key]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public override Int32 GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region ICollection explicit implementation
        //We implement ICollection to get the SyncRoot
        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)m_innerDictionary).CopyTo(array, index);
        }

        Boolean ICollection.IsSynchronized
        {
            get
            {
                return ((ICollection)m_innerDictionary).IsSynchronized;
            }
        }

        Object ICollection.SyncRoot
        {
            get
            {
                return ((ICollection)m_innerDictionary).SyncRoot;
            }
        }
        #endregion

        #region ICollection<KeyValuePair<String, Object> explicit implementation
        void ICollection<KeyValuePair<String, Object>>.Add(KeyValuePair<String, Object> keyValuePair)
        {
            if (this.ValidateNewValues)
            {
                PropertyValidation.ValidatePropertyName(keyValuePair.Key);
                PropertyValidation.ValidatePropertyValue(keyValuePair.Key, keyValuePair.Value);
            }

            ((ICollection<KeyValuePair<String, Object>>)m_innerDictionary).Add(keyValuePair);
        }

        Boolean ICollection<KeyValuePair<String, Object>>.Contains(KeyValuePair<String, Object> keyValuePair)
        {
            return ((ICollection<KeyValuePair<String, Object>>)m_innerDictionary).Contains(keyValuePair);
        }

        void ICollection<KeyValuePair<String, Object>>.CopyTo(KeyValuePair<String, Object>[] array, Int32 index)
        {
            ((ICollection<KeyValuePair<String, Object>>)m_innerDictionary).CopyTo(array, index);
        }

        Boolean ICollection<KeyValuePair<String, Object>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        Boolean ICollection<KeyValuePair<String, Object>>.Remove(KeyValuePair<String, Object> keyValuePair)
        {
            return ((ICollection<KeyValuePair<String, Object>>)m_innerDictionary).Remove(keyValuePair);
        }
        #endregion

        #region IDictionary<String, Object> explicit implementation
        ICollection<String> IDictionary<String, Object>.Keys
        {
            get
            {
                return ((IDictionary<String, Object>)m_innerDictionary).Keys;
            }
        }

        ICollection<Object> IDictionary<String, Object>.Values
        {
            get
            {
                return ((IDictionary<String, Object>)m_innerDictionary).Values;
            }
        }
        #endregion

        #region IEnumerable<KeyValuePair<String, Object>> explicit implementation
        IEnumerator<KeyValuePair<String, Object>> IEnumerable<KeyValuePair<String, Object>>.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<String, Object>>)m_innerDictionary).GetEnumerator();
        }
        #endregion

        #region IEnumerable implementation
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)m_innerDictionary).GetEnumerator();
        }
        #endregion

        #region PropertiesCollectionItemConverter class
        internal class PropertiesCollectionItemConverter : JsonConverter
        {
            public PropertiesCollectionItemConverter() { }

            private const string TypePropertyName = "$type";
            private const string ValuePropertyName = "$value";

            /// <summary>
            /// Writes the JSON representation of the object.
            /// </summary>
            /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
            /// <param name="value">The value.</param>
            /// <param name="serializer">The calling serializer.</param>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                Type valueType = value.GetType();

                // We don't want to use the type name of the enum itself; instead we marshal
                // it as a decimal number inside of a string
                if (valueType.GetTypeInfo().IsEnum)
                {
                    value = ((Enum)value).ToString("D");
                    valueType = typeof(String);
                }

                PropertyValidation.ValidatePropertyValue(WebApiResources.SerializingPhrase(), value);

                //write out as an object with type information
                writer.WriteStartObject();
                writer.WritePropertyName(TypePropertyName);

                // Check that the Type we're claiming is safely deserializable
                String typeName = valueType.FullName;

                if (!PropertyValidation.IsValidTypeString(typeName))
                {
                    throw new PropertyTypeNotSupportedException(TypePropertyName, valueType);
                }

                writer.WriteValue(typeName);
                writer.WritePropertyName(ValuePropertyName);
                writer.WriteValue(value);
                writer.WriteEndObject();
            }

            /// <summary>
            /// Reads the JSON representation of the object.
            /// </summary>
            /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
            /// <param name="objectType">Type of the object.</param>
            /// <param name="existingValue">The existing value of object being read.</param>
            /// <param name="serializer">The calling serializer.</param>
            /// <returns>The object value.</returns>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    JObject valueInfo = serializer.Deserialize<JObject>(reader);
                    if (!valueInfo.TryGetValue(TypePropertyName, out JToken typeToken) ||
                        !valueInfo.TryGetValue(ValuePropertyName, out JToken valueToken))
                    {
                        // The following block is for compatability with old code behavior.
                        // The old code blindly took the first argument add treated it as the $type string,
                        // It blindly took the second argument and treated it as the $value object.
                        IEnumerator<JToken> tokenEnumerator = valueInfo.Values().GetEnumerator();
                        if (tokenEnumerator.MoveNext())
                        {
                            typeToken = tokenEnumerator.Current;
                            if (tokenEnumerator.MoveNext())
                            {
                                valueToken = tokenEnumerator.Current;
                            }
                            else
                            {
                                throw new InvalidOperationException(WebApiResources.DeserializationCorrupt());
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException(WebApiResources.DeserializationCorrupt());
                        }
                    }

                    string typeToCreate = typeToken.ToObject<string>();

                    //make sure the string is a valid type,
                    //an arbitrary type string with nested generics could overflow the
                    //stack for a DOS.
                    if (!PropertyValidation.TryGetValidType(typeToCreate, out Type type))
                    {
                        throw new InvalidOperationException(WebApiResources.DeserializationCorrupt());
                    }

                    //deserialize the type
                    return valueToken.ToObject(type);
                }
                else if (reader.TokenType == JsonToken.Boolean ||
                         reader.TokenType == JsonToken.Bytes ||
                         reader.TokenType == JsonToken.Date ||
                         reader.TokenType == JsonToken.Float ||
                         reader.TokenType == JsonToken.Integer ||
                         reader.TokenType == JsonToken.String)
                {
                    // Allow the JSON to simply specify "name": value syntax if type information is not necessary.
                    return serializer.Deserialize(reader);
                }
                else if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }
                else
                {
                    throw new InvalidOperationException(WebApiResources.DeserializationCorrupt());
                }
            }

            /// <summary>
            /// Determines whether this instance can convert the specified object type.
            /// </summary>
            /// <param name="objectType">Type of the object.</param>
            /// <returns>
            /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
            /// </returns>
            public override Boolean CanConvert(Type objectType)
            {
                return true;
            }
        }
        #endregion
    }
}
