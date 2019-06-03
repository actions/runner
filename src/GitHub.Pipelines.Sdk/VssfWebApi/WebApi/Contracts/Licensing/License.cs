using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;

namespace GitHub.Services.Licensing
{
    /// <summary>
    /// The base class for a specific license source and license
    /// </summary>
    [JsonConverter(typeof(LicenseJsonConverter))]
    [TypeConverter(typeof(LicenseTypeConverter))]
    [JsonObject]
    [DebuggerDisplay("{ToString(), nq}")]
    public abstract class License : IEquatable<License>
    {
        /// <summary>
        /// Represents a non-existent license
        /// </summary>
        public static readonly License None = new NoLicense();

        /// <summary>
        /// Represents a license that is auto assigned at user sign-in (e.g. from msdn licenses)
        /// </summary>
        public static readonly License Auto = new AutoLicense();

        private Type licenseEnumType;
        private int license;

        /// <summary>
        /// Initializes a new instance of the License type
        /// </summary>
        /// <param name="source">The source of the license</param>
        /// <param name="licenseEnumType">The type for the license enum</param>
        /// <param name="license">The value for the license</param>
        internal License(LicensingSource source, Type licenseEnumType, int license)
        {
            this.licenseEnumType = licenseEnumType;
            this.license = license;
            this.Source = source;
        }

        /// <summary>
        /// Gets the source of the license
        /// </summary>
        public LicensingSource Source { get; private set; }

        /// <summary>
        /// Gets the internal value for the license
        /// </summary>
        internal int GetLicenseAsInt32()
        {
            return this.license;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current <see cref="License"/>.</returns>
        public override int GetHashCode()
        {
            return this.Source.GetHashCode()
                 ^ this.license.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified System.Object is equal to the current <see cref="License"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as License);
        }

        /// <summary>
        /// Determines whether the specified <see cref="License"/> is equal to the current <see cref="License"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(License obj)
        {
            return obj != null
                && this.Source == obj.Source
                && this.license == obj.license;
        }

        /// <summary>
        ///  Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(this.Source.ToString());
            sb.Append('-');
            sb.Append(Enum.GetName(this.licenseEnumType, this.license));
            return sb.ToString();
        }

        /// <summary>
        /// Parses the provided text into a <see cref="License"/>
        /// </summary>
        /// <param name="text">The text to parse</param>
        /// <returns>The parsed <see cref="License"/></returns>
        /// <exception cref="FormatException">The <em>text</em> was in the wrong format</exception>
        public static License Parse(string text)
        {
            return Parse(text, ignoreCase: false);
        }

        /// <summary>
        /// Parses the provided text into a <see cref="License"/>
        /// </summary>
        /// <param name="text">The text to parse</param>
        /// <param name="ignoreCase">A value indicating whether to ignore the case of the text</param>
        /// <returns>The parsed <see cref="License"/></returns>
        /// <exception cref="FormatException">The <em>text</em> was in the wrong format</exception>
        public static License Parse(string text, bool ignoreCase)
        {
            License license;
            if (!TryParse(text, ignoreCase, out license))
            {
                throw new FormatException();
            }

            return license;
        }

        /// <summary>
        /// Tries to parse the provided text into a <see cref="License"/>
        /// </summary>
        /// <param name="text">The text to parse</param>
        /// <param name="license">The parsed <see cref="License"/></param>
        /// <returns>True if the <see cref="License"/> could be parsed; otherwise, false</returns>
        public static bool TryParse(string text, out License license)
        {
            return TryParse(text, false, out license);
        }

        /// <summary>
        /// Tries to parse the provided text into a <see cref="License"/>
        /// </summary>
        /// <param name="text">The text to parse</param>
        /// <param name="ignoreCase">A value indicating whether to ignore the case of the text</param>
        /// <param name="license">The parsed <see cref="License"/></param>
        /// <returns>True if the <see cref="License"/> could be parsed; otherwise, false</returns>
        public static bool TryParse(string text, bool ignoreCase, out License license)
        {
            license = None;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var parts = text.Split('-');

            LicensingSource source;
            if (!Enum.TryParse(parts[0], ignoreCase, out source))
            {
                return false;
            }

            if (parts.Length == 1 && source == LicensingSource.None)
            {
                return true;
            }

            if (parts.Length == 1 && source == LicensingSource.Auto)
            {
                license = Auto;
                return true;
            }

            if (parts.Length > 2)
            {
                return false;
            }

            switch (source)
            {
                case LicensingSource.Msdn:
                    MsdnLicenseType msdnLicense;
                    if (Enum.TryParse(parts[1], ignoreCase, out msdnLicense) && msdnLicense != MsdnLicenseType.None)
                    {
                        license = MsdnLicense.GetLicense(msdnLicense);
                        return true;
                    }

                    break;

                case LicensingSource.Account:
                    AccountLicenseType accountLicense;
                    if (Enum.TryParse(parts[1], ignoreCase, out accountLicense) && accountLicense != AccountLicenseType.None)
                    {
                        license = AccountLicense.GetLicense(accountLicense);
                        return true;
                    }

                    break;

                case LicensingSource.Auto:
                    LicensingSource licenseSource;
                    if (Enum.TryParse(parts[1], ignoreCase, out licenseSource))
                    {
                        license = AutoLicense.GetLicense(licenseSource);
                        return true;
                    }

                    break;
            }

            return false;
        }

