using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Licensing
{
    /// <summary>
    /// Represents an Msdn license
    /// </summary>
    public sealed class MsdnLicense : License, IComparable<MsdnLicense>
    {
        /// <summary>
        /// The account user is MSDN Eligible
        /// </summary>
        public static readonly MsdnLicense Eligible = new MsdnLicense(MsdnLicenseType.Eligible);

        /// <summary>
        /// The account user has an MSDN Professional license
        /// </summary>
        public static readonly MsdnLicense Professional = new MsdnLicense(MsdnLicenseType.Professional);

        /// <summary>
        /// The account user has an MSDN Platforms license
        /// </summary>
        public static readonly MsdnLicense Platforms = new MsdnLicense(MsdnLicenseType.Platforms);

        /// <summary>
        /// The account user has an MSDN TestProfessional license
        /// </summary>
        public static readonly MsdnLicense TestProfessional = new MsdnLicense(MsdnLicenseType.TestProfessional);

        /// <summary>
        /// The account user has an MSDN Premium license
        /// </summary>
        public static readonly MsdnLicense Premium = new MsdnLicense(MsdnLicenseType.Premium);

        /// <summary>
        /// The account user has an MSDN Ultimate license
        /// </summary>
        public static readonly MsdnLicense Ultimate = new MsdnLicense(MsdnLicenseType.Ultimate);

        /// <summary>
        /// The account user has an MSDN Enterprise license
        /// </summary>
        public static readonly MsdnLicense Enterprise = new MsdnLicense(MsdnLicenseType.Enterprise);

        /// <summary>
        /// Initializes an instance of the <see cref="MsdnLicense"/> class
        /// </summary>
        /// <param name="license">The type of license</param>
        private MsdnLicense(MsdnLicenseType license)
            : base(LicensingSource.Msdn, typeof(MsdnLicenseType), (int)license)
        {
        }

        /// <summary>
        /// Gets the license type for the license
        /// </summary>
        public MsdnLicenseType License
        {
            get { return (MsdnLicenseType)this.GetLicenseAsInt32(); }
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />. </returns>
        /// <param name="other">An object to compare with this object.</param>
        public int CompareTo(MsdnLicense other)
        {
            return Compare(this, other);
        }

        /// <summary>
        /// Compares two objects of the same type.
        /// </summary>
        /// <returns>A value that indicates the relative order of the objects being compared.</returns>
        /// <param name="left">The left-hand operand to compare</param>
        /// <param name="right">The right-hand operand to compare</param>
        public static int Compare(MsdnLicense left, MsdnLicense right)
        {
            if (object.ReferenceEquals(left, null))
            {
                if (object.ReferenceEquals(right, null)) return 0;
                return -1;
            }
            else if (object.ReferenceEquals(right, null))
            {
                return +1;
            }

            return LicenseComparer.Instance.Compare(left, right);
        }

        /// <summary>
        /// Compares two objects of the same type.
        /// </summary>
        /// <returns>True if the left-hand value is greater than the right-hand value; otherwise, false</returns>
        /// <param name="left">The left-hand operand to compare</param>
        /// <param name="right">The right-hand operand to compare</param>
        public static bool operator >(MsdnLicense left, MsdnLicense right)
        {
            return Compare(left, right) > 0;
        }

        /// <summary>
        /// Compares two objects of the same type.
        /// </summary>
        /// <returns>True if the left-hand value is less than the right-hand value; otherwise, false</returns>
        /// <param name="left">The left-hand operand to compare</param>
        /// <param name="right">The right-hand operand to compare</param>
        public static bool operator <(MsdnLicense left, MsdnLicense right)
        {
            return Compare(left, right) < 0;
        }

            /// <summary>
            /// Gets a <see cref="License"/> instance for the provided license type
            /// </summary>
            /// <param name="license">The type of license</param>
            /// <returns>A license for the provided license type</returns>
            /// <exception cref="ArgumentOutOfRangeException"><em>license</em> was not in the list of allowed values</exception>
            public static License GetLicense(MsdnLicenseType license)
        {
            switch (license)
            {
                case MsdnLicenseType.None: return None;
                case MsdnLicenseType.Eligible: return Eligible;
                case MsdnLicenseType.Professional: return Professional;
                case MsdnLicenseType.Platforms: return Platforms;
                case MsdnLicenseType.TestProfessional: return TestProfessional;
                case MsdnLicenseType.Premium: return Premium;
                case MsdnLicenseType.Ultimate: return Ultimate;
                case MsdnLicenseType.Enterprise: return Enterprise;
            default:
                    throw new InvalidEnumArgumentException("license", (int)license, typeof(MsdnLicenseType));
            }
        }
    }
}
