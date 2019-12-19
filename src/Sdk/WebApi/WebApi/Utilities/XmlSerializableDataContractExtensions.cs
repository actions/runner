using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.WebApi.Xml
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = true, AllowMultiple = false)]
    public class XmlSerializableDataContractAttribute : Attribute
    {
        public XmlSerializableDataContractAttribute() { }

        public bool EnableCamelCaseNameCompat { get; set; }
    }

    /// <summary>
    /// These extensions are intended to be used alongside the <see cref="IXmlSerializable"/> interface
    /// to allow classes to leverage some of the functionality of DataContractSerializer,
    /// such as serialization of publicly immutable properties, while also supporting the conventional <see cref="XmlSerializer"/>.
    /// </summary>
    public static class XmlSerializableDataContractExtensions
    {
        private static SerializableProperties GetSerializableProperties(TypeInfo type)
        {
            if (SerializablePropertiesByType.TryGetValue(type, out var properties))
            {
                return properties;
            }

            var dataContract = type.GetCustomAttribute(typeof(XmlSerializableDataContractAttribute)) as XmlSerializableDataContractAttribute;
            var enableCamelCaseNameCompat = dataContract == null ? false : dataContract.EnableCamelCaseNameCompat;

            var declaredProperties = new List<SerializableProperty>();

            foreach (var declaredProperty in type.DeclaredProperties)
            {
                if (declaredProperty.GetCustomAttribute(typeof(XmlIgnoreAttribute)) != null)
                {
                    continue;
                }

                if (declaredProperty.SetMethod == null)
                {
                    continue;
                }

                var dataMember = declaredProperty.GetCustomAttribute(typeof(DataMemberAttribute)) as DataMemberAttribute;
                if (dataMember == null)
                {
                    continue;
                }

                var shouldSerializeMethodName = string.Concat("ShouldSerialize", declaredProperty.Name);
                var shouldSerializeMethod = type.GetDeclaredMethod(shouldSerializeMethodName);

                declaredProperties.Add(new SerializableProperty(declaredProperty, dataMember, shouldSerializeMethod, enableCamelCaseNameCompat));
            }

            var inheritedProperties = Enumerable.Empty<SerializableProperty>();
            if (type.BaseType != typeof(object))
            {
                inheritedProperties = GetSerializableProperties(type.BaseType.GetTypeInfo()).EnumeratedInOrder;
            }

            var serializableProperties = new SerializableProperties(declaredProperties, inheritedProperties);

            return SerializablePropertiesByType.GetOrAdd(type, serializableProperties);
        }

        private static XmlSerializer GetSerializer(string rootNamespace, string elementName, Type elementType)
        {
            var serializerKey = new SerializerKey(rootNamespace, elementName, elementType);
            return Serializers.GetOrAdd(serializerKey, _ =>
            {
                var rootAttribute = new XmlRootAttribute(elementName) { Namespace = rootNamespace };
                return new XmlSerializer(elementType, rootAttribute);
            });
        }

        private static ConcurrentDictionary<TypeInfo, SerializableProperties> SerializablePropertiesByType
            = new ConcurrentDictionary<TypeInfo, SerializableProperties>();

        private static ConcurrentDictionary<TypeInfo, String> NamespacesByType
            = new ConcurrentDictionary<TypeInfo, String>();

        private static ConcurrentDictionary<SerializerKey, XmlSerializer> Serializers
            = new ConcurrentDictionary<SerializerKey, XmlSerializer>();

        /// <summary>
        /// Creates a HashSet based on the elements in <paramref name="source"/>, using transformation
        /// function <paramref name="selector"/>.
        /// </summary>
        private static HashSet<TOut> ToHashSet<TIn, TOut>(
            this IEnumerable<TIn> source,
            Func<TIn, TOut> selector)
        {
            return new HashSet<TOut>(source.Select(selector));
        }

        private class SerializableProperties
        {
            public IReadOnlyDictionary<string, SerializableProperty> MappedByName { get; }

            public IReadOnlyList<SerializableProperty> EnumeratedInOrder { get; }

            public SerializableProperties(IEnumerable<SerializableProperty> declaredProperties, IEnumerable<SerializableProperty> inheritedProperties)
            {
                var declaredPropertyNames = declaredProperties.ToHashSet(property => property.SerializedName);

                // To maintain consistency with the DataContractSerializer, property ordering is determined according to the following rules:
                // 1. If a data contract type is a part of an inheritance hierarchy, data members of its base types are always first in the order.
                // 2. Next in order are the current type’s data members that do not have the Order property of the DataMemberAttribute attribute set, in alphabetical order.
                // https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/data-member-order
                EnumeratedInOrder = inheritedProperties
                    // Subclass properties should hide inherited properties with the same name
                    .Where(inheritedProperty => !declaredPropertyNames.Contains(inheritedProperty.SerializedName))
                    .Concat(declaredProperties.OrderBy(property => property.SerializedName))
                    .ToList();

                var propertiesMappedByName = new Dictionary<string, SerializableProperty>();
                foreach (var property in EnumeratedInOrder)
                {
                    propertiesMappedByName.Add(property.SerializedName, property);
                    if (property.SerializedNameForCamelCaseCompat != null)
                    {
                        propertiesMappedByName.TryAdd(property.SerializedNameForCamelCaseCompat, property);
                    }
                }
                MappedByName = propertiesMappedByName;
            }

            private Dictionary<string, SerializableProperty> PropertiesDictionary { get; }
        }

        [DebuggerDisplay("Name={SerializedName} Type={SerializedType}")]
        private class SerializableProperty
        {
            public Type SerializedType => Property.PropertyType;

            public string SerializedName { get; }

            public string SerializedNameForCamelCaseCompat { get; }

            public SerializableProperty(PropertyInfo property, DataMemberAttribute dataMember, MethodInfo shouldSerializeMethod, bool enableCamelCaseNameCompat)
            {
                Property = property;
                DataMember = dataMember;
                ShouldSerializeMethod = shouldSerializeMethod;

                SerializedName = DataMember?.Name ?? Property.Name;
                SerializedNameForCamelCaseCompat = ComputeSerializedNameForCameCaseCompat(enableCamelCaseNameCompat);
            }

            public object GetValue(object @object) => Property.GetValue(@object);

            public void SetValue(object @object, object value) => Property.SetValue(@object, value);

            public bool ShouldSerialize(object @object)
                => ShouldSerializeMethod == null ? true : (bool)ShouldSerializeMethod.Invoke(@object, new object[] { });

            public bool IsIgnorableDefaultValue(object value)
            {
                if (DataMember.EmitDefaultValue)
                {
                    return false;
                }

                var serializedType = SerializedType;
                if (serializedType.GetTypeInfo().IsValueType)
                {
                    var defaultValue = DefaultValuesByType.GetOrAdd(serializedType, key => Activator.CreateInstance(key));
                    return Equals(value, defaultValue);
                }

                return value == null;
            }

            private string ComputeSerializedNameForCameCaseCompat(bool enableCamelCaseNameCompat)
            {
                if (!enableCamelCaseNameCompat)
                {
                    return null;
                }

                var upperCamelCaseName = ConvertToUpperCamelCase(SerializedName);

                if (string.Equals(upperCamelCaseName, SerializedName))
                {
                    return null;
                }

                return upperCamelCaseName;
            }

            private static string ConvertToUpperCamelCase(string input)
            {
                return string.Concat(char.ToUpperInvariant(input[0]), input.Substring(1));
            }

            private PropertyInfo Property { get; }
            private DataMemberAttribute DataMember { get; }
            private MethodInfo ShouldSerializeMethod { get; }
            private static ConcurrentDictionary<Type, object> DefaultValuesByType = new ConcurrentDictionary<Type, object>();
        }

        private struct SerializerKey
        {
            public string RootNamespace { get; }

            public string ElementName { get; }

            public Type ElementType { get; }

            public SerializerKey(string rootNamespace, string elementName, Type elementType)
            {
                // root namespace can be null, but element name and type must be nonnull
                ArgumentUtility.CheckForNull(elementName, nameof(elementName));
                ArgumentUtility.CheckForNull(elementType, nameof(elementType));
                RootNamespace = rootNamespace;
                ElementName = elementName;
                ElementType = elementType;
            }

            public override bool Equals(object other)
            {
                if (other is SerializerKey)
                {
                    var otherKey = (SerializerKey)other;
                    return RootNamespace == otherKey.RootNamespace
                        && ElementName == otherKey.ElementName
                        && ElementType == otherKey.ElementType;
                }

                return false;
            }

            public override int GetHashCode()
            {
                int hashCode = 7443; // "large" prime to start the seed
                
                // Bitshifting and subtracting once is an efficient way to multiply by our second "large" prime, 0x7ffff = 524287
                hashCode = (hashCode << 19) - hashCode + (RootNamespace?.GetHashCode() ?? 0);
                hashCode = (hashCode << 19) - hashCode + ElementName.GetHashCode();
                hashCode = (hashCode << 19) - hashCode + ElementType.GetHashCode();

                return hashCode;
            }
        }

    }
}
