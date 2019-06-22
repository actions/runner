using System;
using System.Globalization;
using System.Text;

namespace GitHub.Services.Content.Common.Tracing
{
    public static class SafeStringFormat
    {
        /// <summary>
        /// InvariantCulture by default to avoid:
        /// Different DateTime formats (e.g. en-au instead of en-us) can cause a FormatException
        /// </summary>
        internal static readonly IFormatProvider SafeFormat = CultureInfo.InvariantCulture;

        public static string FormatSafe(string format, params object[] args)
        {
            return FormatSafe(SafeFormat, format, args);
        }

        public static string FormatSafe(IFormatProvider provider, string format, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                // Allow the caller to designate the format as a verbatim message (e.g. preformatted) by passing null or empty args.
                return format;
            }
            else
            {
                try
                {
                    return string.Format(provider, format, args);
                }
                catch (FormatException e)
                {
                    // Failed, fallback to the unformatted string and the exception message.
                    // e.g. MissingParameterValue{1} (FormatException: Index (zero based) must be greater than or equal to zero and less than the size of the argument list.)
                    return format + FormatSafe($" ({e.GetType().Name}: {e.Message})");
                }
            }
        }

        /// <summary>
        /// Formats IFormattables, such as interpolated strings, using CultureInfo.InvariantCulture.
        /// For example, FormatSafe($"My interpolated string: {date}")
        /// </summary>
        public static string FormatSafe(this IFormattable formattable)
        {
            return formattable.ToString(null, SafeStringFormat.SafeFormat);
        }
    }

    public static class SafeStringFormatDateTimeExtensions
    {
        public static string ToStringSafe(this DateTime dateTime, string format)
        {
            return dateTime.ToString(format, SafeStringFormat.SafeFormat);
        }
    }
    
    public static class SafeStringFormatIFormattableExtensions
    {
        public static string ToStringSafe(this IFormattable formattable)
        {
            return SafeStringFormat.FormatSafe(formattable);
        }
    }

    public static class SafeStringFormatStringBuilderExtensions
    {
        public static void AppendFormatSafe(this StringBuilder message, string format, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                // Allow the caller to designate the format as a verbatim message (e.g. preformatted) by passing null or empty args.
                message.Append(format);
            }
            else
            {
                // If AppendFormat throws below, then it will have already written some text to the StringBuilder.
                // Therefore, we AppendFormat to a test first, so we can apply the fallback in case it fails.
                var test = new StringBuilder();

                try
                {
                    test.AppendFormat(SafeStringFormat.SafeFormat, format, args);

                    // Succeeded, append to the real message
                    message.AppendFormat(SafeStringFormat.SafeFormat, format, args);
                }
                catch (FormatException e)
                {
                    // Failed, fallback to appending the unformatted string and the exception message.
                    // e.g. MissingParameterValue{1} (FormatException: Index (zero based) must be greater than or equal to zero and less than the size of the argument list.)
                    message.Append(format);
                    message.Append(SafeStringFormat.FormatSafe($" ({e.GetType().Name}: {e.Message})"));
                }
            }
        }
    }
}
