using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using System;
using System.Linq;
using System.IO;
using System.Security.Principal;

namespace GitHub.Runner.Listener.Configuration
{
    public static class Validators
    {
        private static String UriHttpScheme = "http";
        private static String UriHttpsScheme = "https";

        public static bool ServerUrlValidator(string value)
        {
            try
            {
                Uri uri;
                if (Uri.TryCreate(value, UriKind.Absolute, out uri))
                {
                    if (uri.Scheme.Equals(UriHttpScheme, StringComparison.OrdinalIgnoreCase)
                        || uri.Scheme.Equals(UriHttpsScheme, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        public static bool AuthSchemeValidator(string value)
        {
            return CredentialManager.CredentialTypes.ContainsKey(value);
        }

        public static bool BoolValidator(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "Y", StringComparison.CurrentCultureIgnoreCase) ||
                   string.Equals(value, "N", StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool LabelsValidator(string labels)
        {
            if (!string.IsNullOrEmpty(labels))
            {
                var labelSet = labels.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (labelSet.Any(x => x.Length > 256))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool NonEmptyValidator(string value)
        {
            return !string.IsNullOrEmpty(value);
        }

        public static bool NTAccountValidator(string arg)
        {
            if (string.IsNullOrEmpty(arg) || String.IsNullOrEmpty(arg.TrimStart('.', '\\')))
            {
                return false;
            }

            try
            {
                var logonAccount = arg.TrimStart('.');
                NTAccount ntaccount = new NTAccount(logonAccount);
                SecurityIdentifier sid = (SecurityIdentifier)ntaccount.Translate(typeof(SecurityIdentifier));
            }
            catch (IdentityNotMappedException)
            {
                return false;
            }

            return true;
        }
    }
}
