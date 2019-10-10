using GitHub.Services.Common.Internal;
using System;
using System.Text;

namespace GitHub.Services.Common
{
    public static class PrimitiveExtensions
    {
        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private static readonly long maxSecondsSinceUnixEpoch = (long)DateTime.MaxValue.Subtract(UnixEpoch).TotalSeconds;

        //extension methods to convert to and from a Unix Epoch time to a DateTime
        public static Int64 ToUnixEpochTime(this DateTime dateTime)
        {
            return Convert.ToInt64((dateTime.ToUniversalTime() - UnixEpoch).TotalSeconds);
        }

        public static DateTime FromUnixEpochTime(this Int64 unixTime)
        {
            if (unixTime >= maxSecondsSinceUnixEpoch)
            {
                return DateTime.MaxValue;
            }
            else
            {
                return UnixEpoch + TimeSpan.FromSeconds(unixTime);
            }
        }

        public static string ToBase64StringNoPaddingFromString(string utf8String)
        {
            return ToBase64StringNoPadding(Encoding.UTF8.GetBytes(utf8String));
        }

        public static string FromBase64StringNoPaddingToString(string base64String)
        {
            byte[] result = FromBase64StringNoPadding(base64String);

            if (result == null || result.Length == 0)
            {
                return null;
            }

            return Encoding.UTF8.GetString(result, 0, result.Length);
        }

        //These methods convert To and From base64 strings without padding
        //for JWT scenarios
        //code taken from the JWS spec here: 
        //http://tools.ietf.org/html/draft-ietf-jose-json-web-signature-08#appendix-C
        public static String ToBase64StringNoPadding(this byte[] bytes)
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(bytes, "bytes");

            string s = Convert.ToBase64String(bytes); // Regular base64 encoder
            s = s.Split('=')[0]; // Remove any trailing '='s
            s = s.Replace('+', '-'); // 62nd char of encoding
            s = s.Replace('/', '_'); // 63rd char of encoding
            return s;
        }

        public static byte[] FromBase64StringNoPadding(this String base64String)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(base64String, "base64String");

            string s = base64String;
            s = s.Replace('-', '+'); // 62nd char of encoding
            s = s.Replace('_', '/'); // 63rd char of encoding
            switch (s.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: s += "=="; break; // Two pad chars
                case 3: s += "="; break; // One pad char
                default:
                    throw new ArgumentException(CommonResources.IllegalBase64String(), "base64String");
            }
            return Convert.FromBase64String(s); // Standard base64 decoder
        }

        /// <summary>
        /// Converts base64 represented value into hex string representation.
        /// </summary>
        public static String ConvertToHex(String base64String)
        {
            var bytes = FromBase64StringNoPadding(base64String);
            return BitConverter.ToString(bytes).Replace("-", String.Empty);
        }
    }
}
