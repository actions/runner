using System;
using System.ComponentModel;
using System.Globalization;

namespace GitHub.Actions.Pipelines.WebApi
{
    /// <summary>
    /// Parses known enum flags in a comma-separated string. Unknown flags are ignored. Allows for degraded compatibility without serializing enums to integer values.
    /// </summary>
    /// <remarks>
    /// Case insensitive. Both standard and EnumMemberAttribute names are parsed.
    /// json deserialization doesn't happen for query parameters :)
    /// </remarks>
    public class KnownFlagsEnumTypeConverter : EnumConverter
    {
        public KnownFlagsEnumTypeConverter(Type type)
            : base(type)
        {
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <exception cref="FormatException">Thrown if a flag name is empty.</exception>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                try
                {
                    return FlagsEnum.ParseKnownFlags(EnumType, stringValue);
                }
                catch (Exception ex)
                {
                    // Matches the exception type thrown by EnumTypeConverter.
                    throw new FormatException(PipelinesWebApiResources.InvalidFlagsEnumValue(stringValue, EnumType), ex);
                }
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}
