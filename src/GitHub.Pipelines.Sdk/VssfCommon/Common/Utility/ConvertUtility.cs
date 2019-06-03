using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Utility class for wrapping Convert.ChangeType to handle nullable values.
    /// </summary>
    public class ConvertUtility
    {
        public static object ChangeType(object value, Type type)
        {
            return ChangeType(value, type, CultureInfo.CurrentCulture);
        }

        public static object ChangeType(object value, Type type, IFormatProvider provider)
        {
            if (type.IsOfType(typeof(Nullable<>)))
            {
                var nullableConverter = new NullableConverter(type);
                return nullableConverter.ConvertTo(value, nullableConverter.UnderlyingType);
            }

            return Convert.ChangeType(value, type, provider);
        }
    }
}
