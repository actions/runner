using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate IEnumerable<string> ValueEncoder(string value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ValueEncoders
    {

        public static IEnumerable<string> EnumerateBase64Variations(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                // A byte is 8 bits.  A Base64 "digit" can hold a maximum of 6 bits (2^64 - 1, or values 0 to 63).
                // As a result, many Unicode characters (including single-byte letters) cannot be represented using a single Base64 digit.
                // Furthermore, on average a Base64 string will be about 33% longer than the original text.
                // This is because it generally requires 4 Base64 digits to represent 3 Unicode bytes.  (4 / 3 ~ 1.33)
                //
                // Because of this 4:3 ratio (or, more precisely, 8 bits : 6 bits ratio), there's a cyclical pattern
                // to when a byte boundary aligns with a Base64 digit boundary.
                // The pattern repeats every 24 bits (the lowest common multiple of 8 and 6).
                //
                //                    |-----------24 bits-------------|-----------24 bits------------|
                // Base64 Digits:     |digit 0|digit 1|digit 2|digit 3|digit 4|digit 5|digit 6|digit7|
                // Allocated Bits:    aaaaaa  aaBBBB  BBBBcc  cccccc  DDDDDD  DDeeee  eeeeFF  FFFFFF
                // Unicode chars:     |0th char |1st char |2nd char   |3rd char |4th char |5th char  |

                // Depending on alignment, the Base64-encoded secret can take any of 3 basic forms.
                // For example, the Base64 digits representing "abc" could appear as any of the following:
                //    "YWJj"     when aligned
                //    ".!FiYw==" when preceded by 3x + 1 bytes
                //    "..!hYmM=" when preceded by 3x + 2 bytes
                // (where . represents an unrelated Base64 digit, ! represents a Base64 digit that should be masked, and x represents any non-negative integer)

                var rawBytes = Encoding.UTF8.GetBytes(value);

                for (var offset = 0; offset <= 2; offset++)
                {
                    var prunedBytes = rawBytes.Skip(offset).ToArray();
                    if (prunedBytes.Length > 0)
                    {
                        // Don't include Base64 padding characters (=) in Base64 representations of the secret.
                        // They don't represent anything interesting, so they don't need to be masked.
                        // (Some clients omit the padding, so we want to be sure we recognize the secret regardless of whether the padding is present or not.)
                        var buffer = new StringBuilder(Convert.ToBase64String(prunedBytes).TrimEnd(BASE64_PADDING_SUFFIX));
                        yield return buffer.ToString();

                        // Also, yield the RFC4648-equivalent RegEx.
                        buffer.Replace('+', '-');
                        buffer.Replace('/', '_');
                        yield return buffer.ToString();
                    }
                }
            }
        }

        // Used when we pass environment variables to docker to escape " with \"
        public static IEnumerable<string> CommandLineArgumentEscape(string value)
        {
            yield return value.Replace("\"", "\\\"");
        }

        public static IEnumerable<string> ExpressionStringEscape(string value)
        {
            yield return Expressions2.Sdk.ExpressionUtility.StringEscape(value);
        }

        public static IEnumerable<string> JsonStringEscape(string value)
        {
            // Convert to a JSON string and then remove the leading/trailing double-quote.
            String jsonString = JsonConvert.ToString(value);
            String jsonEscapedValue = jsonString.Substring(startIndex: 1, length: jsonString.Length - 2);
            yield return jsonEscapedValue;
        }

        public static IEnumerable<string> UriDataEscape(string value)
        {
            yield return UriDataEscape(value, 65519);
        }

        public static IEnumerable<string> XmlDataEscape(string value)
        {
            yield return SecurityElement.Escape(value);
        }

        public static IEnumerable<string> TrimDoubleQuotes(string value)
        {
            var trimmed = string.Empty;
            if (!string.IsNullOrEmpty(value) &&
                value.Length > 8 &&
                value.StartsWith('"') &&
                value.EndsWith('"'))
            {
                trimmed = value.Substring(1, value.Length - 2);
            }

            yield return trimmed;
        }

        public static IEnumerable<string> PowerShellPreAmpersandEscape(string value)
        {
            // if the secret is passed to PS as a command and it causes an error, sections of it can be surrounded by color codes
            // or printed individually.

            // The secret secretpart1&secretpart2&secretpart3 would be split into 2 sections:
            // 'secretpart1&secretpart2&' and 'secretpart3'. This method masks for the first section.

            // The secret secretpart1&+secretpart2&secretpart3 would be split into 2 sections:
            // 'secretpart1&+' and (no 's') 'ecretpart2&secretpart3'. This method masks for the first section.

            var trimmed = string.Empty;
            if (!string.IsNullOrEmpty(value) && value.Contains("&"))
            {
                var secretSection = string.Empty;
                if (value.Contains("&+"))
                {
                    secretSection = value.Substring(0, value.IndexOf("&+") + "&+".Length);
                }
                else
                {
                    secretSection = value.Substring(0, value.LastIndexOf("&") + "&".Length);
                }

                // Don't mask short secrets
                if (secretSection.Length >= 6)
                {
                    trimmed = secretSection;
                }
            }

            yield return trimmed;
        }

        public static IEnumerable<string> PowerShellPostAmpersandEscape(string value)
        {
            var trimmed = string.Empty;
            if (!string.IsNullOrEmpty(value) && value.Contains("&"))
            {
                var secretSection = string.Empty;
                if (value.Contains("&+"))
                {
                    // +1 to skip the letter that got colored
                    secretSection = value.Substring(value.IndexOf("&+") + "&+".Length + 1);
                }
                else
                {
                    secretSection = value.Substring(value.LastIndexOf("&") + "&".Length);
                }

                if (secretSection.Length >= 6)
                {
                    trimmed = secretSection;
                }
            }

            yield return trimmed;
        }

        private static string UriDataEscape(string value, Int32 maxSegmentSize)
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

        private const char BASE64_PADDING_SUFFIX = '=';
    }
}
