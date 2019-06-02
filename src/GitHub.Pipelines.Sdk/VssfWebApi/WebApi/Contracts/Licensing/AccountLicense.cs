using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Licensing
{
    /// <summary>
    /// Represents an Account license
    /// </summary>
    public sealed class AccountLicense : License, IComparable<AccountLicense>
    {
        /// <summary>
        /// An Early Adopter License
        /// </summary>
        public static readonly AccountLicense EarlyAdopter = new AccountLicense(AccountLicenseType.EarlyAdopter);

        /// <summary>
        /// A Stakeholder License
        /// </summary>
        public static readonly AccountLicense Stakeholder = new AccountLicense(AccountLicenseType.Stakeholder);

        /// <summary>
        /// An Express License
        /// </summary>
        public static readonly AccountLicense Express = new AccountLicense(AccountLicenseType.Express);

        /// <summary>
        /// A Professional License
        /// </summary>
        public static readonly AccountLicense Professional = new AccountLicense(AccountLicenseType.Professional);

        /// <summary>
        /// An Advanced License
        /// </summary>
        public static readonly AccountLicense Advanced = new AccountLicense(AccountLicenseType.Advanced);

        /// <summary>
        /// Initializes an instance of the <see cref="AccountLicense"/> class
        /// </summary>
        /// <param name="license">The type of license</param>
        private AccountLicense(AccountLicenseType license)
            : base(LicensingSource.Account, typeof(AccountLicenseType), (int)license)
        {
        }

        /// <summary>
        /// Gets the license type for the license
        /// </summary>
        public AccountLicenseType License
        {
            get { return (AccountLicenseType)this.GetLicenseAsInt32(); }
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />. </returns>
        /// <param name="other">An object to compare with this object.</param>
        public int CompareTo(AccountLicense other)
        {
            return Compare(this, other);
        }

        /// <summary>
        /// Compares two objects of the same type.
        /// </summary>
        /// <returns>A value that indicates the relative order of the objects being compared.</returns>
        /// <param name="left">The left-hand operand to compare</param>
        /// <param name="right">The right-hand operand to compare</param>
        public static int Compare(AccountLicense left, AccountLicense right)
        {
            if (object.ReferenceEquals(left, null))
            {
                if (object.ReferenceEquals(right, null))
                {
                    return 0;
                }
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
        public static bool operator >(AccountLicense left, AccountLicense right)
        {
            return Compare(left, right) > 0;
        }

        /// <summary>
        /// Compares two objects of the same type.
        /// </summary>
        /// <returns>True if the left-hand value is less than the right-hand value; otherwise, false</returns>
        /// <param name="left">The left-hand operand to compare</param>
        /// <param name="right">The right-hand operand to compare</param>
        public static bool operator <(AccountLicense left, AccountLicense right)
        {
            return Compare(left, right) < 0;
        }

        /// <summary>
        /// Gets a <see cref="License"/> instance for the provided license type
        /// </summary>
        /// <param name="license">The type of license</param>
        /// <returns>A license for the provided license type</returns>
        /// <exception cref="ArgumentOutOfRangeException"><em>license</em> was not in the list of allowed values</exception>
        public static License GetLicense(AccountLicenseType license)
        {
            switch (license)
            {
                case AccountLicenseType.None: return None;
                case AccountLicenseType.EarlyAdopter: return EarlyAdopter;
                case AccountLicenseType.Stakeholder: return Stakeholder;
                case AccountLicenseType.Express: return Express;
                case AccountLicenseType.Professional: return Professional;
                case AccountLicenseType.Advanced: return Advanced;
                default:
                    throw new InvalidEnumArgumentException("license", (int)license, typeof(AccountLicenseType));
            }
        }
    }
}
