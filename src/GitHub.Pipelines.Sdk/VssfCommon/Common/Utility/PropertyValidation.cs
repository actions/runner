using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace GitHub.Services.Common.Internal
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PropertyValidation
    {
        public static void ValidateDictionary(IDictionary<String, Object> source)
        {
            ArgumentUtility.CheckForNull(source, "source");

            foreach (var entry in source)
            {
                ValidatePropertyName(entry.Key);
                ValidatePropertyValue(entry.Key, entry.Value);
            }
        }

        public static Boolean IsValidConvertibleType(Type type)
        {
            return type != null && (type.GetTypeInfo().IsEnum ||
                                    type == typeof(Object) ||
                                    type == typeof(Byte[]) ||
                                    type == typeof(Guid) ||
                                    type == typeof(Boolean) ||
                                    type == typeof(Char) ||
                                    type == typeof(SByte) ||
                                    type == typeof(Byte) ||
                                    type == typeof(Int16) ||
                                    type == typeof(UInt16) ||
                                    type == typeof(Int32) ||
                                    type == typeof(UInt32) ||
                                    type == typeof(Int64) ||
                                    type == typeof(UInt64) ||
                                    type == typeof(Single) ||
                                    type == typeof(Double) ||
                                    type == typeof(Decimal) ||
                                    type == typeof(DateTime) ||
                                    type == typeof(String)
                                   );
        }

        /// <summary>
        /// Used for deserialization checks. Makes sure that
        /// the type string presented is in the inclusion list
        /// of valid types for the property service
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Boolean IsValidTypeString(String type)
        {
            return s_validPropertyTypeStrings.ContainsKey(type);
        }

        /// <summary>
        /// Used for deserialization checks. Looks up the
        /// type string presented in the inclusion list
        /// of valid types for the property service and returns the Type object
        /// </summary>
        /// <param name="type"></param>
        /// <param name="result">Resulting type that maps to the type string</param>
        /// <returns></returns>
        public static Boolean TryGetValidType(String type, out Type result)
        {
            return s_validPropertyTypeStrings.TryGetValue(type, out result);
        }

        /// <summary>
        /// Make sure the property name conforms to the requirements for a
        /// property name.
        /// </summary>
        /// <param name="propertyName"></param>
        public static void ValidatePropertyName(String propertyName)
        {            
            ValidatePropertyString(propertyName, c_maxPropertyNameLengthInChars, "propertyName");

            // Key must not start or end in whitespace. ValidatePropertyString() checks for null and empty strings, 
            // which is why indexing on length without re-checking String.IsNullOrEmpty() is ok.
            if (Char.IsWhiteSpace(propertyName[0]) || Char.IsWhiteSpace(propertyName[propertyName.Length - 1]))
            {
                throw new VssPropertyValidationException(propertyName, CommonResources.InvalidPropertyName(propertyName));
            }
        }

        /// <summary>
        /// Make sure the property value is within the supported range of values
        /// for the type of the property specified.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public static void ValidatePropertyValue(String propertyName, Object value)
        {
            // Keep this consistent with XmlPropertyWriter.Write.
            if (null != value)
            {
                Type type = value.GetType();
                TypeCode typeCode = Type.GetTypeCode(type);

                if (type.IsEnum)
                {
                    ValidateStringValue(propertyName, ((Enum)value).ToString("D"));
                }
                else if (typeCode == TypeCode.Object && value is byte[])
                {
                    ValidateByteArray(propertyName, (byte[])value);
                }
                else if (typeCode == TypeCode.Object && value is Guid)
                {
                    //treat Guid like the other valid primitive types that
                    //don't have explicit columns, e.g. it gets stored as a string
                    ValidateStringValue(propertyName, ((Guid)value).ToString("N"));
                }
                else if (typeCode == TypeCode.Object)
                {
                    throw new PropertyTypeNotSupportedException(propertyName, type);
                }
                else if (typeCode == TypeCode.DBNull)
                {
                    throw new PropertyTypeNotSupportedException(propertyName, type);
                }
                else if (typeCode == TypeCode.Empty)
                {
                    // should be impossible with null check above, but just in case.
                    throw new PropertyTypeNotSupportedException(propertyName, type);
                }
                else if (typeCode == TypeCode.Int32)
                {
                    ValidateInt32(propertyName, (int)value);
                }
                else if (typeCode == TypeCode.Double)
                {
                    ValidateDouble(propertyName, (double)value);
                }
                else if (typeCode == TypeCode.DateTime)
                {
                    ValidateDateTime(propertyName, (DateTime)value);
                }
                else if (typeCode == TypeCode.String)
                {
                    ValidateStringValue(propertyName, (String)value);
                }
                else
                {
                    // Here are the remaining types. All are supported over in DbArtifactPropertyValueColumns.
                    // With a property definition they'll be strongly-typed when they're read back.
                    // Otherwise they read back as strings.
                    //   Boolean
                    //   Char
                    //   SByte
                    //   Byte
                    //   Int16
                    //   UInt16
                    //   UInt32
                    //   Int64
                    //   UInt64
                    //   Single
                    //   Decimal
                    ValidateStringValue(propertyName, value.ToString());
                }
            }
        }

        private static void ValidateStringValue(String propertyName, String propertyValue)
        {
            if (propertyValue.Length > c_maxStringValueLength)
            {
                throw new VssPropertyValidationException("value", CommonResources.InvalidPropertyValueSize(propertyName, typeof(String).FullName, c_maxStringValueLength));
            }
            ArgumentUtility.CheckStringForInvalidCharacters(propertyValue, "value", true);
        }

        private static void ValidateByteArray(String propertyName, Byte[] propertyValue)
        {
            if (propertyValue.Length > c_maxByteValueSize)
            {
                throw new VssPropertyValidationException("value", CommonResources.InvalidPropertyValueSize(propertyName, typeof(Byte[]).FullName, c_maxByteValueSize));
            }
        }

        private static void ValidateDateTime(String propertyName, DateTime propertyValue)
        {
            // Let users get an out of range error for MinValue and MaxValue, not a DateTimeKind unspecified error.
            if (propertyValue != DateTime.MinValue
                && propertyValue != DateTime.MaxValue)
            {
                if (propertyValue.Kind == DateTimeKind.Unspecified)
                {
                    throw new VssPropertyValidationException("value", CommonResources.DateTimeKindMustBeSpecified());
                }

                // Make sure the property value is in Universal time.
                if (propertyValue.Kind != DateTimeKind.Utc)
                {
                    propertyValue = propertyValue.ToUniversalTime();
                }
            }

            CheckRange(propertyValue, s_minAllowedDateTime, s_maxAllowedDateTime, propertyName, "value");
        }

        private static void ValidateDouble(String propertyName, Double propertyValue)
        {
            if (Double.IsInfinity(propertyValue) || Double.IsNaN(propertyValue))
            {
                throw new VssPropertyValidationException("value", CommonResources.DoubleValueOutOfRange(propertyName, propertyValue));
            }

            // SQL Server support: - 1.79E+308 to -2.23E-308, 0 and 2.23E-308 to 1.79E+308
            if (propertyValue < s_minNegative ||
                (propertyValue < 0 && propertyValue > s_maxNegative) ||
                propertyValue > s_maxPositive ||
                (propertyValue > 0 && propertyValue < s_minPositive))
            {
                throw new VssPropertyValidationException("value", CommonResources.DoubleValueOutOfRange(propertyName, propertyValue));
            }
        }

        private static void ValidateInt32(String propertyName, Int32 propertyValue)
        {
            // All values allowed.
        }

        /// <summary>
        /// Validation helper for validating all property strings.
        /// </summary>
        /// <param name="propertyString"></param>
        /// <param name="maxSize"></param>
        /// <param name="argumentName"></param>
        private static void ValidatePropertyString(String propertyString, Int32 maxSize, String argumentName)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(propertyString, argumentName);
            if (propertyString.Length > maxSize)
            {
                throw new VssPropertyValidationException(argumentName, CommonResources.PropertyArgumentExceededMaximumSizeAllowed(argumentName, maxSize));
            }
            ArgumentUtility.CheckStringForInvalidCharacters(propertyString, argumentName, true);
        }

        public static void CheckPropertyLength(String propertyValue, Boolean allowNull, Int32 minLength, Int32 maxLength, String propertyName, Type containerType, String topLevelParamName)
        {
            Boolean valueIsInvalid = false;

            if (propertyValue == null)
            {
                if (!allowNull)
                {
                    valueIsInvalid = true;
                }
            }
            else if ((propertyValue.Length < minLength) || (propertyValue.Length > maxLength))
            {
                valueIsInvalid = true;
            }

            // throw exception if the value is invalid.
            if (valueIsInvalid)
            {
                // If the propertyValue is null, just print it like an empty string.
                if (propertyValue == null)
                {
                    propertyValue = String.Empty;
                }

                if (allowNull)
                {
                    // paramName comes second for ArgumentException.
                    throw new ArgumentException(CommonResources.InvalidStringPropertyValueNullAllowed(propertyValue, propertyName, containerType.Name, minLength, maxLength), topLevelParamName);
                }
                else
                {
                    throw new ArgumentException(CommonResources.InvalidStringPropertyValueNullForbidden(propertyValue, propertyName, containerType.Name, minLength, maxLength), topLevelParamName);
                }
            }
        }

        /// <summary>
        /// Verify that a propery is within the bounds of the specified range.
        /// </summary>
        /// <param name="propertyValue">The property value</param>
        /// <param name="minValue">The minimum value allowed</param>
        /// <param name="maxValue">The maximum value allowed</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="containerType">The container of the property</param>
        /// <param name="topLevelParamName">The top level parameter name</param>
        public static void CheckRange<T>(T propertyValue, T minValue, T maxValue, String propertyName, Type containerType, String topLevelParamName)
            where T : IComparable<T>
        {
            if (propertyValue.CompareTo(minValue) < 0 || propertyValue.CompareTo(maxValue) > 0)
            {
                // paramName comes first for ArgumentOutOfRangeException.
                throw new ArgumentOutOfRangeException(topLevelParamName, CommonResources.ValueTypeOutOfRange(propertyValue, propertyName, containerType.Name, minValue, maxValue));
            }
        }

        private static void CheckRange<T>(T propertyValue, T minValue, T maxValue, String propertyName, String topLevelParamName)
            where T : IComparable<T>
        {
            if (propertyValue.CompareTo(minValue) < 0 || propertyValue.CompareTo(maxValue) > 0)
            {
                // paramName comes first for ArgumentOutOfRangeException.
                throw new ArgumentOutOfRangeException(topLevelParamName, CommonResources.VssPropertyValueOutOfRange(propertyName, propertyValue, minValue, maxValue));
            }
        }

        /// <summary>
        /// Make sure the property filter conforms to the requirements for a
        /// property filter.
        /// </summary>
        /// <param name="propertyNameFilter"></param>
        public static void ValidatePropertyFilter(String propertyNameFilter)
        {
            PropertyValidation.ValidatePropertyString(propertyNameFilter, c_maxPropertyNameLengthInChars, "propertyNameFilter");
        }

        // Limits on the sizes of property values
        private const Int32 c_maxPropertyNameLengthInChars = 400;
        private const Int32 c_maxByteValueSize = 8 * 1024 * 1024;
        private const Int32 c_maxStringValueLength = 4 * 1024 * 1024;

        // Minium date time allowed for a property value.
        private static readonly DateTime s_minAllowedDateTime = new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Maximum date time allowed for a property value.
        // We can't preserve DateTime.MaxValue faithfully because SQL's cut-off is 3 milliseconds lower. Also to handle UTC to Local shifts, we give ourselves a buffer of one day.
        private static readonly DateTime s_maxAllowedDateTime = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc).AddDays(-1);

        private static Double s_minNegative = Double.Parse("-1.79E+308", CultureInfo.InvariantCulture);
        private static Double s_maxNegative = Double.Parse("-2.23E-308", CultureInfo.InvariantCulture);
        private static Double s_minPositive = Double.Parse("2.23E-308", CultureInfo.InvariantCulture);
        private static Double s_maxPositive = Double.Parse("1.79E+308", CultureInfo.InvariantCulture);

        private static readonly Dictionary<String, Type> s_validPropertyTypeStrings = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase)
        {
            //primitive types:
            //(NO DBNull or Empty)
            { "System.Boolean", typeof(Boolean) },
            { "System.Byte", typeof(Byte) },
            { "System.Char", typeof(Char) },
            { "System.DateTime", typeof(DateTime) },
            { "System.Decimal", typeof(Decimal) },
            { "System.Double", typeof(Double) },
            { "System.Int16", typeof(Int16) },
            { "System.Int32", typeof(Int32) },
            { "System.Int64", typeof(Int64) },
            { "System.SByte", typeof(SByte) },
            { "System.Single", typeof(Single) },
            { "System.String", typeof(String) },
            { "System.UInt16", typeof(UInt16) },
            { "System.UInt32", typeof(UInt32) },
            { "System.UInt64", typeof(UInt64) },
            
            //other valid types
            { "System.Byte[]", typeof(Byte[]) },
            { "System.Guid", typeof(Guid) }
        };
    }
}
