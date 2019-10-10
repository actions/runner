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

        // Base64 is 6 bytes -> char
        // When end user doing somthing like base64(user:password)
        // The length of the leading content will cause different base64 encoding result on the password
        // So we add base64(value - 1/2/3/4/5 bytes) as secret as well.
        public static String Base64StringEscapeShift1(String value)
        {
            return Base64StringEscapeShift(value, 1);
        }

        public static String Base64StringEscapeShift2(String value)
        {
            return Base64StringEscapeShift(value, 2);
        }

        public static String Base64StringEscapeShift3(String value)
        {
            return Base64StringEscapeShift(value, 3);
        }

        public static String Base64StringEscapeShift4(String value)
        {
            return Base64StringEscapeShift(value, 4);
        }

        public static String Base64StringEscapeShift5(String value)
        {
            return Base64StringEscapeShift(value, 5);
        }

        public static String ExpressionStringEscape(String value)
        {
            return Expressions.ExpressionUtil.StringEscape(value);
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
