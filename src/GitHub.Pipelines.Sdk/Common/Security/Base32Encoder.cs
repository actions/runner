using System;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Common
{
    public static class Base32Encoder {

        #region Extenion methods for Encode/Decode
        public static string ToBase32String(this byte[] rgbData) {
            return Encode(rgbData);
        }
        public static byte[] FromBase32String(this String base32String) {
            return Decode(base32String);
        }
        #endregion

        /// <summary>
        /// Encodes a byte array into Base32 (RFC 4648)
        /// </summary>
        /// <param name="rgbData"></param>
        /// <returns></returns>
        public static String Encode(byte[] rgbData) {
            ArgumentUtility.CheckForNull(rgbData, "rgbData");

            int ncTokens = GetTokenCount(rgbData);

            char[] rgz = new char[ncTokens];
            for (int i = 0; i < ncTokens; i++) {
                rgz[i] = GetToken(rgbData, i);
            }
            return new string(rgz);
        }

        /// <summary>
        /// Decodes a Base32 string (RFC 4648) into its original byte array
        /// </summary>
        /// <param name="base32String"></param>
        /// <returns></returns>
        public static byte[] Decode(String base32String) {
            ArgumentUtility.CheckForNull(base32String, "base32String");

            // validate base32 format
            if ((base32String.Length % 8 != 0) || (base32String.Where(t => !m_rgEncodingChars.Contains(t)).Count() != 0)) {
                throw new InvalidOperationException("base32string is not a valid base32 encoding");
            }

            // initialized with all zeros
            byte[] rgbOut = new byte[GetByteCount(base32String)];

            int nBitLocation = 0;

            foreach (Char c in base32String.ToUpperInvariant()) {
                int nByteOffset = nBitLocation / 8;
                int nBitOffset = nBitLocation % 8;

                byte val = (byte)Array.IndexOf(m_rgEncodingChars, c);

                // if we hit an equals sign, we need to stop processing
                if (val == m_padNdx) { break; }

                // locate bits in val correcty respective to the byte
                int nShift = 3 - nBitOffset;
                if (nShift < 0) {
                    rgbOut[nByteOffset] |= (byte)(val >> (-nShift));
                } else {
                    rgbOut[nByteOffset] |= (byte)(val << nShift);
                }

                if ((nShift < 0) && (nByteOffset < rgbOut.Length - 1)) {
                    // remaining bits go into next byte
                    rgbOut[nByteOffset + 1] |= (byte)(val << (8 + nShift));
                }

                nBitLocation += 5;
            }

            // truncate array to actual length (will rarely do anything)
            Array.Resize<byte>(ref rgbOut, nBitLocation / 8);

            return rgbOut;
        }

        /// <summary>
        /// Calculates the number of Base32 tokens (output chars) in a byte array
        /// includes the padding tokens '='
        /// </summary>
        /// <param name="rgbData"></param>
        /// <returns></returns>
        private static int GetTokenCount(byte[] rgbData) {
            return (((rgbData.Length * 8) + 39) / 40) * 8;
        }

        /// <summary>
        /// Calculates the number of bytes that the Base32 string will convert into
        /// when decoded.
        /// </summary>
        /// <param name="szEncoded"></param>
        /// <returns></returns>
        private static int GetByteCount(String szEncoded) {
            return szEncoded.Replace("=", "").Length * 5 / 8;
        }

        /// <summary>
        /// Gets the next Base32 token from the array
        /// WARNING:  ~80% of the time of this function was index bounding, so
        /// the expensive part of that has been removed, bound your calls to this
        /// and ensure that you dont increment index forever.....
        /// </summary>
        /// <param name="rgbData"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static Char GetToken(byte[] rgbData, int index) {

            if (index < 0) { throw new IndexOutOfRangeException(); }

            // 0x20 is returned if the requested index is past the end of rgbData
            // equates to padding char "="
            byte retval = m_padNdx;

            // get location of token in bits
            int byteoffset = (index * 5) / 8;

            // is this input or padding?
            if (byteoffset < rgbData.Length) {

                // calculate which bits are used from the byte
                int bitoffset = (index * 5) % 8;
                int shift1 = bitoffset - 3;

                retval = rgbData[byteoffset];
                if (shift1 < 0) {
                    // shift right
                    retval >>= (-shift1);
                } else if (shift1 > 0) {
                    // shift left
                    retval <<= shift1;
                    // if not last byte in input, include necessary bits from next byte in token
                    if (byteoffset + 1 < rgbData.Length) {
                        int shift2 = 8 - shift1;
                        byte b = rgbData[byteoffset + 1];
                        b >>= shift2;
                        retval |= b;
                    }
                } // (shift1 == 0) {  do nothing }

                // mask to right 5 bits
                retval &= m_bitmask;
            } /* else {
                // this is in "else" to prevent running GetTokenCount() more than necessary
                if (index >= GetTokenCount(rgbData)) {
                    throw new IndexOutOfRangeException();
                }
            } */

            return m_rgEncodingChars[retval];
        }

        private const byte m_padNdx = 0x20;
        private const byte m_bitmask = 0x1F;
        private static Char[] m_rgEncodingChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567=".ToCharArray();
    }
}
