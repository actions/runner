using System;
using System.ComponentModel;
using System.Security;
using System.Text;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate String ValueEncoder(String value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ValueEncoders
    {
        public static String Base64StringEscape(String value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        // Base64 is 6 bits -> char
        // A byte is 8 bits
        // When end user doing somthing like base64(user:password)
        // The length of the leading content will cause different base64 encoding result on the password
        // So we add base64(value shifted 1 and two bytes) as secret as well. 
        //    B1         B2      B3                    B4      B5     B6     B7
        // 000000|00 0000|0000 00|000000|            000000|00 0000|0000 00|000000| 
        //  Char1  Char2    Char3   Char4  
        // See the above, the first byte has a character beginning at index 0, the second byte has a character beginning at index 4, the third byte has a character beginning at index 2 and then the pattern repeats
        // We register byte offsets for all these possible values
        public static String Base64StringEscapeShift1(String value)
        {
            return Base64StringEscapeShift(value, 1);
        }

        public static String Base64StringEscapeShift2(String value)
        {
            return Base64StringEscapeShift(value, 2);
        }

        public static String ExpressionStringEscape(String value)
        {
            return Expressions2.Sdk.ExpressionUtility.StringEscape(value);
        }

        public static String JsonStringEscape(String value)
        {
            // Convert to a JSON string and then remove the leading/trailing double-quote.
            String jsonString = JsonConvert.ToString(value);
            String jsonEscapedValue = jsonString.Substring(startIndex: 1, length: jsonString.Length - 2);
            return jsonEscapedValue;
        }

        public static String UriDataEscape(String value)
        {
            return UriDataEscape(value, 65519);
        }

        public static String XmlDataEscape(String value)
        {
            return SecurityElement.Escape(value);
        }

        public static String TrimDoubleQuotes(String value)
        {
            var trimmed = string.Empty;
            if (!string.IsNullOrEmpty(value) &&
                value.Length > 8 &&
                value.StartsWith('"') &&
                value.EndsWith('"'))
            {
                trimmed = value.Substring(1, value.Length - 2);
            }

            return trimmed;
        }

        private static string Base64StringEscapeShift(String value, int shift)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > shift)
            {
                var shiftArray = new byte[bytes.Length - shift];
                Array.Copy(bytes, shift, shiftArray, 0, bytes.Length - shift);
                return Convert.ToBase64String(shiftArray);
            }
            else
            {
                return Convert.ToBase64String(bytes);
            }
        }

        private static String UriDataEscape(
            String value,
            Int32 maxSegmentSize)
        {
            if (value.Length <= maxSegmentSize)
            {
                return Uri.EscapeDataString(value);
            }

            // Workaround size limitation in Uri.EscapeDataString
            var result = new StringBuilder();
            var i = 0;
            do
            {
                var length = Math.Min(value.Length - i, maxSegmentSize);

                if (Char.IsHighSurrogate(value[i + length - 1]) && length > 1)
                {
                    length--;
                }

                result.Append(Uri.EscapeDataString(value.Substring(i, length)));
                i += length;
            }
            while (i < value.Length);

            return result.ToString();
        }
    }
}
