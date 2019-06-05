using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Licensing
{
    /// <summary>
    /// Performs type conversion for a License
    /// </summary>
    internal sealed class LicenseTypeConverter : TypeConverter
    {
        /// <summary>Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.</summary>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        /// <param name="sourceType">A <see cref="T:System.Type" /> that represents the type you want to convert from. </param>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>Returns whether this converter can convert the object to the specified type, using the specified context.</summary>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        /// <param name="destinationType">A <see cref="T:System.Type" /> that represents the type you want to convert to. </param>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>Converts the given object to the type of this converter, using the specified context and culture information.</summary>
        /// <returns>An <see cref="T:System.Object" /> that represents the converted value.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        /// <param name="culture">The <see cref="T:System.Globalization.CultureInfo" /> to use as the current culture. </param>
        /// <param name="value">The <see cref="T:System.Object" /> to convert. </param>
        /// <exception cref="T:System.NotSupportedException">The conversion cannot be performed. </exception>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            var text = value as string;
            if (text != null)
            {
                return License.Parse(text);
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>Converts the given value object to the specified type, using the specified context and culture information.</summary>
        /// <returns>An <see cref="T:System.Object" /> that represents the converted value.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        /// <param name="culture">A <see cref="T:System.Globalization.CultureInfo" />. If null is passed, the current culture is assumed. </param>
        /// <param name="value">The <see cref="T:System.Object" /> to convert. </param>
        /// <param name="destinationType">The <see cref="T:System.Type" /> to convert the <paramref name="value" /> parameter to. </param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="destinationType" /> parameter is null. </exception>
        /// <exception cref="T:System.NotSupportedException">The conversion cannot be performed. </exception>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            var license = value as License;
            if (license != null && destinationType == typeof(string))
            {
                return license.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>Returns whether the given value object is valid for this type and for the specified context.</summary>
        /// <returns>true if the specified value is valid for this object; otherwise, false.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        /// <param name="value">The <see cref="T:System.Object" /> to test for validity. </param>
        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            var text = value as string;
            if (text != null)
            {
                License license;
                return License.TryParse(text, out license);
            }

            return base.IsValid(context, value);
        }
    }
}
