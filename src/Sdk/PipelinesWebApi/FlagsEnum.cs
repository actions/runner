using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Actions.Pipelines.WebApi
{
    public static class FlagsEnum
    {
        public static TEnum ParseKnownFlags<TEnum>(string stringValue) where TEnum : System.Enum
        {
            return (TEnum)ParseKnownFlags(typeof(TEnum), stringValue);
        }

        /// <summary>
        /// Parse known enum flags in a comma-separated string. Unknown flags are ignored. Allows for degraded compatibility without serializing enums to integers.
        /// </summary>
        /// <remarks>
        /// Case insensitive. Both standard and EnumMemberAttribute names are parsed.
        /// </remarks>
        /// <exception cref="NullReferenceException">Thrown if stringValue is null.</exception>
        /// <exception cref="ArgumentException">Thrown if a flag name is empty.</exception>
        public static object ParseKnownFlags(Type enumType, string stringValue)
        {
            ArgumentUtility.CheckForNull(enumType, nameof(enumType));
            if (!enumType.IsEnum)
            {
                throw new ArgumentException(PipelinesWebApiResources.FlagEnumTypeRequired());
            }

            // Check for the flags attribute in debug. Skip this reflection in release.
            Debug.Assert(enumType.GetCustomAttributes(typeof(FlagsAttribute), inherit: false).Any(), "FlagsEnum only intended for enums with the Flags attribute.");

            // The exception types below are based on Enum.TryParseEnum (http://index/?query=TryParseEnum&rightProject=mscorlib&file=system%5Cenum.cs&rightSymbol=bhaeh2vnegwo)
            if (stringValue == null)
            {
                throw new ArgumentNullException(stringValue);
            }

            if (String.IsNullOrWhiteSpace(stringValue))
            {
                throw new ArgumentException(PipelinesWebApiResources.NonEmptyEnumElementsRequired(stringValue));
            }

            if (UInt64.TryParse(stringValue, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out ulong ulongValue))
            {
                return Enum.Parse(enumType, stringValue);
            }

            var enumNames = Enum.GetNames(enumType).ToHashSet(name => name, StringComparer.OrdinalIgnoreCase);
            var enumMemberMappings = new Lazy<IDictionary<string, string>>(() =>
            {
                IDictionary<string, string> mappings = null;
                foreach (var field in enumType.GetFields())
                {
                    if (field.GetCustomAttributes(typeof(EnumMemberAttribute), false).FirstOrDefault() is EnumMemberAttribute enumMemberAttribute)
                    {
                        if (mappings == null)
                        {
                            mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }
                        mappings.Add(enumMemberAttribute.Value, field.GetValue(null).ToString());
                    }
                }

                return mappings;
            });

            var values = stringValue.Split(s_enumSeparatorCharArray);

            var matches = new List<string>();
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i].Trim();

                if (String.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(PipelinesWebApiResources.NonEmptyEnumElementsRequired(stringValue));
                }

                if (enumNames.Contains(value))
                {
                    matches.Add(value);
                }
                else if (enumMemberMappings.Value != null && enumMemberMappings.Value.TryGetValue(value, out string matchingValue))
                {
                    matches.Add(matchingValue);
                }
            }

            if (!matches.Any())
            {
                return Enum.Parse(enumType, "0");
            }

            string matchesString = String.Join(", ", matches);
            return Enum.Parse(enumType, matchesString, ignoreCase: true);
        }

        private static readonly char[] s_enumSeparatorCharArray = new char[] { ',' };
    }
}
