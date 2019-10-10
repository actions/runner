using System;
using System.Text.RegularExpressions;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines.Expressions
{
    public static class RegexUtility
    {
        /// <summary>
        /// Gets default timeout for regex
        /// </summary>
        /// <returns></returns>
        public static TimeSpan GetRegexTimeOut()
        {
            return s_regexTimeout;
        }

        /// <summary>
        /// Performs regex single match with ECMAScript-complaint behavior
        /// Will throw RegularExpressionFailureException if regular expression parsing error occurs or if regular expression takes more than allotted time to execute
        /// Supported regex options - 'i' (ignorecase), 'm' (multiline)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="regex"></param>
        /// <param name="regexOptionsString"></param>
        /// <returns></returns>
        public static bool IsMatch(
            String value,
            String regexPattern,
            String regexOptionsString)
        {
            return IsSafeMatch(value, regexPattern, ConvertToRegexOptions(regexOptionsString));
        }

        /// <summary>
        /// Performs regex single match with ECMAScript-complaint behavior
        /// Will throw RegularExpressionFailureException if regular expression parsing error occurs or if regular expression takes more than allotted time to execute
        /// If the key is not known, returns true
        /// </summary>
        /// <param name="value"></param>
        /// <param name="wellKnownRegexKey">One of WellKnownRegularExpressionKeys</param>
        /// <returns></returns>
        public static bool IsMatch(
            String value,
            String wellKnownRegexKey)
        {
            Lazy<Regex> lazyRegex = WellKnownRegularExpressions.GetRegex(wellKnownRegexKey);
            if (lazyRegex == null)
            {
                return true;
            }

            Regex regex = lazyRegex.Value;
            return IsSafeMatch(value, x => regex.Match(value));
        }

        /// <summary>
        /// Converts regex in string to RegExOptions, valid flags are "i", "m"
        /// Throws RegularExpressionInvalidOptionsException if there are any invalid options
        /// </summary>
        /// <param name="regexOptions"></param>
        /// <returns></returns>
        public static RegexOptions ConvertToRegexOptions(String regexOptions)
        {
            RegexOptions result;
            if (TryConvertToRegexOptions(regexOptions, out result))
            {
                return result;
            }

            throw new RegularExpressionInvalidOptionsException(PipelineStrings.InvalidRegexOptions(regexOptions, String.Join(",", WellKnownRegexOptions.All)));
        }

        private static bool TryConvertToRegexOptions(
            String regexOptions,
            out RegexOptions result)
        {
            // Eg: "IgnoreCase, MultiLine" or "IgnoreCase"
            result = RegexOptions.ECMAScript | RegexOptions.CultureInvariant;

            if (String.IsNullOrEmpty(regexOptions))
            {
                return false;
            }

            String[] regexOptionValues = regexOptions.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < regexOptionValues.Length; i++)
            {
                String option = regexOptionValues[i];

                if (String.Equals(option, WellKnownRegexOptions.IgnoreCase, StringComparison.OrdinalIgnoreCase))
                {
                    result = result | RegexOptions.IgnoreCase;
                }
                else if (String.Equals(option, WellKnownRegexOptions.Multiline, StringComparison.OrdinalIgnoreCase))
                {
                    result = result | RegexOptions.Multiline;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static Boolean IsSafeMatch(
            String value,
            Func<String, Match> getSafeMatch)
        {
            Boolean result = true;
            try
            {
                var match = getSafeMatch(value);
                result = match.Success;
            }
            catch (Exception ex) when (ex is RegexMatchTimeoutException || ex is ArgumentException)
            {
                throw new RegularExpressionValidationFailureException(PipelineStrings.RegexFailed(value, ex.Message), ex);
            }

            return result;
        }

        private static Boolean IsSafeMatch(
            String value,
            String regex,
            RegexOptions regexOptions)
        {
            return IsSafeMatch(value, x => GetSafeMatch(x, regex, regexOptions));
        }

        private static Match GetSafeMatch(
            String value,
            String regex,
            RegexOptions regexOptions)
        {
            return Regex.Match(value, regex, regexOptions, s_regexTimeout);
        }

        // 2 seconds should be enough mostly, per DataAnnotations class - http://index/?query=REGEX_DEFAULT_MATCH_TIMEOUT
        private static TimeSpan s_regexTimeout = TimeSpan.FromSeconds(2);

        private static class WellKnownRegexOptions
        {
            public static String IgnoreCase = nameof(IgnoreCase);
            public static String Multiline = nameof(Multiline);
            public static String[] All = new String[] { IgnoreCase, Multiline };
        }
    }
}
