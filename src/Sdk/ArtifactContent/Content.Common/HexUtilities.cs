using System;
using System.Linq;
using System.Text;

namespace GitHub.Services.Content.Common
{
    public static class HexUtilities
    {
        public static bool IsHexString(this string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            // Single-dashes between the hexadecimal values are NOT allowed.
            return data.Length % 2 == 0 && data.All(c => (c >= '0' && c <= '9') ||
                                                         (c >= 'a' && c <= 'f') ||
                                                         (c >= 'A' && c <= 'F'));
        }

        public static string ToHexString(this byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            const string HexAlphabet = "0123456789ABCDEF";
            StringBuilder result = new StringBuilder(data.Length * 2);

            foreach (byte b in data)
            {
                result.Append(HexAlphabet[(int)(b >> 4)]);
                result.Append(HexAlphabet[(int)(b & 0xF)]);
            }

            return result.ToString();
        }

        public static byte[] ToByteArray(this string hexString)
        {
            byte[] raw;
            if (!TryToByteArray(hexString, out raw))
            {
                throw new ArgumentException("data", ContentResources.InvalidHexString(hexString));
            }

            return raw;
        }

        public static bool TryToByteArray(string hexString, out byte[] bytes)
        {
            if (!hexString.IsHexString())
            {
                bytes = null;
                return false;
            }

            // surely there is a better way to get a byte[] from a hex string...
            bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return true;
        }
    }
}