        /// <summary>
        /// Gets a value indicating whether the two provided values are equivalent
        /// </summary>
        /// <param name="left">The first value</param>
        /// <param name="right">The second value</param>
        /// <returns>True if both values are equivalent; otherwise, false</returns>
        public static bool Equals(License left, License right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }
            else if (object.ReferenceEquals(right, null))
            {
                return false;
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Gets a value indicating whether the license is null or <see cref="License.None"/>
        /// </summary>
        /// <param name="license">The license</param>
        /// <returns>true if the license is either null or <see cref="License.None"/>; otherwise, false</returns>
        public static bool IsNullOrNone(License license)
        {
            return license == null || license.Source == LicensingSource.None;
        }

        /// <summary>
        /// Gets the license for the provided source and license type
        /// </summary>
        /// <param name="source">The source</param>
        /// <param name="license">The license type</param>
        /// <returns>The license</returns>
        internal static License GetLicense(LicensingSource source, int license)
        {
            switch (source)
            {
                case LicensingSource.None:
                    return None;

                case LicensingSource.Account:
                    return AccountLicense.GetLicense((AccountLicenseType)license);

                case LicensingSource.Msdn:
                    return MsdnLicense.GetLicense((MsdnLicenseType)license);

                case LicensingSource.Profile:
                    throw new NotSupportedException();

                case LicensingSource.Auto:
                    return Auto;

                default:
                    throw new InvalidEnumArgumentException("source", (int)source, typeof(LicensingSource));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the two provided values are equivalent
        /// </summary>
        /// <param name="left">The first operand</param>
        /// <param name="right">The second operand</param>
        /// <returns>True if both values are equivalent; otherwise, false</returns>
        public static bool operator ==(License left, License right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Gets a value indicating whether the two provided values are not equivalent
        /// </summary>
        /// <param name="left">The first operand</param>
        /// <param name="right">The second operand</param>
        /// <returns>True if values are not equivalent; otherwise, false</returns>
        public static bool operator !=(License left, License right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Compares two objects of the same type.
        /// </summary>
        /// <returns>True if the left-hand value is greater than the right-hand value; otherwise, false</returns>
        /// <param name="left">The left-hand operand to compare</param>
        /// <param name="right">The right-hand operand to compare</param>
        public static bool operator >(License left, License right)
        {
            return LicenseComparer.Instance.Compare(left, right) > 0;
        }
        /// <summary>
        /// Compares two objects of the same type.
        /// </summary>
        /// <returns>True if the left-hand value is greater than the right-hand value; otherwise, false</returns>
        /// <param name="left">The left-hand operand to compare</param>
        /// <param name="right">The right-hand operand to compare</param>
        public static bool operator >=(License left, License right)
        {
            return LicenseComparer.Instance.Compare(left, right) >= 0;
        }

        /// <summary>
        /// Compares two objects of the same type.
        /// </summary>
        /// <returns>True if the left-hand value is less than the right-hand value; otherwise, false</returns>
        /// <param name="left">The left-hand operand to compare</param>
        /// <param name="right">The right-hand operand to compare</param>
        public static bool operator <(License left, License right)
        {
            return LicenseComparer.Instance.Compare(left, right) < 0;
        }

        /// <summary>
        /// Compares two objects of the same type.
        /// </summary>
        /// <returns>True if the left-hand value is less than the right-hand value; otherwise, false</returns>
        /// <param name="left">The left-hand operand to compare</param>
        /// <param name="right">The right-hand operand to compare</param>
        public static bool operator <=(License left, License right)
        {
            return LicenseComparer.Instance.Compare(left, right) <= 0;
        }

        /// <summary>
        /// A concrete <see cref="License"/> that represents no license
        /// </summary>
        private sealed class NoLicense : License
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="NoLicense"/> class
            /// </summary>
            internal NoLicense()
                : base(LicensingSource.None, null, 0)
            {
            }

            /// <summary>
            ///  Returns a string that represents the current object.
            /// </summary>
            /// <returns>A string that represents the current object.</returns>
            public override string ToString()
            {
                return "None";
            }
        }

        internal sealed class AutoLicense : License
        {
            /// <summary>
            /// Represents an Auto license where the source provider is MSDN
            /// </summary>
            internal static readonly License Msdn = GetLicense(LicensingSource.Msdn);

            /// <summary>
            /// Initializes a new instance of the <see cref="AutoLicense"/> class
            /// </summary>
            internal AutoLicense()
                : base(LicensingSource.Auto, null, 0)
            {
            }

            private AutoLicense(LicensingSource licenseSource)
                : base(LicensingSource.Auto, typeof(LicensingSource), (int) licenseSource)
            {
            }

            /// <summary>
            /// Gets a <see cref="AutoLicense"/> instance for the provided licensing source
            /// </summary>
            internal static AutoLicense GetLicense(LicensingSource source)
            {
                return new AutoLicense(source);
            }

            /// <summary>
            ///  Returns a string that represents the current object.
            /// </summary>
            /// <returns>A string that represents the current object.</returns>
            public override string ToString()
            {
                return this.GetLicenseAsInt32() == 0 ? "Auto" : base.ToString();
            }
        }
    }
}
