using System;
using System.ComponentModel;

namespace GitHub.Services.Common
{
    public static class SecureCompare
    {
        /// <summary>
        /// Compare two byte arrays for byte-by-byte equality.
        /// If both arrays are the same length, the running time of this routine will not vary with the number of equal bytes between the two.
        /// </summary>
        /// <param name="lhs">A byte array (non-null)</param>
        /// <param name="rhs">A byte array (non-null)</param>
        /// <remarks>
        /// Checking secret values using built-in equality operators is insecure.
        /// Operations like `==` on strings will stop the comparison when the first unmatched character is encountered.
        /// When checking secret values from an untrusted source that we use for authentication, we must be careful
        /// not to stop the comparison early for incorrect values.
        /// If we do, an attacker can send a large volume of requests and use statistical methods to infer the secret value byte-by-byte.
        ///
        /// This method is intended to be used with arrays of the same length -- for example, two hashes from the same SHA algorithm.
        /// Comparing strings of unequal length can leak length information to an attacker.
        /// </remarks>
        public static bool TimeInvariantEquals(byte[] lhs, byte[] rhs)
        {
            if (lhs.Length != rhs.Length)
            {
                return false;
            }

            // Must use bitwise operations
            // Conditional branching or short-circuiting Boolean operators would change the running time depending on the result
            int result = 0;
            for (int i = 0; i < lhs.Length; i++)
            {
                result |= lhs[i] ^ rhs[i];
            }

            return result == 0;
        }

        // Hide the `Equals` method inherited from `object`
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new static bool Equals(object lhs, object rhs)
        {
            throw new NotImplementedException($"This is not the secure equals method! Use `{nameof(SecureCompare.TimeInvariantEquals)}` instead.");
        }
    }
}
