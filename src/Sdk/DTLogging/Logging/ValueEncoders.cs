using System;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate String ValueEncoder(String value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ValueEncoders
    {
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

        internal static String UriDataEscape(
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
