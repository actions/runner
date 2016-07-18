using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class StringUtil
    {
        private static readonly object[] s_defaultFormatArgs = new object[] { null };
        private static Dictionary<string, object> s_locStrings;

        static StringUtil()
        {
#if OS_WINDOWS
            // By default, only Unicode encodings, ASCII, and code page 28591 are supported.
            // This line is required to support the full set of encodings that were included
            // in Full .NET prior to 4.6.
            //
            // For example, on an en-US box, this is required for loading the encoding for the
            // default console output code page '437'. Without loading the correct encoding for
            // code page IBM437, some characters cannot be translated correctly, e.g. write 'ç'
            // from powershell.exe.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }

        public static string Format(string format, params object[] args)
        {
            return Format(CultureInfo.InvariantCulture, format, args);
        }

        public static void EnsureRegisterEncodings()
        {
            // The static constructor should have registered the required encodings.
        }

        public static Encoding GetSystemEncoding()
        {
#if OS_WINDOWS
            // The static constructor should have registered the required encodings.
            // Code page 0 is equivalent to the current system default (i.e. CP_ACP).
            // E.g. code page 1252 on an en-US box.
            return Encoding.GetEncoding(0);
#else
            throw new NotSupportedException(nameof(SystemEncoding)); // Should never reach here.
#endif
        }

        // Do not combine the non-format overload with the format overload.
        public static string Loc(string locKey)
        {
            string locStr = locKey;
            try
            {
                EnsureLoaded();
                if (s_locStrings.ContainsKey(locKey))
                {
                    object item = s_locStrings[locKey];
                    if (item is string)
                    {
                        locStr = item as string;
                    }
                    else if (item is JArray)
                    {
                        string[] lines = (item as JArray).ToObject<string[]>();
                        var sb = new StringBuilder();
                        for (int i = 0 ; i < lines.Length ; i++)
                        {
                            if (i > 0)
                            {
                                sb.AppendLine();
                            }

                            sb.Append(lines[i]);
                        }

                        locStr = sb.ToString();
                    }
                }
                else
                {
                    locStr = StringUtil.Format("notFound:{0}", locKey);
                }
            }
            catch (Exception)
            {
                // loc strings shouldn't take down agent.  any failures returns loc key
            }
            
            return locStr;
        }

        // Do not combine the non-format overload with the format overload.
        public static string Loc(string locKey, params object[] args)
        {
            return Format(CultureInfo.CurrentCulture, Loc(locKey), args);
        }

        /// <summary>
        /// Convert String to boolean, valid true string: "1", "true", "$true", valid false string: "0", "false", "$false".
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue">default result when value is null or empty or not a valid true/false string.</param>
        /// <returns></returns>
        public static bool ConvertToBoolean(string value, bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            switch (value.ToLowerInvariant())
            {
                case "1":
                case "true":
                case "$true":
                    return true;
                case "0":
                case "false":
                case "$false":
                    return false;
                default:
                    return defaultValue;
            }
        }

        private static string Format(CultureInfo culture, string format, params object[] args)
        {
            try
            {
                // 1) Protect against argument null exception for the format parameter.
                // 2) Protect against argument null exception for the args parameter.
                // 3) Coalesce null or empty args with an array containing one null element.
                //    This protects against format exceptions where string.Format thinks
                //    that not enough arguments were supplied, even though the intended arg
                //    literally is null or an empty array.
                return string.Format(
                    culture,
                    format ?? string.Empty,
                    args == null || args.Length == 0 ? s_defaultFormatArgs : args);
            }
            catch (FormatException)
            {
                // TODO: Log that string format failed. Consider moving this into a context base class if that's the only place it's used. Then the current trace scope would be available as well.
                if (args != null)
                {
                    return string.Format(culture, "{0} {1}", format, string.Join(", ", args));
                }

                return format;
            }
        }

        private static void EnsureLoaded()
        {
            if (s_locStrings == null)
            {
                // Determine the list of resource files to load. The fallback "en-US" strings should always be
                // loaded into the dictionary first.
                string[] cultureNames;
                if (string.IsNullOrEmpty(CultureInfo.CurrentCulture.Name) || // Exclude InvariantCulture.
                    string.Equals(CultureInfo.CurrentCulture.Name, "en-US", StringComparison.Ordinal))
                {
                    cultureNames = new[] { "en-US" };
                }
                else
                {
                    cultureNames = new[] { "en-US", CultureInfo.CurrentCulture.Name };
                }

                // Initialize the dictionary.
                var locStrings = new Dictionary<string, object>();
                foreach (string cultureName in cultureNames)
                {
                    // Merge the strings from the file into the instance dictionary.
                    string file = Path.Combine(IOUtil.GetBinPath(), cultureName, "strings.json");
                    if (File.Exists(file))
                    {
                        foreach (KeyValuePair<string, object> pair in IOUtil.LoadObject<Dictionary<string, object>>(file))
                        {
                            locStrings[pair.Key] = pair.Value;
                        }
                    }
                }

                // Store the instance.
                s_locStrings = locStrings;
            }
        }
    }
}