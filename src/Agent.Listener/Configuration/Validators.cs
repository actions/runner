using System;
using System.IO;
using System.Security.Principal;

namespace Microsoft.VisualStudio.Services.Agent.Configuration
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

        public static bool FilePathValidator(string value)
        {
            var directoryInfo = new DirectoryInfo(value);

            if (!directoryInfo.Exists)
            {
                try
                {
                    Directory.CreateDirectory(value);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool BoolValidator(string value)
        {
            return String.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(value, "y", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(value, "n", StringComparison.OrdinalIgnoreCase);
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