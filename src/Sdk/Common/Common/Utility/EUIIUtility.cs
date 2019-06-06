using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitHub.Services.Common
{
    public static class EuiiUtility
    {
        public static bool ContainsEmail(string message, bool assertOnDetection = true)
        {
            return PatternMatch(message, s_emailRegex, assertOnDetection);
        }

        public static bool ContainsIpAddress(string message, bool assertOnDetection = false)
        {
            return PatternMatch(message, s_ipAddressRegex, assertOnDetection);
        }

        private static bool PatternMatch(string message, Regex pattern, bool assertOnDetection)
        {
            Match match = pattern.Match(message);

            // Note: The if debug check below is required as we do no want to enabled this peice of logic for releases bits (production)
            if (match.Success && assertOnDetection)
            {
                // we need to mask the euii string in the message, otherwise it may go into the endless loop
                string maskedMessage = Regex.Replace(message, pattern.ToString(), c_euiiMask);
                EUIILeakException exception = new EUIILeakException(maskedMessage);
#if DEBUG
                Debug.Assert(false, exception.Message);
#endif
                throw exception;
            }
            return match.Success;
        }

        private static readonly Regex s_emailRegex = new Regex(c_emailPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private const string c_emailPattern = @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}";

        private static readonly Regex s_ipAddressRegex = new Regex(c_ipAddressPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private const string c_ipAddressPattern = @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";

        private const string c_euiiMask = "******";
    }
}
